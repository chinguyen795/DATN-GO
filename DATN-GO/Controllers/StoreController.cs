using DATN_GO.Models;
using DATN_GO.Service;
using DATN_GO.ViewModels.Store;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Linq;

namespace DATN_GO.Controllers
{
    public class StoreController : Controller
    {
        private readonly HttpClient _http;
        private readonly StoreService _storeService;

        public StoreController(IHttpClientFactory factory, StoreService storeService)
        {
            _http = factory.CreateClient();
            _http.BaseAddress = new Uri("https://localhost:7096");
            _storeService = storeService;
        }

        public async Task<IActionResult> Store(string search)
        {
            var storeEntities = await _storeService.GetAllStoresAsync();

            // Chỉ cho phép hiển thị Active & Inactive
            var allowed = new[] { StoreStatus.Active };
            storeEntities = storeEntities
                .Where(s => allowed.Contains(s.Status))
                .ToList();

            if (storeEntities == null || storeEntities.Count == 0)
            {
                Console.WriteLine("[DEBUG] Không có store nào.");
                return View(new List<StoreViewModel>());
            }
            // ⭐ rating theo UserId (từ Reviews)
            var ratingMap = await _storeService.GetRatingsByStoreUserAsync();

            // 🔢 tổng sản phẩm theo StoreId (Products)
            var productCounts = await _storeService.GetTotalProductsByStoreAsync(onlyApproved: false);

            // 🧾 tổng sản phẩm đã bán theo StoreId (Reviews)
            var soldCounts = await _storeService.GetTotalSoldProductsByStoreAsync();



            var quantities = await _storeService.GetStoreQuantitiesAsync();

            var storeViewModels = storeEntities.Select(store =>
            {
                var matched = quantities.FirstOrDefault(q => q.StoreId == store.Id);
                return new StoreViewModel
                {
                    Id = store.Id,
                    UserId = store.UserId,
                    Name = store.Name,
                    RepresentativeName = store.RepresentativeName,
                    Address = store.Address,
                    Latitude = store.Latitude,
                    Longitude = store.Longitude,
                    Avatar = store.Avatar,
                    Status = store.Status,
                    Slug = store.Slug,
                    CoverPhoto = store.CoverPhoto,
                    Bank = store.Bank,
                    BankAccount = store.BankAccount,
                    BankAccountOwner = store.BankAccountOwner,
                    
                    CreateAt = store.CreateAt,
                    UpdateAt = store.UpdateAt,
                    TotalProductQuantity = matched?.TotalProductQuantity ?? 0,
                   
                };
            }).ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                storeViewModels = storeViewModels
                    .Where(x => x.Name != null && x.Name.ToLower().Contains(s))
                    .ToList();
            }

            ViewBag.Search = search;
            return View(storeViewModels);
        }




        public async Task<IActionResult> Detail(int id, string? search)
        {
            var storeResponse = await _http.GetAsync($"/api/Stores/{id}");
            if (!storeResponse.IsSuccessStatusCode)
                return NotFound();

            var storeJson = await storeResponse.Content.ReadAsStringAsync();
            var store = JsonConvert.DeserializeObject<StoreAdminViewModel>(storeJson);

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