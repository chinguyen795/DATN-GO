using DATN_GO.Models;
using DATN_GO.Service;
using DATN_GO.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using static DATN_GO.Services.CategoryService;

namespace DATN_GO.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoryController : Controller
    {
        private readonly CategoryService _categoryService;
        private readonly DecoratesService _decorationService;
        public CategoryController(CategoryService categoryService, DecoratesService decorationService)
        {
            _categoryService = categoryService;
            _decorationService = decorationService;
        }

        public async Task<IActionResult> Index()
        {
            // vẫn yêu cầu đăng nhập
            var userIdStr = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                TempData["ToastMessage"] = "Vui lòng đăng nhập để tiếp tục!";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            // vẫn khóa admin
            var user = await _decorationService.GetUserByIdAsync(userId);
            if (user == null || user.RoleId != 3)
            {
                TempData["ToastMessage"] = "Bạn không có quyền truy cập vào trang này!";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index", "Home", new { area = "" });
            }
            var (success, data, message) = await _categoryService.GetAllWithUsageAsync();

            if (!success)
            {
                TempData["ToastMessage"] = message;
                TempData["ToastType"] = "error";
            }
            ViewBag.UserInfo = user; 
            return View(data);
        }
        [HttpPost]
        public async Task<IActionResult> Create(CreateCategoryRequest request, IFormFile? imageFile)
        {
            if (!IsValidName(request.Name))
            {
                TempData["ToastMessage"] = "Tên danh mục không được chứa số hoặc ký tự đặc biệt.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index");
            }

            if (!IsValidHashtag(request.Hashtag))
            {
                TempData["ToastMessage"] = "Hashtag không hợp lệ. Không được chứa khoảng trắng hoặc ký tự đặc biệt.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index");
            }

            if (imageFile != null)
            {
                using var ms = new MemoryStream();
                await imageFile.CopyToAsync(ms);
                var base64 = Convert.ToBase64String(ms.ToArray());
                request.Image = $"data:{imageFile.ContentType};base64,{base64}";
            }

            var (success, _, message) = await _categoryService.CreateCategoryAsync(request);

            if (success)
            {
                TempData["ToastMessage"] = $"Đã thêm danh mục \"{request.Name}\" thành công!";
                TempData["ToastType"] = "success";
            }
            else
            {
                TempData["ToastMessage"] = message;
                TempData["ToastType"] = "error";
            }

            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> Edit(UpdateCategoryRequest request, IFormFile? imageFile)
        {
            if (!IsValidName(request.Name))
            {
                TempData["ToastMessage"] = "Tên danh mục không được chứa số hoặc ký tự đặc biệt.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index");
            }

            if (!IsValidHashtag(request.Hashtag))
            {
                TempData["ToastMessage"] = "Hashtag không hợp lệ. Không được chứa khoảng trắng hoặc ký tự đặc biệt.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index");
            }

            if (imageFile != null)
            {
                using var ms = new MemoryStream();
                await imageFile.CopyToAsync(ms);
                var base64 = Convert.ToBase64String(ms.ToArray());
                request.Image = $"data:{imageFile.ContentType};base64,{base64}";
            }

            var (success, message) = await _categoryService.UpdateCategoryAsync(request.Id, request);
            TempData["ToastMessage"] = message;
            TempData["ToastType"] = success ? "success" : "error";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var (success, message) = await _categoryService.DeleteCategoryAsync(id);
            TempData["ToastMessage"] = message;
            TempData["ToastType"] = success ? "success" : "error";
            return RedirectToAction("Index");
        }

        private bool IsValidName(string name)
        {
            return !string.IsNullOrWhiteSpace(name) &&
                   Regex.IsMatch(name, @"^[\p{L} ]+$", RegexOptions.Compiled); // chỉ chữ cái và khoảng trắng
        }

        private bool IsValidHashtag(string? hashtag)
        {
            if (string.IsNullOrEmpty(hashtag)) return true;
            return Regex.IsMatch(hashtag, @"^#[a-zA-Z0-9_]+$", RegexOptions.Compiled);
        }
        // Logout
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            TempData["ToastMessage"] = "Bạn đã đăng xuất thành công!";
            TempData["ToastType"] = "success";

            return RedirectToAction("Index", "Home", new { area = "" });
        }
    }
}