using DATN_GO.Models;
using DATN_GO.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System; // Thêm namespace này cho DateTime

namespace DATN_GO.Controllers
{
    public class ProfileController : Controller
    {
        private readonly UserService _userService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(UserService userService, ILogger<ProfileController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // Action này vẫn là Index (theo tên View)
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Lấy User ID từ Session
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

        // POST: /Profile/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Users model)
        {
            // Lấy User ID từ Session
            if (!HttpContext.Session.TryGetValue("Id", out byte[] idBytes) || !int.TryParse(System.Text.Encoding.UTF8.GetString(idBytes), out int userId))
            {
                _logger.LogWarning("User ID not found in session during profile update.");
                TempData["ToastMessage"] = "Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Login", "UserAuthentication");
            }

            // Lấy thông tin người dùng hiện tại từ API để bảo toàn các trường không được phép cập nhật
            var currentUser = await _userService.GetUserByIdAsync(userId);
            if (currentUser == null)
            {
                _logger.LogError($"Current user with ID {userId} not found for update.");
                TempData["ToastMessage"] = "Không tìm thấy thông tin người dùng để cập nhật.";
                TempData["ToastType"] = "danger";
             RedirectToAction("Index", "Home");
            }

            model.Id = userId;

            if (model.Id != userId)
            {
                _logger.LogWarning($"User ID mismatch: Session ID {userId}, Form ID {model.Id}");
                TempData["ToastMessage"] = "Lỗi bảo mật: Thông tin người dùng không khớp.";
                TempData["ToastType"] = "danger";
              RedirectToAction("Index", "Home");
            }

          
            model.Email = currentUser.Email;
            model.PhoneNumber = currentUser.PhoneNumber;
            model.Password = currentUser.Password; // Quan trọng: Đừng bao giờ gửi mật khẩu trực tiếp qua form!
            model.Avatar = currentUser.Avatar;
            model.RoleId = currentUser.RoleId;
            model.CreatedAt = currentUser.CreatedAt;
           
            currentUser.FullName = model.FullName;
            currentUser.Gender = model.Gender;
            currentUser.CitizenIdentityCard = model.CitizenIdentityCard;
            currentUser.Status = model.Status; // Giữ nguyên Status nếu không có trong form update
            currentUser.DateOfBirth = model.DateOfBirth; // Cập nhật ngày sinh

            if (!ModelState.IsValid)
            {
                TempData["ToastMessage"] = "Dữ liệu nhập vào không hợp lệ. Vui lòng kiểm tra lại các trường.";
                TempData["ToastType"] = "danger";
                return View("Index", model);
            }

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