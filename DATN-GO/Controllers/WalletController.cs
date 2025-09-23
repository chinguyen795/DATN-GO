using DATN_GO.Models;
using DATN_GO.Service;
using DATN_GO.Services;
using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Controllers
{
    public class WalletController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly UserService _userService;
        private readonly BankService _bankService;
        private readonly StoreService _storeService;
        private readonly UserTradingPaymentService _userTradingPaymentService;
        public WalletController(IHttpClientFactory factory, UserService userService, BankService bankService, UserTradingPaymentService userTradingPaymentService, StoreService storeService)
        {
            _httpClient = factory.CreateClient();
            _userService = userService;
            _bankService = bankService;
            _userTradingPaymentService = userTradingPaymentService;
            _storeService = storeService;
        }
        public async Task<IActionResult> Index()
        {
            if (!HttpContext.Session.TryGetValue("Id", out byte[] idBytes) ||
                !int.TryParse(System.Text.Encoding.UTF8.GetString(idBytes), out int userId))
            {
                TempData["ToastMessage"] = "Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Login", "UserAuthentication");
            }
            var store = await _storeService.GetStoreByUserIdAsync(userId);
            ViewData["StoreStatus"] = store?.Status; // enum StoreStatus
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy thông tin người dùng.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Banks = await _bankService.GetBankListAsync();
            var payments = await _userTradingPaymentService.GetByUserIdAsync(userId);

            // fix đúng key để view dùng
            ViewBag.TradingPayments = payments;

            var remaining = user.Balance;

            ViewBag.Remaining = remaining;

            return View(user);
        }
        public async Task<IActionResult> CreatePayment(UserTradingPayment model)
        {
            var userId = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Index", "Home");

            var user = await _userService.GetUserByIdAsync(int.Parse(userId));

            // ❌ Kiểm tra số dư
            if (user.Balance == null || user.Balance < 50000)
            {
                TempData["error"] = "Số dư phải lớn hơn hoặc bằng 50,000 đồng mới có thể rút.";
                return RedirectToAction("Index", new { userId = user.Id });
            }

            // ❌ Kiểm tra còn payment Chờ Xử Lý không
            var existingPayments = await _userTradingPaymentService.GetByUserIdAsync(user.Id);
            if (existingPayments.Any(p => p.Status == TradingPaymentStatus.ChoXuLy))
            {
                TempData["error"] = "Bạn đang có yêu cầu rút tiền Chờ Xử Lý. Vui lòng chờ xử lý xong trước khi tạo yêu cầu mới.";
                return RedirectToAction("Index", new { userId = user.Id });
            }

            // ✅ tạo payment mới
            var payment = new UserTradingPayment
            {
                UserId = user.Id,
                Date = DateTime.Now,
                Cost = user.Balance.Value,
                Status = TradingPaymentStatus.ChoXuLy,
                Bank = model.Bank,
                BankAccount = model.BankAccount,
                BankAccountOwner = model.BankAccountOwner
            };

            var result = await _userTradingPaymentService.CreateAsync(payment);

            if (result != null)
            {
                TempData["success"] = "Yêu cầu rút tiền đã được gửi.";
            }
            else
            {
                TempData["error"] = "Tạo yêu cầu thất bại.";
            }

            return RedirectToAction("Index", new { userId = user.Id });
        }
    }
}
