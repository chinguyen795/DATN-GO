using DATN_GO.Service;
using Microsoft.AspNetCore.Mvc;
using DATN_GO.Models;
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
				// Lưu thông tin đăng nhập vào session
				HttpContext.Session.SetString("JwtToken", loginResult.Token);
				HttpContext.Session.SetString("Id", loginResult.Id.ToString());
				HttpContext.Session.SetString("FullName", loginResult.FullName);
				HttpContext.Session.SetString("Role", loginResult.Roles.ToString());

				TempData["ToastMessage"] = $"Chào mừng {loginResult.FullName}, bạn đã đăng nhập thành công!";
				TempData["ToastType"] = "success";

				// Điều hướng dựa trên Role (cần điều chỉnh URL cho phù hợp với ứng dụng của bạn)
				string redirectUrl = loginResult.Roles switch
				{
					1 => "/",
					2 => "/",
					_ => "/"
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

			var result = await _AuthenticationService.SendVerificationCodeAsync(identifier);
			if (!result)
			{
				TempData["ToastMessage"] = "Không thể gửi mã xác minh. Vui lòng thử lại!";
				TempData["ToastType"] = "danger";
				return View();
			}

			return RedirectToAction("AuthenticationCode", new { identifier });
		}



		[HttpGet("AuthenticationCode")]
		public IActionResult AuthenticationCode(string identifier)
		{
			ViewBag.Identifier = identifier;
			return View();
		}

        [HttpPost("AuthenticationCode")]
        public async Task<IActionResult> AuthenticationCode(string identifier, string code)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                TempData["ToastMessage"] = "❌ Đã xảy ra lỗi. Vui lòng thử lại!";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index", "Home");
            }

            var verifyCodeResult = await _AuthenticationService.VerifyCodeAsync(identifier, code);

            if (verifyCodeResult)
            {
                TempData["ToastMessage"] = "✅ Mã xác thực hợp lệ!";
                TempData["ToastType"] = "success";
                return RedirectToAction("CreatePassword", new { identifier }); // Chuyển đến trang tạo mật khẩu
            }
            else
            {
                TempData["ToastMessage"] = "❌ Mã xác thực không đúng. Vui lòng kiểm tra lại!";
                TempData["ToastType"] = "danger";
                ViewBag.Identifier = identifier;

                // Hiển thị lại thông báo đã gửi mã để người dùng không bị nhầm lẫn
                if (identifier.Contains('@'))
                {
                    ViewBag.VerificationMessage = $"Mã xác thực đã được gửi đến địa chỉ email: {identifier}";
                }
                else
                {
                    ViewBag.VerificationMessage = $"Mã xác thực đã được gửi đến số điện thoại: {identifier}";
                }

                return View();
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

			var result = await _AuthenticationService.RegisterAsync(identifier, password, confirmPassword);

			if (string.IsNullOrEmpty(result))
			{
				TempData["ToastMessage"] = "🎉 Đăng ký thành công! Vui lòng đăng nhập.";
				TempData["ToastType"] = "success";
                return RedirectToAction("Index", "Home");
            }
            else
			{
				TempData["ToastMessage"] = $"❌ Đăng ký thất bại: {result}";
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
				return RedirectToAction("ForgotPassword");
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