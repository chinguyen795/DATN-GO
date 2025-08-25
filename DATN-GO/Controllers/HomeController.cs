using DATN_GO.Models;
using DATN_GO.Service;
using DATN_GO.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using DATN_GO.Services;

namespace DATN_GO.Controllers
{
    public class HomeController : Controller
    {
        private readonly StoreService _storeService;
        private readonly ProductService _productService;
        private readonly CategoryService _categoryService;
        private readonly PriceService _priceService;

        public HomeController(StoreService storeService, ProductService productService, CategoryService categoryService, PriceService priceService)
        {
            _storeService = storeService;
            _productService = productService;
            _categoryService = categoryService;
            _priceService = priceService;
        }

        public async Task<IActionResult> Index()
        {
            // 1) Danh mục (chỉ Visible)
            var (_, categories, _) = await _categoryService.GetAllCategoriesAsync();
            categories ??= new List<Categories>();
            var visibleCategories = categories.Where(c => c.Status == CategoryStatus.Visible).ToList();
            var categoryDict = visibleCategories.ToDictionary(c => c.Id, c => c.Name);

            // 2) Cửa hàng
            var stores = await _storeService.GetAllStoresAsync() ?? new();
            var storeDict = stores.ToDictionary(s => s.Id, s => s.Name);

            // 3) Sản phẩm
            var products = await _productService.GetAllProductsAsync() ?? new();

            // 4) Lấy giá từ bảng Price (đơn giản)
            var priceInfos = new Dictionary<int, decimal>();

            foreach (var product in products)
            {
                try
                {
                    var priceInfo = await _priceService.GetPriceByProductIdAsync(product.Id);
                    if (priceInfo.HasValue)
                    {
                        priceInfos[product.Id] = priceInfo.Value;
                    }
                    else
                    {
                        // Fallback về CostPrice nếu không có trong bảng Price
                        priceInfos[product.Id] = product.CostPrice ?? 0;
                    }
                }
                catch
                {
                    // Fallback về CostPrice nếu có lỗi
                    priceInfos[product.Id] = product.CostPrice ?? 0;
                }
            }

            // 5) Đếm sản phẩm theo danh mục (để tính xu hướng)
            var categoryProductCounts = products
                .GroupBy(p => p.CategoryId)
                .ToDictionary(g => g.Key, g => g.Count());

            // 6) Top 4 danh mục xu hướng
            var trendCategories = visibleCategories
                .OrderByDescending(c => categoryProductCounts.TryGetValue(c.Id, out var cnt) ? cnt : 0)
                .Take(4)
                .Select((c, index) => new CategoryHomeViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Image = string.IsNullOrEmpty(c.Image) ? "/images/no-image.png" : c.Image,
                    Rank = index + 1,
                    TotalProducts = categoryProductCounts.TryGetValue(c.Id, out var total) ? total : 0
                })
                .ToList();

            // 7) Build ViewModel
            var vm = new HomeViewModel
            {
                Stores = stores.Take(4).Select(s => new StoreHomeViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Address = s.Address,
                    Avatar = string.IsNullOrEmpty(s.Avatar) ? "/images/no-store.png" : s.Avatar,
                    Rating = s.Rating,
                    Status = "Mở cửa"
                }).ToList(),

                FeaturedProducts = products
                    .OrderByDescending(p => p.Views).Take(8)
                    .Select(p => new ProductHomeViewModel
                    {
                        Id = p.Id,
                        Name = p.Name,
                        MainImage = string.IsNullOrEmpty(p.MainImage) ? "/images/no-image.png" : p.MainImage,
                        CategoryName = categoryDict.TryGetValue(p.CategoryId, out var cname) ? cname : "Chưa phân loại",
                        StoreName = storeDict.TryGetValue(p.StoreId, out var sname) ? sname : "Đang cập nhật",
                        Price = priceInfos.TryGetValue(p.Id, out var price) ? price : 0,
                        Rating = p.Rating ?? 0
                    }).ToList(),

                SuggestedProducts = products
                    .OrderBy(_ => Guid.NewGuid()).Take(8)
                    .Select(p => new ProductHomeViewModel
                    {
                        Id = p.Id,
                        Name = p.Name,
                        MainImage = string.IsNullOrEmpty(p.MainImage) ? "/images/no-image.png" : p.MainImage,
                        CategoryName = categoryDict.TryGetValue(p.CategoryId, out var cname) ? cname : "Chưa phân loại",
                        StoreName = storeDict.TryGetValue(p.StoreId, out var sname) ? sname : "Đang cập nhật",
                        Price = priceInfos.TryGetValue(p.Id, out var price) ? price : 0,
                        Rating = p.Rating ?? 0
                    }).ToList(),

                Categories = visibleCategories.Select(c => new CategoryHomeViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Image = string.IsNullOrEmpty(c.Image) ? "/images/no-image.png" : c.Image
                }).ToList(),

                TrendCategories = trendCategories
            };

            return View(vm);
        }

        [HttpGet]
        // --- Search Action ---
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return View(new SearchViewModel
                {
                    Query = query,
                    Products = new List<ProductHomeViewModel>(),
                    Stores = new List<StoreHomeViewModel>()
                });
            }

            var stores = await _storeService.GetAllStoresAsync() ?? new();
            var storesDictionary = stores.ToDictionary(s => s.Id, s => s.Name);

            var products = await _productService.GetAllProductsAsync() ?? new();
            var matchedProducts = products
                .Where(p => (!string.IsNullOrEmpty(p.Name) && p.Name.Contains(query, StringComparison.OrdinalIgnoreCase)))
                .Select(p => new ProductHomeViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    MainImage = string.IsNullOrEmpty(p.MainImage) ? "/images/no-image.png" : p.MainImage,
                    CategoryName = p.Category?.Name ?? "Chưa phân loại",
                    StoreName = storesDictionary.ContainsKey(p.StoreId) ? storesDictionary[p.StoreId] : "Không rõ cửa hàng",
                    Price = p.CostPrice ?? 0,
                    Rating = p.Rating ?? 0
                }).ToList();

            var matchedStores = stores
                .Where(s => !string.IsNullOrEmpty(s.Name) && s.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(s => new StoreHomeViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Address = s.Address,
                    Avatar = string.IsNullOrEmpty(s.Avatar) ? "/images/no-store.png" : s.Avatar,
                    Rating = s.Rating,
                    Status = "Mở cửa"
                }).ToList();

            var vm = new SearchViewModel
            {
                Query = query,
                Products = matchedProducts,
                Stores = matchedStores
            };

            return View(vm);
        }
    }
}