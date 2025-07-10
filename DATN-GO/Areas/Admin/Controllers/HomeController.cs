using DATN_GO.Service;
using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        private readonly StoreService _storeService;
        private readonly ProductService _productService;

        public HomeController(StoreService storeService, ProductService productService)
        {
            _storeService = storeService;
            _productService = productService;
        }

        public async Task<IActionResult> Index()
        {
            var totalShops = await _storeService.GetTotalStoresAsync();
            var totalActiveShops = await _storeService.GetTotalActiveStoresAsync();
            ViewBag.TotalProducts = await _productService.GetTotalProductsAsync();
            ViewBag.TotalShops = totalShops;
            ViewBag.TotalActiveShops = totalActiveShops;

            return View();
        }
        public async Task<IActionResult> StoreStats(int month, int year)
        {
            var count = await _storeService.GetStoreCountByMonthYearAsync(month, year);
            ViewBag.Month = month;
            ViewBag.Year = year;
            ViewBag.StoreCount = count;

            return View();
        }
        [HttpGet("/api/Stores/count/by-month/{year}")]
        public async Task<IActionResult> GetStoreCountByMonth(int year)
        {
            var data = await _storeService.GetStoreCountByYearAsync(year);

            // Đảm bảo đủ 12 tháng (1 -> 12)
            var result = new Dictionary<int, int>();
            for (int month = 1; month <= 12; month++)
            {
                result[month] = data.ContainsKey(month) ? data[month] : 0;
            }

            return Json(result);
        }
        [HttpGet("/api/Products/count/by-month/{year}")]
        public async Task<IActionResult> GetProductCountByMonth(int year)
        {
            var data = await _productService.GetProductCountByMonthAsync(year);

            // Đảm bảo đủ 12 tháng (1 -> 12)
            var result = new Dictionary<int, int>();
            for (int month = 1; month <= 12; month++)
            {
                result[month] = data.ContainsKey(month) ? data[month] : 0;
            }

            return Json(result);
        }
    }
}
