using Microsoft.AspNetCore.Mvc;
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
    public class AuthenticationController : ControllerBase
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
            if (!_verifiedAccounts.Contains(identifier))
                return BadRequest("Bạn chưa xác minh mã OTP hoặc email!");

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
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
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
    }
}