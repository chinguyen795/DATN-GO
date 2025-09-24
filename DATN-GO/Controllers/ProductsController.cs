using Microsoft.AspNetCore.Mvc;
using DATN_GO.Services; // Đảm bảo đúng namespace chứa các service
using DATN_GO.Service;
using DATN_GO.ViewModels;
using DATN_GO.Models;
using System.Text; // Nếu có tách riêng interface

namespace DATN_GO.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ProductService _productService;
        private readonly ProductVariantService _productVariantService;
        private readonly StoreService _storeService;
        private readonly UserService _userService;
        private readonly VariantService _variantService;
        private readonly VariantValueService _variantValueService;
        private readonly VariantCompositionService _variantCompositionService;
        private readonly PriceService _priceService;
        private readonly CategoryService _categoryService;
        private readonly ReviewService _reviewService;

        public ProductsController(
            ProductService productService,
            ProductVariantService productVariantService,
            StoreService storeService,
            UserService userService,
            VariantService variantService,
            VariantValueService variantValueService,
            VariantCompositionService variantCompositionService,
            PriceService priceService,
            CategoryService categoryService,
            ReviewService reviewService)
        {
            _productService = productService;
            _productVariantService = productVariantService;
            _storeService = storeService;
            _userService = userService;
            _variantService = variantService;
            _variantValueService = variantValueService;
            _variantCompositionService = variantCompositionService;
            _priceService = priceService;
            _categoryService = categoryService;
            _reviewService = reviewService;
        }

        public async Task<IActionResult> Products()
        {
            if (HttpContext.Session.TryGetValue("Id", out var idBytes)
    && int.TryParse(System.Text.Encoding.UTF8.GetString(idBytes), out var userId))
            {
                var store = await _storeService.GetStoreByUserIdAsync(userId);
                ViewData["StoreStatus"] = store?.Status; // enum StoreStatus
            }

            var products = await _productService.GetAllProductsAsync();
            var productCards = new List<Products>();

            var allImages = new Dictionary<int, List<string>>();
            var allMinMaxPrices = new Dictionary<int, MinMaxPriceResponse>();
            var allVariantOptions = new Dictionary<int, List<VariantWithValuesViewModel>>();
            var allVariantCombinations = new Dictionary<int, List<VariantCombinationViewModel>>();
            var allStores = new Dictionary<int, Stores>();

            // ⭐ lấy rating từ reviews
            var ratingDict = await _storeService.GetProductRatingsAsync();

            foreach (var product in products)
            {
                productCards.Add(product);

                var productVariants = await _productVariantService.GetByProductIdAsync(product.Id);
                var variantCombinations = await _productVariantService.GetVariantCombinationsByProductIdAsync(product.Id);
                var variantImages = await _productVariantService.GetImagesByProductIdAsync(product.Id);

                var images = new List<string>();
                if (!string.IsNullOrEmpty(product.MainImage))
                    images.Add(product.MainImage);

                if (productVariants != null)
                {
                    foreach (var variant in productVariants)
                    {
                        if (!string.IsNullOrEmpty(variant.Image))
                            images.Add(variant.Image);
                    }
                }
                allImages[product.Id] = images;

                var store = await _storeService.GetStoreByIdAsync(product.StoreId);
                if (store != null)
                    allStores[product.Id] = store;

                // ✅ giá
                var price = await _priceService.GetPriceByProductIdAsync(product.Id);

                allMinMaxPrices[product.Id] = new MinMaxPriceResponse
                {
                    IsVariant = false, // vì giờ không phân biệt variant
                    Price = price,
                    OriginalPrice = null,
                    MinPrice = price,
                    MaxPrice = price
                };


                // ✅ variants & values
                var variants = await _variantService.GetByProductIdAsync(product.Id);
                var variantViewModels = new List<VariantWithValuesViewModel>();

                foreach (var variant in variants)
                {
                    var values = await _variantValueService.GetByVariantIdAsync(variant.Id);

                    variantViewModels.Add(new VariantWithValuesViewModel
                    {
                        VariantName = variant.VariantName,
                        VariantType = variant.Type,
                        Values = values.Select(v => new VariantValueItem
                        {
                            Id = v.Id,
                            ValueName = v.ValueName,
                            ColorHex = v.colorHex
                        }).ToList()
                    });
                }

                allVariantOptions[product.Id] = variantViewModels;
                allVariantCombinations[product.Id] = variantCombinations;
            }

            ViewBag.ProductList = productCards;
            ViewBag.ImagesDict = allImages;
            ViewBag.MinMaxPriceDict = allMinMaxPrices;
            ViewBag.VariantOptionsDict = allVariantOptions;
            ViewBag.VariantCombinationsDict = allVariantCombinations;
            ViewBag.StoreDict = allStores;

            // ⭐ đẩy ratingDict ra ViewBag
            ViewBag.RatingDict = ratingDict;

            ViewBag.Categories = (await _categoryService.GetAllCategoriesAsync()).Data;
            ViewBag.Provinces = new List<string> { "Cần Thơ", "TP. Hồ Chí Minh", "Hà Nội", "Đà Nẵng", "Tỉnh/TP khác" };

            return View();
        }

        [Route("Products/DetailProducts/{id:int}", Name = "ProductDetailById")]
        [Route("Products/DetailProducts/{slug}-{id:int}", Name = "ProductDetailBySlug")]
        public async Task<IActionResult> DetailProducts(int id, string? slug)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();

            // Redirect đúng slug
            string expectedSlug = product.Slug ?? string.Empty;
            if (string.IsNullOrEmpty(slug) || slug != expectedSlug)
            {
                var targetUrl = Url.RouteUrl("ProductDetailBySlug", new { slug = expectedSlug, id });
                if (!string.IsNullOrEmpty(targetUrl)) return RedirectPermanent(targetUrl);
                return RedirectToRoutePermanent("ProductDetailBySlug", new { slug = expectedSlug, id });
            }

            // ==== Product basics ====
            ViewBag.ProductName = product.Name;
            ViewBag.ProductBrand = product.Brand;
            ViewBag.ProductPlaceOfOrigin = product.PlaceOfOrigin;
            ViewBag.ProductQuantity = product.Quantity;

            // Category
            var categoryResult = await _categoryService.GetCategoryByIdAsync(product.CategoryId);
            ViewBag.CategoryName = categoryResult.Success ? categoryResult.Data.Name : "Không có danh mục";
            ViewBag.Category = categoryResult.Data;

            // Variants + combinations + images (LUÔN load trước check session)
            var productVariants = await _productVariantService.GetByProductIdAsync(id);
            var variantCombinations = await _productVariantService.GetVariantCombinationsByProductIdAsync(id);

            var allImages = new List<string>();
            if (!string.IsNullOrEmpty(product.MainImage)) allImages.Add(product.MainImage);
            if (productVariants != null)
            {
                foreach (var variant in productVariants)
                    if (!string.IsNullOrEmpty(variant.Image)) allImages.Add(variant.Image);
            }
            ViewBag.Images = allImages;

            // Build VariantOptions cho MỌI người (kể cả chưa login)
            var variants = await _variantService.GetByProductIdAsync(id);
            var variantViewModels = new List<VariantWithValuesViewModel>();
            foreach (var variant in variants)
            {
                var values = await _variantValueService.GetByVariantIdAsync(variant.Id);
                variantViewModels.Add(new VariantWithValuesViewModel
                {
                    VariantName = variant.VariantName,
                    VariantType = variant.Type,
                    Values = values.Select(v => new VariantValueItem
                    {
                        Id = v.Id,
                        ValueName = v.ValueName,
                        ColorHex = v.colorHex
                    }).ToList()
                });
            }
            ViewBag.VariantOptions = variantViewModels;
            ViewBag.VariantCombinations = variantCombinations;

            // Store info
            var store = await _storeService.GetStoreByIdAsync(product.StoreId);
            if (store != null)
            {
                ViewBag.StoreName = store.Name;
                ViewBag.StoreLogo = store.Avatar ?? "/image/default-logo.png";
                ViewBag.StoreAddress = store.Province ?? "Chưa cập nhật địa chỉ";
            }
            ViewData["StoreStatus"] = store?.Status; // enum StoreStatus
            // Giá min/max
            ViewBag.MinMaxPrice = await _priceService.GetMinMaxPriceByProductIdAsync(id);

            var price = await _priceService.GetPriceByIdAsync(id);
            ViewBag.Price = price;
            // Reviews (đừng gọi 2 lần)
            var reviews = await _reviewService.GetReviewsByProductIdAsync(id) ?? new List<ReviewViewModel>();
            ViewBag.Reviews = reviews;
            ViewBag.PurchaseCount = reviews.FirstOrDefault()?.PurchaseCount ?? 0;

            // Gợi ý cùng cửa hàng
            var sameStore = await _productService.GetProductsByStoreIdAsync(product.StoreId) ?? new List<Products>();
            var suggested = sameStore
                .Where(p => p.Id != product.Id && p.Quantity > 0)
                .OrderBy(_ => Guid.NewGuid())
                .Take(4)
                .Select(p => new { p.Id, p.Name, Image = p.MainImage, Price = p.CostPrice ?? 0m })
                .ToList();
            ViewBag.SuggestedByStore = suggested;

            // ===== Check session chỉ để quyết định quyền review, KHÔNG ảnh hưởng Variant =====
            if (!HttpContext.Session.TryGetValue("Id", out byte[] idBytes) ||
                !int.TryParse(Encoding.UTF8.GetString(idBytes), out int userId))
            {
                // Chưa đăng nhập => Không review được, NHƯNG vẫn thấy Variant
                ViewBag.CanReview = false;
                ViewBag.OrderId = null;

                return View(product);
            }

            // Đã đăng nhập => check đơn hoàn tất để mở review
            var completedOrders = await _reviewService.GetCompletedOrdersByUserAsync(userId);
            var productOrders = completedOrders
                .Where(o => o.Products.Any(p => p.ProductId == id))
                .Select(o => o.OrderId)
                .ToList();

            if (!productOrders.Any())
            {
                ViewBag.CanReview = false;
                ViewBag.OrderId = null;
            }
            else
            {
                var reviewedOrderIds = reviews
                    .Where(r => r.UserId == userId)
                    .Select(r => r.OrderId)
                    .ToList();

                var orderNotReviewed = productOrders.FirstOrDefault(o => !reviewedOrderIds.Contains(o));
                ViewBag.OrderId = orderNotReviewed;
                ViewBag.CanReview = orderNotReviewed != 0;
            }

            return View(product);
        }



    }
}