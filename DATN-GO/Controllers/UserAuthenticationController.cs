using DATN_GO.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using DATN_GO.ViewModels.Authentication;

namespace DATN_GO.Controllers
{
    public class UserAuthenticationController : Controller
    {
        private readonly AuthenticationService _AuthenticationService;
        private readonly ILogger<UserAuthenticationController> _logger;

        public UserAuthenticationController(ILogger<UserAuthenticationController> logger, AuthenticationService authenticationService)
        {
            _logger = logger;
            _AuthenticationService = authenticationService;
        }


        [HttpGet("Login")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginRequest model)
        {
            if (!ModelState.IsValid)
            {
                return View("Login", model);
            }

            var loginResult = await _AuthenticationService.LoginAsync(model.Identifier, model.Password);

            if (loginResult != null)
            {
                HttpContext.Session.SetString("JwtToken", loginResult.Token);
                HttpContext.Session.SetString("Id", loginResult.Id.ToString());
                HttpContext.Session.SetString("FullName", loginResult.FullName);
                HttpContext.Session.SetString("Role", loginResult.Roles.ToString());
                HttpContext.Session.SetString("Identifier", model.Identifier);
                HttpContext.Session.SetString("Email", loginResult.Email ?? string.Empty);

                TempData["ToastMessage"] = $"Chào mừng {loginResult.FullName}, bạn đã đăng nhập thành công!";
                TempData["ToastType"] = "success";

                string redirectUrl = loginResult.Roles switch
                {
                    1 => "/",
                    2 => "/Seller/Home/",
                    _ => "/Admin/Home/"
                };
                return Redirect(redirectUrl);
            }
            else
            {
                TempData["ToastMessage"] = "Tài khoản hoặc mật khẩu không đúng!";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Login");
            }
        }


        [HttpGet("Register")]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register(RegisterIdentifierRequest model)
        {
            if (!ModelState.IsValid)
            {
                return View("Register", model);
            }

            var (success, message) = await _AuthenticationService.SendVerificationCodeAsync(model.Identifier);

            if (success)
            {
                TempData["ToastMessage"] = message;
                TempData["ToastType"] = "success";
                return RedirectToAction("AuthenticationCode", new { identifier = model.Identifier });
            }
            else
            {
                ModelState.AddModelError(nameof(model.Identifier), message);
                return View("Register", model);
            }
        }

        [HttpGet("CreatePassword")]
        public IActionResult CreatePassword(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.Identifier = identifier;
            return View();
        }

        [HttpPost("CreatePassword")]
        public async Task<IActionResult> CreatePassword(RegisterRequest model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Identifier = model.Identifier;
                return View(model);
            }

            if (string.IsNullOrEmpty(model.Identifier))
                return RedirectToAction("Register");


            var (success, message) = await _AuthenticationService.RegisterAsync(model.Identifier, model.Password, model.ConfirmPassword);

            if (success)
            {
                TempData["ToastMessage"] = "🎉 Bạn đã đăng ký thành công! Vui lòng đăng nhập.";
                TempData["ToastType"] = "success";
                return RedirectToAction("index", "Home");
            }
            else
            {
                ViewBag.Identifier = model.Identifier;
                return View();
            }
        }




        [HttpGet("AuthenticationCode")]
        public IActionResult AuthenticationCode(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return RedirectToAction("Register");
            }
            ViewBag.Identifier = identifier;
            return View();
        }


        [HttpPost("AuthenticationCode")]
        public async Task<IActionResult> AuthenticationCode(string identifier, string code)
        {

            if (string.IsNullOrEmpty(identifier) || string.IsNullOrEmpty(code))
            {
                TempData["ToastMessage"] = " Thiếu thông tin hoặc mã OTP. Vui lòng thử lại!";
                TempData["ToastType"] = "danger";
                ViewBag.Identifier = identifier;
                return View();
            }

            var (success, message) = await _AuthenticationService.VerifyCodeAsync(identifier, code);

            if (success)
            {
                TempData["ToastMessage"] = " Mã xác thực hợp lệ! " + message;
                TempData["ToastType"] = "success";
                return RedirectToAction("CreatePassword", new { identifier });
            }
            else
            {
                TempData["ToastMessage"] = $" {message}";
                TempData["ToastType"] = "danger";
                ViewBag.Identifier = identifier;
                return View();
            }
        }



        [HttpPost("ResendAuthenticationCode")]
        public async Task<IActionResult> ResendAuthenticationCode(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                TempData["ToastMessage"] = " Không tìm thấy thông tin người dùng để gửi lại mã.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Register");
            }

            var (success, message) = await _AuthenticationService.SendVerificationCodeAsync(identifier);

            if (success)
            {
                TempData["ToastMessage"] = message;
                TempData["ToastType"] = "success";
            }
            else
            {
                TempData["ToastMessage"] = $" {message}";
                TempData["ToastType"] = "danger";
            }

            return RedirectToAction("AuthenticationCode", new { identifier });
        }

       
        [HttpGet("ChangePassword")]
        public IActionResult ChangePassword()
        {
            string? identifier = HttpContext.Session.GetString("Identifier");
            ViewBag.Identifier = identifier;
            return View();
        }

        [HttpPost("ChangePassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            string? identifier = HttpContext.Session.GetString("Identifier");

            if (string.IsNullOrEmpty(identifier))
            {
                _logger.LogWarning("Identifier not found in session for ChangePassword. Redirecting to Login.");
                TempData["ToastMessage"] = "Không tìm thấy thông tin tài khoản. Vui lòng đăng nhập lại.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Login");
            }

            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                TempData["ToastMessage"] = "Vui lòng nhập đầy đủ thông tin.";
                TempData["ToastType"] = "danger";
                ViewBag.Identifier = identifier; 
                return View();
            }

            if (newPassword != confirmPassword)
            {
                TempData["ToastMessage"] = "Mật khẩu mới và mật khẩu xác nhận không khớp.";
                TempData["ToastType"] = "danger";
                ViewBag.Identifier = identifier; 
                return View();
            }

            var (success, message) = await _AuthenticationService.ChangePasswordAsync(identifier, currentPassword, newPassword, confirmPassword);

            if (success)
            {
                TempData["ToastMessage"] = message;
                TempData["ToastType"] = "success";

                HttpContext.Session.Clear();
                TempData["ToastMessage"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.";
                TempData["ToastType"] = "success";
                return RedirectToAction("Index", "Home");
            }
            else
            {
                TempData["ToastMessage"] = message;
                TempData["ToastType"] = "danger";
                ViewBag.Identifier = identifier; 
                return View();
            }
        }
        [HttpGet("Logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            if (TempData["ChangeEmailSuccessMessage"] is string changeEmailMessage)
            {
                TempData["ToastMessage"] = "Đã đổi thông tin email thành công. Vui lòng đăng nhập lại.";
                TempData["ToastType"] = TempData["ChangeEmailSuccessType"];
            }
            else
            {
                TempData["ToastMessage"] = " Bạn đã đăng xuất thành công!";
                TempData["ToastType"] = "success";
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet("ChangeEmailPrompt")]
        public IActionResult ChangeEmailPrompt()
        {
            if (HttpContext.Session.GetString("Id") == null)
            {
                TempData["ToastMessage"] = "Vui lòng đăng nhập để đổi email.";
                TempData["ToastType"] = "warning";
                return RedirectToAction("Login");
            }
            ViewBag.CurrentEmail = HttpContext.Session.GetString("Email");
            return View();
        }

        [HttpPost("ChangeEmailPrompt")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeEmailPrompt(string newEmail)
        {
            string? userIdString = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                TempData["ToastMessage"] = "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Login");
            }

            if (string.IsNullOrEmpty(newEmail))
            {
                TempData["ToastMessage"] = "Vui lòng nhập email mới.";
                TempData["ToastType"] = "danger";
                ViewBag.CurrentEmail = HttpContext.Session.GetString("Email");
                return View();
            }

            var (success, message) = await _AuthenticationService.SendOtpToNewEmailAsync(newEmail);

            if (success)
            {
                TempData["ToastMessage"] = message;
                TempData["ToastType"] = "success";
                return RedirectToAction("ChangeEmailVerifyOtp", new { newEmail = newEmail, userId = userId });
            }
            else
            {
                TempData["ToastMessage"] = $" {message}";
                TempData["ToastType"] = "danger";
                ViewBag.CurrentEmail = HttpContext.Session.GetString("Email");
                return View();
            }
        }

        [HttpGet("ChangeEmailVerifyOtp")]
        public IActionResult ChangeEmailVerifyOtp(string newEmail, int userId)
        {
            if (string.IsNullOrEmpty(newEmail) || userId == 0)
            {
                return RedirectToAction("ChangeEmailPrompt");
            }
            ViewBag.NewEmail = newEmail;
            ViewBag.UserId = userId;
            return View();
        }

        [HttpPost("ChangeEmailVerifyOtp")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeEmailVerifyOtp(int userId, string newEmail, string otpCode)
        {
            if (userId == 0 || string.IsNullOrEmpty(newEmail) || string.IsNullOrEmpty(otpCode))
            {
                TempData["ToastMessage"] = "Vui lòng cung cấp đầy đủ thông tin.";
                TempData["ToastType"] = "danger";
                ViewBag.NewEmail = newEmail;
                ViewBag.UserId = userId;
                return View();
            }

            var (success, message) = await _AuthenticationService.ChangeEmailAsync(userId, newEmail, otpCode);

            if (success)
            {
                TempData["ChangeEmailSuccessMessage"] = message;
                TempData["ChangeEmailSuccessType"] = "success";

                return RedirectToAction("Logout");
            }
            else
            {
                TempData["ToastMessage"] = $" {message}";
                TempData["ToastType"] = "danger";
                ViewBag.NewEmail = newEmail;
                ViewBag.UserId = userId;
                return View();
            }
        }
    }
}