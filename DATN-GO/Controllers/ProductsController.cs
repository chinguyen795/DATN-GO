using Microsoft.AspNetCore.Mvc;
using DATN_GO.Services; // Đảm bảo đúng namespace chứa các service
using DATN_GO.Service;
using DATN_GO.ViewModels;
using DATN_GO.Models; // Nếu có tách riêng interface

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

        public ProductsController(
            ProductService productService,
            ProductVariantService productVariantService,
            StoreService storeService,
            UserService userService,
            VariantService variantService,
            VariantValueService variantValueService,
            VariantCompositionService variantCompositionService,
            PriceService priceService,
            CategoryService categoryService)
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
        }

        public async Task<IActionResult> Products()
        {
            var products = await _productService.GetAllProductsAsync();
            var productCards = new List<Products>();

            var allImages = new Dictionary<int, List<string>>();
            var allMinMaxPrices = new Dictionary<int, MinMaxPriceResponse>();
            var allVariantOptions = new Dictionary<int, List<VariantWithValuesViewModel>>();
            var allVariantCombinations = new Dictionary<int, List<VariantCombinationViewModel>>();
            var allStores = new Dictionary<int, Stores>();

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

                // ✅ Xử lý giá
                if (productVariants != null && productVariants.Any())
                {
                    decimal min = productVariants.Min(v => v.Price);
                    decimal max = productVariants.Max(v => v.Price);

                    allMinMaxPrices[product.Id] = new MinMaxPriceResponse
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


                    allMinMaxPrices[product.Id] = new MinMaxPriceResponse
                    {
                        IsVariant = false,
                        Price = product.CostPrice, // ✅ giữ null nếu không có giá
                        OriginalPrice = null
                    };
                }

                // ✅ Load variants & values
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

            ViewBag.Categories = (await _categoryService.GetAllCategoriesAsync()).Data;
            ViewBag.Provinces = new List<string> { "Cần Thơ", "TP. Hồ Chí Minh", "Hà Nội", "Đà Nẵng", "Tỉnh/TP khác" };

            return View();
        }

        public async Task<IActionResult> DetailProducts(int id)
        {
            // Lấy sản phẩm
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();

            ViewBag.ProductName = product.Name;
            var productvariant = await _productVariantService.GetByProductIdAsync(id);

            var variantCombinations = await _productVariantService.GetVariantCombinationsByProductIdAsync(id);

            // Lấy ảnh
            var variantImages = await _productVariantService.GetImagesByProductIdAsync(id);

            var allImages = new List<string>();
            if (!string.IsNullOrEmpty(product.MainImage))
                allImages.Add(product.MainImage);

            if (productvariant != null)
            {
                foreach (var variant in productvariant)
                {
                    if (!string.IsNullOrEmpty(variant.Image))
                    {
                        allImages.Add(variant.Image);
                    }
                }
            }

            ViewBag.Images = allImages;
            
            // Lấy cửa hàng
            var store = await _storeService.GetStoreByIdAsync(product.StoreId);
            if (store != null)
            {
                ViewBag.StoreName = store.Name;
                ViewBag.StoreLogo = store.Avatar ?? "/image/default-logo.png";
            }

            ViewBag.MinMaxPrice = await _priceService.GetMinMaxPriceByProductIdAsync(id);
            ViewBag.Rating = 4.8;
            ViewBag.Reviews = "1.2k";

            // ✅ Lấy variants và values
            var variants = await _variantService.GetByProductIdAsync(id);

            var variantViewModels = new List<VariantWithValuesViewModel>();

            foreach (var variant in variants)
            {
                var values = await _variantValueService.GetByVariantIdAsync(variant.Id);

                variantViewModels.Add(new VariantWithValuesViewModel
                {
                    VariantName = variant.VariantName,
                    VariantType = variant.Type, // Dùng làm name="size"/"color"
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

            return View(product);
        }


    }
}
