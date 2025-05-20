using DATN_GO.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

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
        public async Task<IActionResult> Login(string identifier, string password)
        {
            var loginResult = await _AuthenticationService.LoginAsync(identifier, password);

            if (loginResult != null)
            {
                HttpContext.Session.SetString("JwtToken", loginResult.Token);
                HttpContext.Session.SetString("Id", loginResult.Id.ToString());
                HttpContext.Session.SetString("FullName", loginResult.FullName);
                HttpContext.Session.SetString("Role", loginResult.Roles.ToString());
                HttpContext.Session.SetString("Identifier", identifier);

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
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet("Register")]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                TempData["ToastMessage"] = "Vui lòng nhập email hoặc số điện thoại!";
                TempData["ToastType"] = "danger";
                return View();
            }

            var (success, message) = await _AuthenticationService.SendVerificationCodeAsync(identifier);

            if (success)
            {
                TempData["ToastMessage"] = message;
                TempData["ToastType"] = "success";
                return RedirectToAction("AuthenticationCode", new { identifier });
            }
            else
            {
                TempData["ToastMessage"] = $"❌ {message}";
                TempData["ToastType"] = "danger";
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
                TempData["ToastMessage"] = "❌ Thiếu thông tin hoặc mã OTP. Vui lòng thử lại!";
                TempData["ToastType"] = "danger";
                ViewBag.Identifier = identifier;
                return View();
            }

            var (success, message) = await _AuthenticationService.VerifyCodeAsync(identifier, code);

            if (success)
            {
                TempData["ToastMessage"] = "✅ Mã xác thực hợp lệ! " + message;
                TempData["ToastType"] = "success";
                return RedirectToAction("CreatePassword", new { identifier });
            }
            else
            {
                TempData["ToastMessage"] = $"❌ {message}";
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
                TempData["ToastMessage"] = "❌ Không tìm thấy thông tin định danh để gửi lại mã.";
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
                TempData["ToastMessage"] = $"❌ {message}";
                TempData["ToastType"] = "danger";
            }

            return RedirectToAction("AuthenticationCode", new { identifier });
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
        public async Task<IActionResult> CreatePassword(string identifier, string password, string confirmPassword)
        {
            if (string.IsNullOrEmpty(identifier))
                return RedirectToAction("Register");

            if (password != confirmPassword)
            {
                TempData["ToastMessage"] = "❌ Mật khẩu không trùng khớp!";
                TempData["ToastType"] = "danger";
                ViewBag.Identifier = identifier;
                return View();
            }

            var (success, message) = await _AuthenticationService.RegisterAsync(identifier, password, confirmPassword);

            if (success)
            {
                TempData["ToastMessage"] = "🎉 Đăng ký thành công! Vui lòng đăng nhập.";
                TempData["ToastType"] = "success";
                return RedirectToAction("index","Home");
            }
            else
            {
                TempData["ToastMessage"] = $"❌ Đăng ký thất bại: {message}";
                TempData["ToastType"] = "danger";
                ViewBag.Identifier = identifier;
                return View();
            }
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


        [HttpGet("ResetPassword")]
        public IActionResult ResetPassword(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return RedirectToAction("index", "Home");
            }
            ViewBag.Identifier = identifier;
            return View();
        }

        [HttpGet("Logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["ToastMessage"] = "✅ Bạn đã đăng xuất thành công!";
            TempData["ToastType"] = "success";
            return RedirectToAction("Index", "Home");
        }
    }
}