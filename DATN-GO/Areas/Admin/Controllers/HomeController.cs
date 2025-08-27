using DATN_GO.Models;
using DATN_GO.Service;
using DATN_GO.Services;
using DATN_GO.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        private readonly StoreService _storeService;
        private readonly ProductService _productService;
        private readonly OrderService _orderService;
        private readonly TradingPaymentService _tradingPaymentService;
        private readonly UserTradingPaymentService _userTradingPaymentService;
        public HomeController(StoreService storeService, ProductService productService, OrderService orderService, TradingPaymentService tradingPaymentService, UserTradingPaymentService userTradingPaymentService)
        private readonly DecoratesService _decorationService;

        public HomeController(StoreService storeService, ProductService productService, OrderService orderService, DecoratesService decorationService)
        {
            _storeService = storeService;
            _productService = productService;
            _orderService = orderService;
            _decorationService = decorationService;
            _tradingPaymentService = tradingPaymentService;
            _userTradingPaymentService = userTradingPaymentService;
        }

        public async Task<IActionResult> Index()
        {
            // 🔒 vẫn yêu cầu đăng nhập
            var userIdStr = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                TempData["ToastMessage"] = "Vui lòng đăng nhập để tiếp tục!";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            // 🚫 vẫn khóa admin (RoleId == 3 mới vào)
            var user = await _decorationService.GetUserByIdAsync(userId);
            if (user == null || user.RoleId != 3)
            {
                TempData["ToastMessage"] = "Bạn không có quyền truy cập vào trang này!";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            // 📊 lấy số liệu dashboard
            var totalShops = await _storeService.GetTotalStoresAsync();
            var totalActiveShops = await _storeService.GetTotalActiveStoresAsync();
            ViewBag.TotalProducts = await _productService.GetTotalProductsAsync();
            ViewBag.TotalShops = totalShops;
            ViewBag.TotalActiveShops = totalActiveShops;

            var totalRevenue = await _orderService.GetTotalRevenueAsync();
            var netRevenue = totalRevenue * 0.05m; // giả sử 5% là phí dịch vụ
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.NetRevenue = netRevenue;

            // 🎁 bơm user info lên view (giống Decorates)
            ViewBag.UserInfo = user;

            var payments = await _tradingPaymentService.GetAllAsync();
            foreach (var p in payments)
            {
                Console.WriteLine($"PaymentId={p.Id}, Store={p.Store?.Bank} - {p.Store?.BankAccount}");
            }
            var paymentsWithBank = new List<TradingPaymentViewModel>();

            foreach (var p in payments.Where(p => p.Status == TradingPaymentStatus.ChoXuLy))
            {
                var store = await _storeService.GetStoreByIdAsync(p.StoreId);

                paymentsWithBank.Add(new TradingPaymentViewModel
                {
                    Id = p.Id,
                    StoreId = p.StoreId,
                    Cost = p.Cost,
                    Date = p.Date,
                    Status = p.Status,
                    Bank = store?.Bank,
                    BankAccount = store?.BankAccount,
                    BankAccountOwner = store?.BankAccountOwner
                });
            }

            ViewBag.PendingPayments = paymentsWithBank;

            var userpayments = await _userTradingPaymentService.GetAllAsync();
            var userpaymentsWithBank = new List<UserTradingPayment>();

            foreach (var up in userpayments.Where(p => p.Status == TradingPaymentStatus.ChoXuLy))
            {

                userpaymentsWithBank.Add(new UserTradingPayment
                {
                    Id = up.Id,
                    UserId = up.UserId,
                    Cost = up.Cost,
                    Date = up.Date,
                    Status = up.Status,
                    Bank = up.Bank,
                    BankAccount = up.BankAccount,
                    BankAccountOwner = up.BankAccountOwner
                });
            }

            ViewBag.UserPendingPayments = userpaymentsWithBank;
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
        [HttpPost]
        [Route("Admin/SendReport")]
        public async Task<IActionResult> SendAllStoresRevenueReportCurrentMonth()
        {
            var result = await _orderService.SendRevenueReportAllStoresCurrentMonthAsync();
            if (result.Success)
                return Json(new { success = true, message = result.Message });
            else
                return Json(new { success = false, message = result.Message });
        }

        // Đăng xuất
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            TempData["ToastMessage"] = "Bạn đã đăng xuất thành công!";
            TempData["ToastType"] = "success";

            return RedirectToAction("Index", "Home", new { area = "" });
        }





        [HttpPost]
        public async Task<IActionResult> ConfirmPayment(int id)
        {
            var result = await _tradingPaymentService.ConfirmAsync(id);
            if (result)
            {
                TempData["success"] = "Xác nhận thành công!";
            }
            else
            {
                TempData["error"] = "Xác nhận thất bại!";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RejectPayment(int id)
        {
            var result = await _tradingPaymentService.RejectAsync(id);
            if (result)
            {
                TempData["success"] = "Từ chối thành công!";
            }
            else
            {
                TempData["error"] = "Từ chối thất bại!";
            }

            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> UserConfirmPayment(int id)
        {
            var result = await _userTradingPaymentService.ConfirmAsync(id);
            if (result)
            {
                TempData["success"] = "Xác nhận thành công!";
            }
            else
            {
                TempData["error"] = "Xác nhận thất bại!";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UserRejectPayment(int id)
        {
            var result = await _userTradingPaymentService.RejectAsync(id);
            if (result)
            {
                TempData["success"] = "Từ chối thành công!";
            }
            else
            {
                TempData["error"] = "Từ chối thất bại!";
            }

            return RedirectToAction("Index");
        }
    }
}
