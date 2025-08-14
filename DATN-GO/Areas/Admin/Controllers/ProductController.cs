using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using DATN_GO.Models;
using DATN_GO.ViewModels;
using DATN_GO.ViewModels.Store;

namespace DATN_GO.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
        {
            private readonly HttpClient _http;

        public ProductController(IHttpClientFactory factory)
        {
            _http = factory.CreateClient();
            _http.BaseAddress = new Uri("https://localhost:7096");
        }

        public async Task<IActionResult> Index()
        {
            var response = await _http.GetAsync("/api/Products/PendingApproval");
            if (!response.IsSuccessStatusCode)
                return View(new List<ProductAdminViewModel>());

            var json = await response.Content.ReadAsStringAsync();
            var products = JsonConvert.DeserializeObject<List<ProductAdminViewModel>>(json);
            return View(products);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var res = await _http.PutAsync($"/api/Products/approve/{id}", null);
            return res.IsSuccessStatusCode ? Ok() : StatusCode(500);
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            var res = await _http.PutAsync($"/api/Products/reject/{id}", null);
            return res.IsSuccessStatusCode ? Ok() : StatusCode(500);
        }
    }
}