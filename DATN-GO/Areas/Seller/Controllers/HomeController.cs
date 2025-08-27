using DATN_GO.Models;
using DATN_GO.Service;
using DATN_GO.Services;
using DATN_GO.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;

namespace DATN_GO.Areas.Seller.Controllers
{
    [Area("Seller")]
    public class HomeController : Controller
    {
        private readonly StoreService _storeService;
        private readonly OrderService _orderService;
        private readonly UserService _userService;
        private readonly ProductService _productService;
        private readonly GoogleCloudStorageService _gcsService;
        private readonly ILogger<HomeController> _logger;
        private readonly TradingPaymentService _tradingPaymentService;
        private readonly VoucherService _voucherService;

        public HomeController(StoreService storeService,OrderService orderSerivce, UserService userService, GoogleCloudStorageService gcsService, ILogger<HomeController> logger, ProductService productService, VoucherService voucherService)
        public HomeController(StoreService storeService, OrderService orderSerivce, UserService userService, GoogleCloudStorageService gcsService, ILogger<HomeController> logger, ProductService productService, TradingPaymentService tradingPaymentService)
        {
            _storeService = storeService;
            _userService = userService;
            _gcsService = gcsService;
            _logger = logger;
            _orderService = orderSerivce;
            _productService = productService;
            _voucherService = voucherService;
            _tradingPaymentService = tradingPaymentService;
        }

        public async Task<IActionResult> Index()
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
            var store = await _storeService.GetStoreByUserIdAsync(int.Parse(userId));

            if (user == null || store == null) return NotFound();

            var vm = new StoreProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                UserAvatar = user.Avatar,

                StoreName = store.Name,
                Ward = store.Ward,
                District = store.District,
                Province = store.Province,
                PickupAddress = store.PickupAddress,
                CreateAt = store.CreateAt,
                Avatar = store.Avatar,
                CoverImage = store.CoverPhoto,
                Bank = store.Bank,
                BankAccount = store.BankAccount
            };
            ViewBag.BankList = new List<SelectListItem>
{
    new SelectListItem { Value = "ACB", Text = "Ngân hàng Á Châu (ACB)" },
    new SelectListItem { Value = "Agribank", Text = "Ngân hàng Nông nghiệp (Agribank)" },
    new SelectListItem { Value = "BIDV", Text = "Ngân hàng Đầu tư & Phát triển Việt Nam (BIDV)" },
    new SelectListItem { Value = "DongA", Text = "Ngân hàng Đông Á (DongA Bank)" },
    new SelectListItem { Value = "Eximbank", Text = "Ngân hàng Xuất Nhập Khẩu (Eximbank)" },
    new SelectListItem { Value = "HDBank", Text = "Ngân hàng Phát triển TP.HCM (HDBank)" },
    new SelectListItem { Value = "LienVietPostBank", Text = "Ngân hàng Bưu điện Liên Việt (LienVietPostBank)" },
    new SelectListItem { Value = "MB", Text = "Ngân hàng Quân Đội (MB)" },
    new SelectListItem { Value = "OCB", Text = "Ngân hàng Phương Đông (OCB)" },
    new SelectListItem { Value = "Sacombank", Text = "Ngân hàng Sài Gòn Thương Tín (Sacombank)" },
    new SelectListItem { Value = "SeABank", Text = "Ngân hàng Đông Nam Á (SeABank)" },
    new SelectListItem { Value = "SHB", Text = "Ngân hàng Sài Gòn - Hà Nội (SHB)" },
    new SelectListItem { Value = "Techcombank", Text = "Ngân hàng Kỹ Thương (Techcombank)" },
    new SelectListItem { Value = "TPBank", Text = "Ngân hàng Tiên Phong (TPBank)" },
    new SelectListItem { Value = "VIB", Text = "Ngân hàng Quốc tế (VIB)" },
    new SelectListItem { Value = "VietABank", Text = "Ngân hàng Việt Á (VietABank)" },
    new SelectListItem { Value = "VietBank", Text = "Ngân hàng Việt Nam Thương Tín (VietBank)" },
    new SelectListItem { Value = "Vietcombank", Text = "Ngân hàng Ngoại thương Việt Nam (Vietcombank)" },
    new SelectListItem { Value = "VietinBank", Text = "Ngân hàng Công Thương Việt Nam (VietinBank)" },
    new SelectListItem { Value = "VPBank", Text = "Ngân hàng Việt Nam Thịnh Vượng (VPBank)" }
};
            ViewBag.StoreId = store.Id;
            ViewBag.MoneyAmout = store.MoneyAmout;
            var storeId = store.Id;
            var totalProducts = await _productService.GetProductCountByStoreIdAsync(storeId);
            ViewBag.TotalProductsInStore = totalProducts;
            var totalOrders = await _orderService.GetTotalOrdersByStoreIdAsync(storeId);
            ViewBag.TotalOrders = totalOrders;
            var payments = await _tradingPaymentService.GetByStoreIdAsync(storeId);

            ViewBag.TradingPayments = payments;
            ViewBag.StoreId = storeId;

            return View(vm);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateBasicInfo(string FullName, string Email, string Phone, string Address, string StoreName)
        {
            if (string.IsNullOrWhiteSpace(FullName))
            {
                TempData["ToastMessage"] = "Họ tên không được để trống.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(Email))
            {
                TempData["ToastMessage"] = "Email không được để trống.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(Phone))
            {
                TempData["ToastMessage"] = "Số điện thoại không được để trống.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index");
            }
            else if (!Phone.All(char.IsDigit))
            {
                TempData["ToastMessage"] = "Số điện thoại chỉ được chứa số.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(Address))
            {
                TempData["ToastMessage"] = "Địa chỉ không được để trống.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index");
            }

            var userId = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var user = await _userService.GetUserByIdAsync(int.Parse(userId));
            var store = await _storeService.GetStoreByUserIdAsync(int.Parse(userId));

            if (user == null || store == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy tài khoản hoặc cửa hàng.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index");
            }

            user.FullName = FullName;
            user.Email = Email;
            user.Phone = Phone;
            await _userService.UpdateUserAsync(user.Id, user);

            store.Name = StoreName;
            store.Address = Address;
            await _storeService.UpdateStoreAsync(store.Id, store);

            TempData["ToastMessage"] = "Cập nhật thông tin thành công!";
            TempData["ToastType"] = "success";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePaymentInfo(string Bank, string BankAccount)
        {
            if (string.IsNullOrWhiteSpace(Bank))
            {
                TempData["ToastMessage"] = "Ngân hàng không được để trống.";
                TempData["ToastType"] = "danger";
            }

            if (string.IsNullOrWhiteSpace(BankAccount))
            {
                TempData["ToastMessage"] = "Số tài khoản không được để trống.";
                TempData["ToastType"] = "danger";
            }
            else if (!BankAccount.All(char.IsDigit))
            {
                TempData["ToastMessage"] = "Số tài khoản chỉ được chứa số.";
                TempData["ToastType"] = "danger";
            }

            var userId = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var store = await _storeService.GetStoreByUserIdAsync(int.Parse(userId));
            if (store == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy cửa hàng.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index");
            }

            store.Bank = Bank;
            store.BankAccount = BankAccount;
            await _storeService.UpdateStoreAsync(store.Id, store);

            TempData["ToastMessage"] = "Cập nhật thông tin thanh toán thành công!";
            TempData["ToastType"] = "success";
            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> UpdateImages(IFormFile avatarFile, IFormFile coverFile)
        {
            if (!HttpContext.Session.TryGetValue("Id", out byte[] idBytes) || !int.TryParse(System.Text.Encoding.UTF8.GetString(idBytes), out int userId))
                return Unauthorized();

            var store = await _storeService.GetStoreByUserIdAsync(userId);
            if (store == null)
                return NotFound();

            try
            {
                if (avatarFile != null && avatarFile.Length > 0)
                {
                    store.Avatar = await _gcsService.UploadFileAsync(avatarFile, "seller/avatars/");
                }

                if (coverFile != null && coverFile.Length > 0)
                {
                    store.CoverPhoto = await _gcsService.UploadFileAsync(coverFile, "seller/covers/");
                }

                await _storeService.UpdateStoreAsync(store.Id, store);

                TempData["ToastMessage"] = "Cập nhật ảnh thành công!";
                TempData["ToastType"] = "success";
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật hình ảnh.");
                return BadRequest("Đã xảy ra lỗi khi cập nhật hình ảnh.");
            }
        }
        [HttpGet("/api/Orders/totalprice/by-month/{year}/store/{storeId}")]
        public async Task<IActionResult> GetTotalPriceCountByMonth(int year)
        {
            var userId = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Index", "Home");

            var user = await _userService.GetUserByIdAsync(int.Parse(userId));
            var store = await _storeService.GetStoreByUserIdAsync(int.Parse(userId));
            var storeId = store.Id;
            var data = await _orderService.GetTotalPriceByMonthAsync(year, storeId);

            // Đảm bảo đủ 12 tháng (1 -> 12)
            var result = new Dictionary<int, decimal>();
            for (int month = 1; month <= 12; month++)
            {
                result[month] = data.Data != null && data.Data.ContainsKey(month) ? data.Data[month] : 0;
            }

            return Json(result);
        }
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            TempData["ToastMessage"] = "Bạn đã đăng xuất thành công!";
            TempData["ToastType"] = "success";

            return RedirectToAction("Index", "Home", new { area = "" });
        }
        public async Task<IActionResult> CreatePayment(int storeId)
        {
            var userId = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Index", "Home");

            var user = await _userService.GetUserByIdAsync(int.Parse(userId));
            var store = await _storeService.GetStoreByUserIdAsync(int.Parse(userId));
            if (store == null)
            {
                return NotFound("Store not found");
            }

            // ❌ Kiểm tra số dư
            if (store.MoneyAmout == null || store.MoneyAmout < 200000)
            {
                TempData["error"] = "Số dư phải lớn hơn hoặc bằng 200,000 đồng mới có thể rút.";
                return RedirectToAction("Index", new { storeId = store.Id });
            }

            // ❌ Kiểm tra còn payment Chờ Xử Lý không
            var existingPayments = await _tradingPaymentService.GetByStoreIdAsync(store.Id);
            if (existingPayments.Any(p => p.Status == TradingPaymentStatus.ChoXuLy))
            {
                TempData["error"] = "Bạn đang có yêu cầu rút tiền Chờ Xử Lý. Vui lòng chờ xử lý xong trước khi tạo yêu cầu mới.";
                return RedirectToAction("Index", new { storeId = store.Id });
            }

            // ✅ tạo payment mới
            var payment = new TradingPayment
            {
                StoreId = store.Id,
                Date = DateTime.Now,
                Cost = store.MoneyAmout.Value,
                Status = TradingPaymentStatus.ChoXuLy
            };

            var result = await _tradingPaymentService.CreateAsync(payment);

            if (result)
            {
                TempData["success"] = "Yêu cầu rút tiền đã được gửi.";
            }
            else
            {
                TempData["error"] = "Tạo yêu cầu thất bại.";
            }

            return RedirectToAction("Index", new { storeId = store.Id });
        }

    }
}