using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using DATN_GO.Models;
using DATN_GO.ViewModels;
using DATN_GO.ViewModels.Store;
using DATN_GO.Service;
using DATN_GO.Services;

namespace DATN_GO.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
        {
        private readonly HttpClient _http;
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
        private readonly DecoratesService _decorationService;

        public ProductController(IHttpClientFactory factory,
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
    DecoratesService decorationService)
        {
            _http = factory.CreateClient();
            _http.BaseAddress = new Uri("https://localhost:7096");
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
            _decorationService = decorationService;
        }

        public async Task<IActionResult> Index()
        {
            // vẫn yêu cầu đăng nhập
            var userIdStr = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                TempData["ToastMessage"] = "Vui lòng đăng nhập để tiếp tục!";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            // vẫn khóa admin
            var user = await _decorationService.GetUserByIdAsync(userId);
            if (user == null || user.RoleId != 3)
            {
                TempData["ToastMessage"] = "Bạn không có quyền truy cập vào trang này!";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index", "Home", new { area = "" });
            }
            var response = await _http.GetAsync("/api/Products/PendingApproval");
            if (!response.IsSuccessStatusCode)
                return View(new List<ProductAdminViewModel>());

            var json = await response.Content.ReadAsStringAsync();
            var products = JsonConvert.DeserializeObject<List<ProductAdminViewModel>>(json);
            ViewBag.UserInfo = user; 
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
                variants = variantData,
                mainImage = product?.MainImage 

            });
        }

    }
}