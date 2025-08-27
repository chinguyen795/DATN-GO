using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using DATN_GO.Models;
using DATN_GO.ViewModels.Store;
using DATN_GO.Service;
using DATN_GO.ViewModels;

namespace DATN_GO.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class StoreController : Controller
    {
        private readonly HttpClient _http;
        private readonly StoreService _storeService;
        private readonly DecoratesService _decorationService;


        public StoreController(IHttpClientFactory factory, StoreService storeService, DecoratesService decorationService)
        {
            _http = factory.CreateClient();
            _http.BaseAddress = new Uri("https://localhost:7096"); // <-- sửa lại URL nếu cần
            _storeService = storeService;
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
            var stores = await _storeService.GetAllAdminStoresAsync();
            ViewBag.UserInfo = user; 
            return View(stores ?? new List<AdminStorelViewModels>());
        }
        // Hiển thị danh sách cửa hàng chờ duyệt
        public async Task<IActionResult> BrowseStore()
        {
            var response = await _http.GetAsync("/api/Stores/PendingApproval");
            if (!response.IsSuccessStatusCode)
                return View(new List<StoreAdminViewModel>());

            var json = await response.Content.ReadAsStringAsync();
            var stores = JsonConvert.DeserializeObject<List<StoreAdminViewModel>>(json);

            return View(stores);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var res = await _http.PutAsync($"/api/Stores/approve/{id}", null);
            if (res.IsSuccessStatusCode)
            {
                return Json(new { success = true, message = "Cửa hàng đã được duyệt thành công!" });
            }

            return Json(new { success = false, message = "Đã xảy ra lỗi khi duyệt cửa hàng!" });
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            var res = await _http.PutAsync($"/api/Stores/reject/{id}", null);
            if (res.IsSuccessStatusCode)
            {
                return Json(new { success = true, message = "Cửa hàng đã bị từ chối!" });
            }

            return Json(new { success = false, message = "Đã xảy ra lỗi khi từ chối cửa hàng!" });
        }


        [HttpGet]
        public async Task<IActionResult> GetDetail(int id)
        {
            var response = await _http.GetAsync($"/api/Stores/admin/{id}");
            if (!response.IsSuccessStatusCode)
                return NotFound();

            var json = await response.Content.ReadAsStringAsync();
            var store = JsonConvert.DeserializeObject<AdminStorelViewModels>(json);

            return Json(store); // trả JSON cho Ajax
        }


    }
}