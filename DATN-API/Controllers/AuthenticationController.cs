/*using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using DATN_API.Data;
using DATN_API.Models;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Exceptions;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net; // Đảm bảo bạn đã cài đặt package này

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase // Luôn được register ở DI scope
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        private static Dictionary<string, string> _verificationCodes = new();
        private static Dictionary<string, DateTime> _lastCodeSentTime = new();
        private static HashSet<string> _verifiedAccounts = new();

        private static readonly TimeSpan _resendDelay = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan _codeExpiration = TimeSpan.FromMinutes(2);

        public AuthenticationController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        private bool IsPhoneNumber(string input)
        {
            //_verifiedAccounts.Add(input);
            return Regex.IsMatch(input, @"^\+84\d{9,10}$");
        }

        [HttpPost("SendVerificationCode")]
        public async Task<IActionResult> SendVerificationCode([FromBody] string input)
        {
            bool isPhone = IsPhoneNumber(input);

            if (_lastCodeSentTime.TryGetValue(input, out DateTime lastSent) && (DateTime.UtcNow - lastSent) < _resendDelay)
                return BadRequest($"Vui lòng chờ {(_resendDelay - (DateTime.UtcNow - lastSent)).Seconds} giây trước khi gửi lại mã.");

            if (isPhone)
            {
                if (await _context.Users.AnyAsync(u => u.PhoneNumber == input))
                    return BadRequest("Số điện thoại đã được đăng ký!");

                var code = new Random().Next(100000, 999999).ToString();
                _verificationCodes[input] = code;
                _lastCodeSentTime[input] = DateTime.UtcNow;

                try
                {
                    TwilioClient.Init(_configuration["Twilio:AccountSID"], _configuration["Twilio:AuthToken"]);
                    await MessageResource.CreateAsync(
                        body: $"Mã xác thực của bạn là: {code}",
                        from: new Twilio.Types.PhoneNumber(_configuration["Twilio:PhoneNumber"]),
                        to: new Twilio.Types.PhoneNumber(input)
                    );
                    return Ok("Mã OTP đã được gửi!");
                }
                catch (TwilioException ex)
                {
                    Console.WriteLine($"Lỗi gửi SMS: {ex.Message}");
                    return BadRequest("Không thể gửi OTP!");
                }
            }
            else
            {
                try
                {
                    var mailAddress = new MailAddress(input); // xác thực email hợp lệ
                }
                catch
                {
                    return BadRequest("Định dạng email không hợp lệ!");
                }

                if (await _context.Users.AnyAsync(u => u.Email == input))
                    return BadRequest("Email đã được đăng ký!");

                var code = new Random().Next(100000, 999999).ToString();
                _verificationCodes[input] = code;
                _lastCodeSentTime[input] = DateTime.UtcNow;

                bool sent = SendEmail(input, "Mã xác thực đăng ký", $"Mã xác thực của bạn là: {code}");
                return sent ? Ok("Mã xác thực đã được gửi!") : BadRequest("Không thể gửi email!");
            }
        }

        [HttpPost("VerifyCode")]
        public IActionResult VerifyCode([FromBody] VerifyRequest request)
        {
            if (_verificationCodes.TryGetValue(request.Identifier, out string code) && code == request.Code)
            {
                if (_lastCodeSentTime.TryGetValue(request.Identifier, out DateTime sentTime) && (DateTime.UtcNow - sentTime) <= _codeExpiration)
                {
                    _verificationCodes.Remove(request.Identifier);
                    _lastCodeSentTime.Remove(request.Identifier);
                    _verifiedAccounts.Add(request.Identifier);
                    return Ok(true);
                }
                else
                {
                    _verificationCodes.Remove(request.Identifier);
                    _lastCodeSentTime.Remove(request.Identifier);
                    return BadRequest("Mã xác thực đã hết hạn!");
                }
            }
            return BadRequest("Mã xác thực không đúng!");
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            string identifier = request.Identifier;
            bool isPhone = IsPhoneNumber(identifier);

            // Kiểm tra xem mã OTP đã được xác minh chưa
            //if (!_verifiedAccounts.Contains(identifier)) // Luôn = false => Trả ra 'Bạn chưa xác minh mã OTP hoặc email!'
            //    return BadRequest("Bạn chưa xác minh mã OTP hoặc email!");

            // Kiểm tra xác nhận mật khẩu
            if (request.Password != request.ConfirmPassword)
                return BadRequest("Mật khẩu xác nhận không khớp!");

            // Kiểm tra trùng tài khoản
            bool accountExists = isPhone
                ? await _context.Users.AnyAsync(u => u.PhoneNumber == identifier)
                : await _context.Users.AnyAsync(u => u.Email == identifier);

            if (accountExists)
                return BadRequest("Tài khoản đã tồn tại!");

            // Tạo tài khoản mới
            var user = new Users
            {
                Email = isPhone ? "" : identifier,
                PhoneNumber = isPhone ? identifier : "",
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                RoleId = 1,
                Avatar = "",
                FullName = "Người dùng",
                Status = false,
                Gender = false,
                CitizenIdentityCard = "",
                CreatedAt = DateTime.Now,
                DateOfBirth = null

            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _verifiedAccounts.Remove(identifier);
            return Ok("Đăng ký thành công!");
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Identifier || u.PhoneNumber == request.Identifier);

            if (user == null)
            {
                return Unauthorized("Tài khoản không tồn tại!");
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            {
                return Unauthorized("Mật khẩu không đúng!");
            }

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                Id = user.Id,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                FullName = user.FullName,
                Roles = user.RoleId,
                Token = token
            });
        }

        private string GenerateJwtToken(Users user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.MobilePhone, user.PhoneNumber),
                new Claim(ClaimTypes.Role, user.RoleId.ToString()),
                new Claim("FullName", user.FullName)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private bool SendEmail(string toEmail, string subject, string body)
        {
            try
            {
                var smtp = new SmtpClient
                {
                    Host = _configuration["EmailSettings:SmtpServer"],
                    Port = int.Parse(_configuration["EmailSettings:SmtpPort"]),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_configuration["EmailSettings:SmtpUsername"], _configuration["EmailSettings:SmtpPassword"])
                };

                var message = new MailMessage(_configuration["EmailSettings:FromEmail"], toEmail, subject, body);
                smtp.Send(message);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi gửi email: {ex.Message}");
                return false;
            }
        }

       
        [HttpPost("ChangePasswordWithIdentifier")]
        public async Task<IActionResult> ChangePasswordWithIdentifier([FromBody] ChangePasswordWithIdentifierRequest request)
        {
            if (request.NewPassword == null || request.ConfirmNewPassword == null || request.CurrentPassword == null || string.IsNullOrEmpty(request.Identifier))
            {
                return BadRequest("Vui lòng nhập đầy đủ thông tin.");
            }

            if (request.NewPassword != request.ConfirmNewPassword)
            {
                return BadRequest("Mật khẩu mới và mật khẩu xác nhận không khớp.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Identifier || u.PhoneNumber == request.Identifier);
            if (user == null)
            {
                return NotFound("Không tìm thấy người dùng.");
            }

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password))
            {
                return Unauthorized("Mật khẩu cũ không đúng.");
            }

            if (request.NewPassword.Length < 6)
            {
                return BadRequest("Mật khẩu mới phải có ít nhất 6 ký tự.");
            }

            try
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                await _context.SaveChangesAsync();
                return Ok("Đổi mật khẩu thành công!");
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Lỗi khi cập nhật mật khẩu: {ex.Message}");
                return StatusCode(500, "Đã xảy ra lỗi khi đổi mật khẩu. Vui lòng thử lại sau.");
            }
        }





        [HttpPost("SendOtpToNewEmail")]
        public async Task<IActionResult> SendOtpToNewEmail([FromBody] string newEmail)
        {
            try
            {
                var mailAddress = new MailAddress(newEmail);
            }
            catch
            {
                return BadRequest("Định dạng email mới không hợp lệ!");
            }

            if (_lastCodeSentTime.TryGetValue(newEmail, out DateTime lastSent) && (DateTime.UtcNow - lastSent) < _resendDelay)
                return BadRequest($"Vui lòng chờ {(_resendDelay - (DateTime.UtcNow - lastSent)).Seconds} giây trước khi gửi lại mã.");

            var code = new Random().Next(100000, 999999).ToString();
            _verificationCodes[newEmail] = code;
            _lastCodeSentTime[newEmail] = DateTime.UtcNow;

            bool sent = SendEmail(newEmail, "Mã xác thực Email", $"Mã xác thực của bạn là: {code}");
            return sent ? Ok("Mã xác thực đã được gửi đến email mới của bạn!") : BadRequest("Không thể gửi email!");
        }
        
        [HttpPost("ChangeEmail")]
        public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequest request)
        {
            if (string.IsNullOrEmpty(request.NewEmail) || string.IsNullOrEmpty(request.OtpCode) || request.UserId == 0)
            {
                return BadRequest("Vui lòng cung cấp đầy đủ ID người dùng, email mới và mã OTP.");
            }

            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
            {
                return NotFound("Không tìm thấy người dùng.");
            }
         
            if (await _context.Users.AnyAsync(u => u.Email == request.NewEmail && u.Id != user.Id))
            {
                return BadRequest("Email mới đã được sử dụng bởi một tài khoản khác!");
            }

            if (!_verificationCodes.TryGetValue(request.NewEmail, out string storedCode) || storedCode != request.OtpCode)
            {
                return BadRequest("Mã OTP không đúng.");
            }

            if (_lastCodeSentTime.TryGetValue(request.NewEmail, out DateTime sentTime) && (DateTime.UtcNow - sentTime) > _codeExpiration)
            {
                _verificationCodes.Remove(request.NewEmail);
                _lastCodeSentTime.Remove(request.NewEmail);
                return BadRequest("Mã OTP đã hết hạn.");
            }

            try
            {
                
                if (string.IsNullOrEmpty(user.Email))
                {
                    user.Email = request.NewEmail;
                    await _context.SaveChangesAsync();
                    _verificationCodes.Remove(request.NewEmail);
                    _lastCodeSentTime.Remove(request.NewEmail);
                    _verifiedAccounts.Remove(request.NewEmail);
                    return Ok("Thêm email thành công!");
                }
                else // Tài khoản đã có email, tiến hành sửa
                {
                    user.Email = request.NewEmail;
                    await _context.SaveChangesAsync();
                    _verificationCodes.Remove(request.NewEmail);
                    _lastCodeSentTime.Remove(request.NewEmail);
                    _verifiedAccounts.Remove(request.NewEmail);
                    return Ok("Đổi email thành công!");
                }
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Lỗi khi cập nhật email: {ex.Message}");
                return StatusCode(500, "Đã xảy ra lỗi khi cập nhật email. Vui lòng thử lại sau.");
            }
        }
        public class ChangePasswordWithIdentifierRequest
        {
            public string Identifier { get; set; } 
            public string CurrentPassword { get; set; }
            public string NewPassword { get; set; }
            public string ConfirmNewPassword { get; set; }
        }

        public class VerifyRequest
        {
            public string Identifier { get; set; }
            public string Code { get; set; }
        }

        public class RegisterRequest
        {
            public string Identifier { get; set; }
            public string Password { get; set; }
            public string ConfirmPassword { get; set; }
        }

        public class LoginRequest
        {
            public string Identifier { get; set; }
            public string Password { get; set; }
        }

        public class ChangeEmailRequest
        {
            public int UserId { get; set; }
            public string NewEmail { get; set; }
            public string OtpCode { get; set; }
        }
    }
}*/