using DATN_GO.Service;
using DATN_GO.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using DATN_GO.Models;
using DATN_GO.ViewModels;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DATN_GO.Areas.Seller.Controllers
{
    [Area("Seller")]
    public class ProductController : Controller
    {
        private readonly ProductService _productService;
        private readonly CategoryService _categoryService;
        private readonly StoreService _storeService;
        private readonly UserService _userService;
        private readonly VariantService _variantService;
        private readonly VariantValueService _variantValueService;
        private readonly ProductVariantService _productVariantService;
        private readonly PriceService _priceService;
        private readonly VariantCompositionService _variantCompositionService;
        private readonly GoogleCloudStorageService _gcsService;
        private readonly ILogger<ProductController> _logger;
        private readonly VoucherService _voucherService;

        public ProductController(
            ProductService productService,
            CategoryService categoryService,
            StoreService storeService,
            UserService userService,
            VariantService variantService,
            ProductVariantService productVariantService,
            PriceService priceService,
            VariantValueService variantValueService,
            VariantCompositionService variantCompositionService,
            GoogleCloudStorageService gcsService,
            ILogger<ProductController> logger,
            VoucherService voucherService)
        {
            _productService = productService;
            _categoryService = categoryService;
            _storeService = storeService;
            _userService = userService;
            _variantService = variantService;
            _productVariantService = productVariantService;
            _priceService = priceService;
            _variantValueService = variantValueService;
            _variantCompositionService = variantCompositionService;
            _gcsService = gcsService;
            _logger = logger;
            _voucherService = voucherService;
        }

        [HttpGet]
        public async Task<IActionResult> Product()
        {
            var userId = HttpContext.Session.GetString("Id");

            if (string.IsNullOrEmpty(userId))
            {
                TempData["ToastMessage"] = "Vui lòng đăng nhập để tiếp tục!";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            int userIdInt = Convert.ToInt32(userId);


            // Lấy StoreId và StoreName của người dùng đang đăng nhập
            var storeInfo = await _voucherService.GetStoreInfoByUserIdAsync(userIdInt);

            // Gán StoreId và StoreName vào ViewBag
            ViewBag.StoreId = storeInfo.StoreId;
            ViewBag.StoreName = storeInfo.StoreName;

            var user = await _userService.GetUserByIdAsync(int.Parse(userId));
            var store = await _storeService.GetStoreByUserIdAsync(user.Id);
            var products = await _productService.GetProductsByStoreIdAsync(store.Id);

            var categoryNames = new Dictionary<int, string>();
            var variantsDict = new Dictionary<int, List<Variants>>();
            var productVariantsDict = new Dictionary<int, List<ProductVariants>>();
            var totalQuantities = new Dictionary<int, int>();
            var prices = new Dictionary<int, decimal>();

            if (products != null)
            {
                foreach (var product in products)
                {
                    var category = await _categoryService.GetCategoryByProductIdAsync(product.Id);
                    categoryNames[product.Id] = category?.Name ?? "Không xác định";

                    var variants = await _variantService.GetByProductIdAsync(product.Id);
                    variantsDict[product.Id] = variants;

                    var productVariants = await _productVariantService.GetByProductIdAsync(product.Id);
                    productVariantsDict[product.Id] = productVariants;

                    int totalQuantity = productVariants?.Sum(pv => pv.Quantity) ?? product.Quantity;
                    totalQuantities[product.Id] = totalQuantity;

                    var price = await _priceService.GetPriceByProductIdAsync(product.Id);
                    if (price.HasValue)
                    {
                        prices[product.Id] = price.Value;
                    }
                }
            }
            ViewBag.Products = products;
            ViewBag.CategoryNames = categoryNames;
            ViewBag.VariantsDict = variantsDict;
            ViewBag.ProductVariantsDict = productVariantsDict;
            ViewBag.TotalQuantities = totalQuantities;
            ViewBag.Prices = prices;
            ViewBag.UserName = user.FullName;
            ViewBag.UserAvatar = string.IsNullOrEmpty(user.Avatar)
                ? "/images/default-avatar.jpg"
                : user.Avatar;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> CreateProduct()
        {
            var userId = HttpContext.Session.GetString("Id");
            int userIdInt = Convert.ToInt32(userId);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Index", "Home");

            // Lấy StoreId và StoreName của người dùng đang đăng nhập
            var storeInfo = await _voucherService.GetStoreInfoByUserIdAsync(userIdInt);
            var user = await _userService.GetUserByIdAsync(int.Parse(userId));
            var categories = await _categoryService.GetAllCategoriesAsync();
            ViewBag.Categories = categories.Data.Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToList();
            ViewBag.UserName = user.FullName;
            ViewBag.StoreName = storeInfo.StoreName;
            ViewBag.UserAvatar = string.IsNullOrEmpty(user.Avatar)
                ? "/images/default-avatar.jpg"
                : user.Avatar;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct(
    [FromForm] ProductCreateViewModel model,
    [FromForm] string? Variants,
    [FromForm] string? Combinations)
        {
            var userId = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Bạn chưa đăng nhập." });
            }

            var user = await _userService.GetUserByIdAsync(int.Parse(userId));
            var store = await _storeService.GetStoreByUserIdAsync(user.Id);

            string imageUrl = null;
            if (model.Image != null && model.Image.Length > 0)
            {
                try
                {
                    imageUrl = await _gcsService.UploadFileAsync(model.Image, "products/");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi upload ảnh GCS");
                    return Json(new { success = false, message = "Tải ảnh lên thất bại." });
                }
            }

            var product = new Products
            {
                Name = model.Name,
                Brand = model.Brand,
                PlaceOfOrigin = model.PlaceOfOrigin,
                CategoryId = model.CategoryId,
                StoreId = store.Id,
                Quantity = model.Quantity ?? 0,
                CostPrice = model.CostPrice,
                Description = model.Description,
                Weight = model.Weight,
                Height = model.Height,
                Width = model.Width,
                Length = model.Length,
                MainImage = imageUrl
            };

            var fullModel = new ProductFullCreateViewModel
            {
                Product = product,
                Price = model.Price
            };

            if (!string.IsNullOrEmpty(Variants))
                fullModel.Variants = JsonSerializer.Deserialize<List<VariantCreateModel>>(Variants);

            if (!string.IsNullOrEmpty(Combinations))
                fullModel.Combinations = JsonSerializer.Deserialize<List<VariantCombinationModel>>(Combinations);

            var result = await _productService.CreateFullProductAsync(fullModel);

            if (result.Success)
            {
                return Json(new { success = true, productId = result.ProductId });
            }

            return Json(new { success = false, message = result.ErrorMessage ?? "Tạo sản phẩm thất bại." });
        }


        [HttpGet]
        public async Task<IActionResult> GetProductDetail(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            var productVariants = await _productVariantService.GetByProductIdAsync(id);
            var compositions = await _variantCompositionService.GetByProductIdAsync(id);
            var variants = await _variantService.GetByProductIdAsync(id);
            var variantValues = await _variantValueService.GetAllAsync();
            var price = await _priceService.GetPriceByProductIdAsync(id);

            int totalQuantity = productVariants?.Sum(p => p.Quantity) ?? product?.Quantity ?? 0;

            var valueDict = variantValues.ToDictionary(v => v.Id, v => v.ValueName);
            var nameDict = variants.ToDictionary(v => v.Id, v => v.VariantName);

            string variantName1 = variants.ElementAtOrDefault(0)?.VariantName ?? "";
            string variantName2 = variants.ElementAtOrDefault(1)?.VariantName ?? "";

            var variantOrder = variants.Select(v => v.Id).ToList();

            var variantData = productVariants?.Select(pv =>
            {
                var comps = compositions?
                    .Where(c => c.ProductVariantId == pv.Id)
                    .OrderBy(c => variantOrder.IndexOf(c.VariantId ?? 0))
                    .ToList();

                string val1 = comps?.ElementAtOrDefault(0)?.VariantValueId is int id1 && valueDict.ContainsKey(id1) ? valueDict[id1] : "";
                string val2 = comps?.ElementAtOrDefault(1)?.VariantValueId is int id2 && valueDict.ContainsKey(id2) ? valueDict[id2] : "";

                return new
                {
                    costPrice = pv.CostPrice,
                    price = pv.Price,
                    quantity = pv.Quantity,
                    weight = pv.Weight,
                    length = pv.Length,
                    width = pv.Width,
                    height = pv.Height,
                    image = pv.Image,
                    variantValue1 = val1,
                    variantValue2 = val2
                };
            }) ?? Enumerable.Empty<object>();

            return Json(new
            {
                name = product?.Name,
                placeoforigin = product?.PlaceOfOrigin,
                brand = product?.Brand,
                createat = product?.CreateAt.ToString("yyyy-MM-dd"),
                price = price ?? 0,
                totalQuantity = totalQuantity,
                variantName1 = variantName1,
                variantName2 = variantName2,
                variants = variantData
            });
        }

        [HttpDelete]
        [Route("Seller/Product/DeleteProduct/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var result = await _productService.DeleteProduct2Async(id);
            return result
                ? Ok(new { message = "Xóa thành công" })
                : BadRequest("Không thể xóa sản phẩm.");
        }

        [HttpPost]
        [Route("Upload/VariantImage")]
        public async Task<IActionResult> UploadVariantImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "Không có file nào được gửi." });

            try
            {
                var imageUrl = await _gcsService.UploadFileAsync(file, "products/");
                if (string.IsNullOrEmpty(imageUrl))
                    return Json(new { success = false, message = "Upload thất bại." });

                return Json(new { success = true, url = imageUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi upload ảnh biến thể");
                return Json(new { success = false, message = ex.Message });
            }
        }
        // Logout
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            TempData["ToastMessage"] = "Bạn đã đăng xuất thành công!";
            TempData["ToastType"] = "success";

            return RedirectToAction("Index", "Home", new { area = "" });
        }
    }
}
