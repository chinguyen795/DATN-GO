using DATN_GO.Models;
using DATN_GO.Service;
using DATN_GO.Services;
using DATN_GO.ViewModels;
using DATN_GO.ViewModels.Decorates;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DATN_GO.Controllers
{
    public class HomeController : Controller
    {
        private readonly StoreService _storeService;
        private readonly ProductService _productService;
        private readonly CategoryService _categoryService;
        private readonly DecoratesService _decorationService;

        public HomeController(StoreService storeService, ProductService productService, CategoryService categoryService, DecoratesService decorationService)
        {
            _storeService = storeService;
            _productService = productService;
            _categoryService = categoryService;
            _decorationService = decorationService;
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

            // ⭐ Lấy rating theo Reviews, group theo UserId (chủ shop)
            var storeRatingMap = await _storeService.GetRatingsByStoreUserAsync();
            // storeRatingMap: Dictionary<int UserId, (double AvgRating, int ReviewCount)>

            // 3) Sản phẩm
            var products = await _productService.GetAllProductsAsync() ?? new();

            // ⭐ Lấy rating sản phẩm từ Reviews theo ProductId
            var prodRatingMap = await _storeService.GetProductRatingsAsync();
            // prodRatingMap: Dictionary<int ProductId, (double Avg, int Count)>

            // 4) Đếm sản phẩm theo danh mục
            var categoryProductCounts = products
                .GroupBy(p => p.CategoryId)
                .ToDictionary(g => g.Key, g => g.Count());

            // 5) Top 4 danh mục xu hướng
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

            // 6) Lấy decorate GLOBAL từ API
            var decorate = await _decorationService.GetGlobalDecorateAsync();
            var decorateVm = decorate == null
                ? new DecoratesViewModel()
                : new DecoratesViewModel
                {
                    Id = decorate.Id,
                    Slide1Path = decorate.Slide1,
                    Slide2Path = decorate.Slide2,
                    Slide3Path = decorate.Slide3,
                    Slide4Path = decorate.Slide4,
                    Slide5Path = decorate.Slide5,
                    TitleSlide1 = decorate.TitleSlide1,
                    DescriptionSlide1 = decorate.DescriptionSlide1,
                    TitleSlide2 = decorate.TitleSlide2,
                    DescriptionSlide2 = decorate.DescriptionSlide2,
                    TitleSlide3 = decorate.TitleSlide3,
                    DescriptionSlide3 = decorate.DescriptionSlide3,
                    TitleSlide4 = decorate.TitleSlide4,
                    DescriptionSlide4 = decorate.DescriptionSlide4,
                    TitleSlide5 = decorate.TitleSlide5,
                    DescriptionSlide5 = decorate.DescriptionSlide5,
                    Image1Path = decorate.Image1,
                    Image2Path = decorate.Image2,
                    VideoPath = decorate.Video,
                    Title1 = decorate.Title1,
                    Description1 = decorate.Description1,
                    Title2 = decorate.Title2,
                    Description2 = decorate.Description2
                };

            // 7) Build ViewModel
            var vm = new HomeViewModel
            {
                // ⭐ Rating của store lấy từ Reviews theo UserId; nếu không có review → -1
                Stores = stores.Take(4).Select(s =>
                {
                    float rating = -1f;
                    if (s.UserId > 0 &&
                        storeRatingMap.TryGetValue(s.UserId, out var stat) &&
                        stat.ReviewCount > 0)
                    {
                        rating = (float)Math.Round(stat.AvgRating, 1);
                    }

                    return new StoreHomeViewModel
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Address = s.Address,
                        Avatar = string.IsNullOrEmpty(s.Avatar) ? "/images/no-store.png" : s.Avatar,
                        Rating = rating, // View sẽ kiểm tra < 0 để hiện "Chưa có đánh giá"
                        Status = "Mở cửa"
                    };
                }).ToList(),

                FeaturedProducts = products
                    .OrderByDescending(p => p.Views).Take(8)
                    .Select(p =>
                    {
                        float prodAvg = 0f;
                        if (prodRatingMap.TryGetValue(p.Id, out var pr))
                            prodAvg = (float)Math.Round(pr.Avg, 1);

                        return new ProductHomeViewModel
                        {
                            Id = p.Id,
                            Name = p.Name,
                            MainImage = string.IsNullOrEmpty(p.MainImage) ? "/images/no-image.png" : p.MainImage,
                            CategoryName = categoryDict.TryGetValue(p.CategoryId, out var cname) ? cname : "Chưa phân loại",
                            StoreName = storeDict.TryGetValue(p.StoreId, out var sname) ? sname : "Đang cập nhật",
                            Price = p.CostPrice ?? 0,
                            Rating = prodAvg // ⭐ đổ sao từ Reviews
                        };
                    }).ToList(),

                SuggestedProducts = products
                    .OrderBy(_ => Guid.NewGuid()).Take(8)
                    .Select(p =>
                    {
                        float prodAvg = 0f;
                        if (prodRatingMap.TryGetValue(p.Id, out var pr))
                            prodAvg = (float)Math.Round(pr.Avg, 1);

                        return new ProductHomeViewModel
                        {
                            Id = p.Id,
                            Name = p.Name,
                            MainImage = string.IsNullOrEmpty(p.MainImage) ? "/images/no-image.png" : p.MainImage,
                            CategoryName = categoryDict.TryGetValue(p.CategoryId, out var cname) ? cname : "Chưa phân loại",
                            StoreName = storeDict.TryGetValue(p.StoreId, out var sname) ? sname : "Đang cập nhật",
                            Price = p.CostPrice ?? 0,
                            Rating = prodAvg // ⭐ đổ sao từ Reviews
                        };
                    }).ToList(),

                Categories = visibleCategories.Select(c => new CategoryHomeViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Image = string.IsNullOrEmpty(c.Image) ? "/images/no-image.png" : c.Image
                }).ToList(),

                TrendCategories = trendCategories,
                Decorate = decorateVm
            };

            // 8) Dict giá (non-variant) cho các block cần quick add
            var minMaxPriceDict = new System.Collections.Hashtable();
            foreach (var p in products)
            {
                decimal price = p.CostPrice ?? 0m;
                minMaxPriceDict[p.Id] = new
                {
                    IsVariant = false,
                    MinPrice = (decimal?)null,
                    MaxPrice = (decimal?)null,
                    Price = price
                };
            }
            ViewBag.MinMaxPriceDict = minMaxPriceDict;

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