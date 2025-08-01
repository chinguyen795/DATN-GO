using Microsoft.AspNetCore.Mvc;
using DATN_GO.Service;
using DATN_GO.Models;
using static DATN_GO.Service.CategoryService;
using System.Text.RegularExpressions;

namespace DATN_GO.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoryController : Controller
    {
        private readonly CategoryService _categoryService;

        public CategoryController(CategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        public async Task<IActionResult> Index()
        {
            var (success, data, message) = await _categoryService.GetAllCategoriesAsync();
            if (!success)
            {
                TempData["ToastMessage"] = message;
                TempData["ToastType"] = "error";
            }
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
    }
}