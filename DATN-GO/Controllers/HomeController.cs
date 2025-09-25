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
        private readonly PriceService _priceService;
        private readonly ProductVariantService _productVariantService;

        public HomeController(StoreService storeService, ProductService productService, CategoryService categoryService, DecoratesService decorationService, PriceService priceService, ProductVariantService productVariantService)
        {
            _storeService = storeService;
            _productService = productService;
            _categoryService = categoryService;
            _decorationService = decorationService;
            _priceService = priceService;
            _productVariantService = productVariantService;
        }

        public async Task<IActionResult> Index()
        {
            // Lấy trạng thái kênh người bán (nếu có)
            if (HttpContext.Session.TryGetValue("Id", out var idBytes)
                && int.TryParse(System.Text.Encoding.UTF8.GetString(idBytes), out var userId))
            {
                var store = await _storeService.GetStoreByUserIdAsync(userId);
                ViewData["StoreStatus"] = store?.Status; // enum StoreStatus
            }

            // 1) Danh mục (Visible)
            var (_, categories, _) = await _categoryService.GetAllCategoriesAsync();
            categories ??= new List<Categories>();
            var visibleCategories = categories.Where(c => c.Status == CategoryStatus.Visible).ToList();
            var categoryDict = visibleCategories.ToDictionary(c => c.Id, c => c.Name);

            // 2) Cửa hàng
            var stores = await _storeService.GetAllStoresAsync() ?? new();
            var storeDict = stores.ToDictionary(s => s.Id, s => s.Name);

            // 3) Rating
            var storeRatingMap = await _storeService.GetRatingsByStoreUserAsync();
            var products = await _productService.GetAllProductsAsync() ?? new();
            var prodRatingMap = await _storeService.GetProductRatingsAsync();

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

            // 6) Chọn nguồn dữ liệu
            const int FEATURED_MAX = 200;
            const int SUGGESTED_MAX = 8;

            var featuredSource = products
                .OrderByDescending(p => p.Views)
                .Take(FEATURED_MAX)
                .ToList();

            // lấy tất cả sản phẩm, KHÔNG random
            var suggestedSource = products.ToList();

            // 6.1) Lấy giá đơn
            var allNeededProductIds = featuredSource
                .Select(p => p.Id)
                .Concat(suggestedSource.Select(p => p.Id))
                .Distinct()
                .ToList();

            var priceDict = new Dictionary<int, decimal>(allNeededProductIds.Count);
            foreach (var productId in allNeededProductIds)
            {
                var price = await _priceService.GetPriceByProductIdAsync(productId);
                priceDict[productId] = price ?? 0m;
            }

            // 7) Decor
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

            // 8) Build ViewModel

            // Stores
            var storeCards = stores.Take(4).Select(s =>
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
                    Ward = s.Ward,
                    District = s.District,
                    Province = s.Province,
                    Avatar = string.IsNullOrEmpty(s.Avatar) ? "/images/no-store.png" : s.Avatar,
                    Rating = rating,
                    Status = "Mở cửa"
                };
            }).ToList();

            // FeaturedProducts
            var featuredCards = new List<ProductHomeViewModel>();
            foreach (var p in featuredSource)
            {
                float prodAvg = 0f;
                if (prodRatingMap.TryGetValue(p.Id, out var pr))
                    prodAvg = (float)Math.Round(pr.Avg, 1);

                var productVariants = await _productVariantService.GetByProductIdAsync(p.Id);

                MinMaxPriceResponse info;
                if (productVariants != null && productVariants.Any())
                {
                    decimal min = productVariants.Min(v => v.Price);
                    decimal max = productVariants.Max(v => v.Price);

                    info = new MinMaxPriceResponse
                    {
                        IsVariant = true,
                        MinPrice = min,
                        MaxPrice = max,
                        Price = min,
                        OriginalPrice = max
                    };
                }
                else
                {
                    var fallback = priceDict.TryGetValue(p.Id, out var v) ? v : (p.CostPrice ?? 0m);
                    info = new MinMaxPriceResponse
                    {
                        IsVariant = false,
                        Price = fallback,
                        OriginalPrice = null
                    };
                }

                var displayPrice = info.MinPrice ?? info.Price ?? 0m;

                featuredCards.Add(new ProductHomeViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    MainImage = string.IsNullOrEmpty(p.MainImage) ? "/images/no-image.png" : p.MainImage,
                    CategoryName = categoryDict.TryGetValue(p.CategoryId, out var cname) ? cname : "Chưa phân loại",
                    StoreName = storeDict.TryGetValue(p.StoreId, out var sname) ? sname : "Đang cập nhật",
                    Price = displayPrice,
                    Rating = prodAvg,
                    PriceInfo = info,
                    PurchaseCount = p.OrderDetails?.Sum(od => od.Quantity) ?? 0
                });
            }

            // SuggestedProducts
            var suggestedCards = new List<ProductHomeViewModel>();
            foreach (var p in suggestedSource)
            {
                float prodAvg = 0f;
                if (prodRatingMap.TryGetValue(p.Id, out var pr))
                    prodAvg = (float)Math.Round(pr.Avg, 1);

                var productVariants = await _productVariantService.GetByProductIdAsync(p.Id);

                MinMaxPriceResponse info;
                if (productVariants != null && productVariants.Any())
                {
                    decimal min = productVariants.Min(v => v.Price);
                    decimal max = productVariants.Max(v => v.Price);

                    info = new MinMaxPriceResponse
                    {
                        IsVariant = true,
                        MinPrice = min,
                        MaxPrice = max,
                        Price = min,
                        OriginalPrice = max
                    };
                }
                else
                {
                    var fallback = priceDict.TryGetValue(p.Id, out var v) ? v : (p.CostPrice ?? 0m);
                    info = new MinMaxPriceResponse
                    {
                        IsVariant = false,
                        Price = fallback,
                        OriginalPrice = null
                    };
                }

                var displayPrice = info.MinPrice ?? info.Price ?? 0m;

                suggestedCards.Add(new ProductHomeViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    MainImage = string.IsNullOrEmpty(p.MainImage) ? "/images/no-image.png" : p.MainImage,
                    CategoryName = categoryDict.TryGetValue(p.CategoryId, out var cname) ? cname : "Chưa phân loại",
                    StoreName = storeDict.TryGetValue(p.StoreId, out var sname) ? sname : "Đang cập nhật",
                    Price = displayPrice,
                    Rating = prodAvg,
                    PriceInfo = info,
                    PurchaseCount = p.OrderDetails?.Sum(od => od.Quantity) ?? 0
                });
            }

            // 👉 Sort Suggested theo lượt mua ↓, tie-break rating ↓, KHÔNG random
            var suggestedCardsTop = suggestedCards
                .OrderByDescending(x => x.PurchaseCount)
                .ThenByDescending(x => x.Rating)
                .Take(SUGGESTED_MAX)
                .ToList();

            var vm = new HomeViewModel
            {
                Stores = storeCards,
                FeaturedProducts = featuredCards,
                SuggestedProducts = suggestedCardsTop,
                Categories = visibleCategories.Select(c => new CategoryHomeViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Image = string.IsNullOrEmpty(c.Image) ? "/images/no-image.png" : c.Image
                }).ToList(),
                TrendCategories = trendCategories,
                Decorate = decorateVm
            };

            return View(vm);
        }










        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return View(new SearchhViewModel
                {
                    Query = query,
                    Products = new List<ProductHomeViewModel>(),
                    Stores = new List<StoreHomeViewModel>()
                });
            }

            var stores = await _storeService.GetAllStoresAsync() ?? new();
            var storeDict = stores.ToDictionary(s => s.Id, s => s.Name);

            var products = await _productService.GetAllProductsAsync() ?? new();
            var prodRatingMap = await _storeService.GetProductRatingsAsync();

            var matchedProducts = new List<ProductHomeViewModel>();

            foreach (var p in products.Where(p =>
                         !string.IsNullOrEmpty(p.Name) &&
                         p.Name.Contains(query, StringComparison.OrdinalIgnoreCase)))
            {
                float rating = 0f;
                if (prodRatingMap.TryGetValue(p.Id, out var pr))
                    rating = (float)Math.Round(pr.Avg, 1);

                // === xử lý Price với variant ===
                var productVariants = await _productVariantService.GetByProductIdAsync(p.Id);

                MinMaxPriceResponse info;
                if (productVariants != null && productVariants.Any())
                {
                    decimal min = productVariants.Min(v => v.Price);
                    decimal max = productVariants.Max(v => v.Price);

                    info = new MinMaxPriceResponse
                    {
                        IsVariant = true,
                        MinPrice = min,
                        MaxPrice = max,
                        Price = min,
                        OriginalPrice = max
                    };
                }
                else
                {
                    var fallback = p.CostPrice ?? 0m;
                    info = new MinMaxPriceResponse
                    {
                        IsVariant = false,
                        Price = fallback
                    };
                }

                var displayPrice = info.MinPrice ?? info.Price ?? 0m;

                matchedProducts.Add(new ProductHomeViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    MainImage = string.IsNullOrEmpty(p.MainImage) ? "/images/no-image.png" : p.MainImage,
                    CategoryName = p.Category?.Name ?? "Chưa phân loại",
                    StoreName = storeDict.TryGetValue(p.StoreId, out var sname) ? sname : "Không rõ cửa hàng",
                    Price = displayPrice,
                    Rating = rating,
                    PriceInfo = info
                });
            }

            var matchedStores = stores
                .Where(s => !string.IsNullOrEmpty(s.Name) &&
                            s.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(s => new StoreHomeViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Ward = s.Ward,
                    District = s.District,
                    Province = s.Province,
                    Avatar = string.IsNullOrEmpty(s.Avatar) ? "/images/no-store.png" : s.Avatar,
                    Rating = s.Rating < 0 ? 0 : s.Rating,
                    Status = "Mở cửa"
                }).ToList();

            var vm = new SearchhViewModel
            {
                Query = query,
                Products = matchedProducts,
                Stores = matchedStores
            };

            return View(vm);
        }

    }
}