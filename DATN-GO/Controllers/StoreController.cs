using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using DATN_GO.Models;
using System.Linq;
using DATN_GO.ViewModels.Store;

namespace DATN_GO.Controllers
{
    public class StoreController : Controller
    {
        private readonly HttpClient _http;

        public StoreController(IHttpClientFactory factory)
        {
            _http = factory.CreateClient();
            _http.BaseAddress = new Uri("https://localhost:7096"); // Ensure the API URL is correct
        }

        // Display list of active stores only
        public async Task<IActionResult> Store(string search)
        {
            var response = await _http.GetAsync("/api/Stores");
            if (!response.IsSuccessStatusCode)
                return View(new List<StoreAdminViewModel>());

            var json = await response.Content.ReadAsStringAsync();
            var stores = JsonConvert.DeserializeObject<List<StoreAdminViewModel>>(json);

            // Lọc theo trạng thái active
            var activeStores = stores.Where(store => store.Status == StoreStatus.Active).ToList();

            // Nếu có chuỗi tìm kiếm, lọc theo tên
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower(); // Convert to lowercase để tìm chính xác hơn
                activeStores = activeStores
                    .Where(store => store.Name != null && store.Name.ToLower().Contains(search))
                    .ToList();
            }

            return View(activeStores);
        }


        public async Task<IActionResult> Detail(int id, string? search)
        {
            var storeResponse = await _http.GetAsync($"/api/Stores/{id}");
            if (!storeResponse.IsSuccessStatusCode)
                return NotFound();

            var storeJson = await storeResponse.Content.ReadAsStringAsync();
            var store = JsonConvert.DeserializeObject<StoreAdminViewModel>(storeJson);

            if (store.Status != StoreStatus.Active)
                return RedirectToAction("Diner");

            var productResponse = await _http.GetAsync($"/api/Products/store/{id}");
            if (!productResponse.IsSuccessStatusCode)
                return View(store);

            var productJson = await productResponse.Content.ReadAsStringAsync();
            var products = JsonConvert.DeserializeObject<List<ProductAdminViewModel>>(productJson);

            // ✨ Thêm filter sản phẩm tại đây
            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.ToLower();
                products = products
                    .Where(p => !string.IsNullOrEmpty(p.Name) && p.Name.ToLower().Contains(keyword))
                    .ToList();
            }

            var voucherResponse = await _http.GetAsync($"/api/vouchers/shop/{id}");
            List<Vouchers> vouchers = new();

            if (voucherResponse.IsSuccessStatusCode)
            {
                var voucherJson = await voucherResponse.Content.ReadAsStringAsync();
                vouchers = JsonConvert.DeserializeObject<List<Vouchers>>(voucherJson);
            }

            store.Vouchers = vouchers;

            store.Products = products;

            return View(store);
        }

        public async Task<IActionResult> Voucher(string id)
        {
            var productResponse = await _http.GetAsync($"/api/vouchers/shop/{id}");
            var result = await productResponse.Content.ReadAsStringAsync();
            return Json(result);
        }
    }
}