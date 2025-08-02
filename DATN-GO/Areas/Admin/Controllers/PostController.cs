using DATN_GO.Models;
using DATN_GO.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace DATN_GO.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PostController : Controller
    {
        private readonly HttpClient _http;

        public PostController(IHttpClientFactory factory)
        {
            _http = factory.CreateClient();
            _http.BaseAddress = new Uri("https://localhost:7096"); // API URL
        }

        // Action to display pending posts
        public async Task<IActionResult> Index()
        {
            var res = await _http.GetAsync("/api/posts/pending");
            if (!res.IsSuccessStatusCode)
                return View(new List<PostAdminViewModel>());

            var json = await res.Content.ReadAsStringAsync();
            var posts = JsonConvert.DeserializeObject<List<PostAdminViewModel>>(json);
            return View(posts);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var res = await _http.PutAsync($"/api/posts/approve/{id}", null);
            if (res.IsSuccessStatusCode)
            {
                return Json(new { success = true, message = " Bài viết đã được duyệt! " });
            }

            return Json(new { success = false, message = " Lỗi khi duyệt bài viết!" });
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            var res = await _http.PutAsync($"/api/posts/reject/{id}", null);
            if (res.IsSuccessStatusCode)
            {
                return Json(new { success = true, message = " Bài viết đã bị từ chối!" });
            }

            return Json(new { success = false, message = " Lỗi khi từ chối bài viết!" });
        }

    }
}