using DATN_GO.Service;
using Microsoft.AspNetCore.Mvc;
using DATN_GO.Models;
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
		public async Task<IActionResult> Register(AuthenticationService.Register request)
		{
			if (!ModelState.IsValid)
			{
				TempData["ToastMessage"] = "Dữ liệu đăng ký không hợp lệ!";
				TempData["ToastType"] = "danger";
				return View(request);
			}

			var errorMessage = await _AuthenticationService.RegisterAsync(request.Identifier, request.Password, request.ConfirmPassword);

			if (string.IsNullOrEmpty(errorMessage))
			{
				TempData["ToastMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
				TempData["ToastType"] = "success";
				return RedirectToAction("Login");
			}
			else
			{
				TempData["ToastMessage"] = $"Đăng ký thất bại! Lỗi: {errorMessage}";
				TempData["ToastType"] = "danger";
				return View(request);
			}
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
			var verifyCodeResult = await _AuthenticationService.VerifyCodeAsync(identifier, code);

			if (verifyCodeResult)
			{
				TempData["ToastMessage"] = "✅ Mã xác thực hợp lệ!";
				TempData["ToastType"] = "success";
				return RedirectToAction("ResetPassword", new { identifier });
			}
			else
			{
				TempData["ToastMessage"] = "❌ Mã xác thực không đúng!";
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