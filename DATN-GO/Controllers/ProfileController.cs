using DATN_GO.Models;
using DATN_GO.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;

namespace DATN_GO.Controllers
{
    public class ProfileController : Controller
    {
        private readonly UserService _userService;
        private readonly GoogleCloudStorageService _gcsService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(UserService userService, GoogleCloudStorageService gcsService, ILogger<ProfileController> logger)
        {
            _userService = userService;
            _gcsService = gcsService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!HttpContext.Session.TryGetValue("Id", out byte[] idBytes) || !int.TryParse(System.Text.Encoding.UTF8.GetString(idBytes), out int userId))
            {
                _logger.LogWarning("User ID not found in session or invalid. Redirecting to Login.");
                TempData["ToastMessage"] = "Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Login", "UserAuthentication");
            }

            var user = await _userService.GetUserByIdAsync(userId);

            if (user == null)
            {
                _logger.LogError($"User with ID {userId} not found via API.");
                TempData["ToastMessage"] = "Không tìm thấy thông tin người dùng.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index", "Home");
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Users model, IFormFile? avatarFile)
        {
            if (!HttpContext.Session.TryGetValue("Id", out byte[] idBytes) || !int.TryParse(System.Text.Encoding.UTF8.GetString(idBytes), out int userId))
            {
                _logger.LogWarning("User ID not found in session during profile update.");
                TempData["ToastMessage"] = "Bạn chưa đăng nhập.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Login", "UserAuthentication");
            }

            var currentUser = await _userService.GetUserByIdAsync(userId);
            if (currentUser == null)
            {
                _logger.LogError($"Current user with ID {userId} not found for update.");
                TempData["ToastMessage"] = "Không tìm thấy thông tin người dùng để cập nhật.";
                TempData["ToastType"] = "danger";
                return View("Index", model);
            }

            model.Id = userId;

            if (model.Id != userId)
            {
                _logger.LogWarning($"User ID mismatch: Session ID {userId}, Form ID {model.Id}");
                TempData["ToastMessage"] = "Lỗi bảo mật: Thông tin người dùng không khớp.";
                TempData["ToastType"] = "danger";
                return View("Index", model);
            }
            if (model.BirthDay.HasValue && model.BirthDay.Value > DateTime.Today)
            {
                TempData["ToastMessage"] = "❌ Ngày sinh không được lớn hơn ngày hiện tại!";
                TempData["ToastType"] = "danger";
                return View("Index", model);
            }
            string? newAvatarUrl = currentUser.Avatar;
            if (avatarFile != null && avatarFile.Length > 0)
            {
                newAvatarUrl = await _gcsService.UploadFileAsync(avatarFile, "avatars/");
                if (string.IsNullOrEmpty(newAvatarUrl))
                {
                    _logger.LogError($"Failed to upload new avatar for user {userId}.");
                    TempData["ToastMessage"] = "Tải ảnh đại diện thất bại. Vui lòng thử lại.";
                    TempData["ToastType"] = "danger";
                    return View("Index", model);
                }
                currentUser.Avatar = newAvatarUrl;
            }


            currentUser.Password = currentUser.Password;
            currentUser.Email = currentUser.Email;
            currentUser.Phone = currentUser.Phone;
            currentUser.RoleId = currentUser.RoleId;
            currentUser.CreateAt = currentUser.CreateAt;
            currentUser.Status = currentUser.Status;
            currentUser.CitizenIdentityCard = currentUser.CitizenIdentityCard;
            currentUser.Avatar = currentUser.Avatar;
            currentUser.FullName = model.FullName;
            currentUser.Gender = model.Gender;
            currentUser.BirthDay = model.BirthDay;


            var success = await _userService.UpdateUserAsync(currentUser.Id, currentUser);

            if (success)
            {
                TempData["ToastMessage"] = "Cập nhật thông tin cá nhân thành công!";
                TempData["ToastType"] = "success";
                HttpContext.Session.SetString("FullName", currentUser.FullName);
            }
            else
            {
                TempData["ToastMessage"] = "Cập nhật thông tin cá nhân thất bại. Vui lòng thử lại.";
                TempData["ToastType"] = "danger";
            }

            return View("Index", currentUser);
        }
    }
}