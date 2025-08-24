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
                double avg = store.Rating; // fallback nếu chưa có review
                int cnt = 0;
                if (ratingMap.TryGetValue(store.UserId, out var r))
                {
                    avg = r.AvgRating;
                    cnt = r.ReviewCount;
                }

                var totalProducts = productCounts.TryGetValue(store.Id, out var pc) ? pc : 0;
                var totalSold = soldCounts.TryGetValue(store.Id, out var sc) ? sc : 0;

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

                    AverageRating = Math.Round(avg, 1),
                    ReviewCount = cnt,

                    CreateAt = store.CreateAt,
                    UpdateAt = store.UpdateAt,

                    TotalProductQuantity = totalProducts,
                    TotalSoldProducts = totalSold


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
            // 1) Store
            var storeResponse = await _http.GetAsync($"/api/Stores/{id}");
            if (!storeResponse.IsSuccessStatusCode)
                return NotFound();

            var storeJson = await storeResponse.Content.ReadAsStringAsync();
            var store = JsonConvert.DeserializeObject<StoreAdminViewModel>(storeJson);
            if (store == null)
                return NotFound();

            // 2) Products by store
            var productResponse = await _http.GetAsync($"/api/Products/store/{id}");
            if (productResponse.IsSuccessStatusCode)
            {
                var productJson = await productResponse.Content.ReadAsStringAsync();
                var products = JsonConvert.DeserializeObject<List<ProductAdminViewModel>>(productJson) ?? new();

                // Optional filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var keyword = search.ToLower();
                    products = products
                        .Where(p => !string.IsNullOrEmpty(p.Name) && p.Name.ToLower().Contains(keyword))
                        .ToList();
                }

                store.Products = products;
            }

            // 3) Vouchers
            var voucherResponse = await _http.GetAsync($"/api/vouchers/shop/{id}");
            if (voucherResponse.IsSuccessStatusCode)
            {
                var voucherJson = await voucherResponse.Content.ReadAsStringAsync();
                store.Vouchers = JsonConvert.DeserializeObject<List<Vouchers>>(voucherJson) ?? new();
            }

            // 4) Tổng sản phẩm đã bán (từ reviews)
            var soldMap = await _storeService.GetTotalSoldProductsByStoreAsync();
            store.TotalSoldProducts = soldMap.TryGetValue(id, out var sold) ? sold : 0;

            // 5) ⭐ Rating STORE theo user (1 user = 1 store) — dùng chung hàm đã có
            var storeRatingMap = await _storeService.GetRatingsByStoreUserAsync();
            if (store.UserId > 0 && storeRatingMap.TryGetValue(store.UserId, out var stat))
            {
                store.AverageRating = Math.Round(stat.AvgRating, 1);
                store.ReviewCount = stat.ReviewCount;
            }
            else
            {
                store.AverageRating = 0;
                store.ReviewCount = 0;
            }

            // 6) ⭐ Rating PRODUCT: lấy avg theo ProductId, gán vào product.Rating
            var prodRatingMap = await _storeService.GetProductRatingsAsync();
            foreach (var p in store.Products)
            {
                if (prodRatingMap.TryGetValue(p.Id, out var pr))
                {
                    p.Rating = (float)Math.Round(pr.Avg, 1);
                    // Nếu ProductAdminViewModel có ReviewCount thì bật dòng này:
                    // p.ReviewCount = pr.Count;
                }
                else
                {
                    p.Rating = 0;
                    // p.ReviewCount = 0;
                }
            }

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