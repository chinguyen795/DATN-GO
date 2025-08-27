using DATN_GO.Models;
using DATN_GO.Service;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace DATN_GO.Areas.Admin.Controllers
{
    [Area("Admin")]

    public class UserManagerController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly DecoratesService _decorationService;
        public UserManagerController(IHttpClientFactory factory, DecoratesService decorationService)
        {
            _httpClient = factory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7096");
            _decorationService = decorationService;

        }
        public async Task<IActionResult> UserManager()
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
            var response = await _httpClient.GetAsync("api/users");
            if (!response.IsSuccessStatusCode)
            {
                return View(new List<Users>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var users = JsonConvert.DeserializeObject<List<Users>>(json);
            ViewBag.UserInfo = user;
            return View(users);

        }
    }
}