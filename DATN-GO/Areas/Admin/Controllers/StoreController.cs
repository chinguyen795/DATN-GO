using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using DATN_GO.Models;
using DATN_GO.ViewModels.Store;

namespace DATN_GO.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class StoreController : Controller
    {
        private readonly HttpClient _http;

        public StoreController(IHttpClientFactory factory)
        {
            _http = factory.CreateClient();
            _http.BaseAddress = new Uri("https://localhost:7096"); // <-- sửa lại URL nếu cần
        }

        public async Task<IActionResult> Index()
        {
            var response = await _http.GetAsync("/api/Stores");
            if (!response.IsSuccessStatusCode)
                return View(new List<StoreAdminViewModel>());

            var json = await response.Content.ReadAsStringAsync();
            var stores = JsonConvert.DeserializeObject<List<StoreAdminViewModel>>(json);
            return View(stores);
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


    }
}