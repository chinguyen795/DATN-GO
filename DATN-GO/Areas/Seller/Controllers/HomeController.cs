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
        private readonly UserService _userService;
        private readonly GoogleCloudStorageService _gcsService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(StoreService storeService, UserService userService, GoogleCloudStorageService gcsService, ILogger<HomeController> logger)
        {
            _storeService = storeService;
            _userService = userService;
            _gcsService = gcsService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Index", "Home");

            var uid = int.Parse(userId);
            var user = await _userService.GetUserByIdAsync(uid);
            var store = await _storeService.GetStoreByUserIdAsync(uid);

            if (user == null || store == null) return NotFound();

            var vm = new StoreProfileViewModel
            {
                // Users
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                UserAvatar = user.Avatar,

                // Stores
                StoreName = store.Name,
                // BỎ Address, thay bằng các trường mới
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

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBasicInfo(
            string FullName,
            string Email,
            string Phone,
            string StoreName,
            string Ward,
            string District,
            string Province,
            string PickupAddress)
        {
            // Validate Users
            if (string.IsNullOrWhiteSpace(FullName))
            {
                TempData["Error"] = "Họ tên không được để trống.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(Email))
            {
                TempData["Error"] = "Email không được để trống.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(Phone))
            {
                TempData["Error"] = "Số điện thoại không được để trống.";
                return RedirectToAction("Index");
            }
            else if (!Phone.All(char.IsDigit))
            {
                TempData["Error"] = "Số điện thoại chỉ được chứa số.";
                return RedirectToAction("Index");
            }

            // Validate Stores
            if (string.IsNullOrWhiteSpace(PickupAddress))
            {
                TempData["Error"] = "Địa chỉ lấy hàng không được để trống.";
                return RedirectToAction("Index");
            }
            if (string.IsNullOrWhiteSpace(Ward) ||
                string.IsNullOrWhiteSpace(District) ||
                string.IsNullOrWhiteSpace(Province))
            {
                TempData["Error"] = "Phường/Xã, Quận/Huyện, Tỉnh/Thành phố không được để trống.";
                return RedirectToAction("Index");
            }

            var userId = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var uid = int.Parse(userId);
            var user = await _userService.GetUserByIdAsync(uid);
            var store = await _storeService.GetStoreByUserIdAsync(uid);

            if (user == null || store == null)
            {
                TempData["Error"] = "Không tìm thấy tài khoản hoặc cửa hàng.";
                return RedirectToAction("Index");
            }

            // Update Users
            user.FullName = FullName;
            user.Email = Email;
            user.Phone = Phone;
            await _userService.UpdateUserAsync(user.Id, user);

            // Update Stores
            store.Name = StoreName;
            store.Ward = Ward;
            store.District = District;
            store.Province = Province;
            store.PickupAddress = PickupAddress;
            await _storeService.UpdateStoreAsync(store.Id, store);

            TempData["Success"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> UpdatePaymentInfo(string Bank, string BankAccount)
        {
            if (string.IsNullOrWhiteSpace(Bank))
            {
                TempData["Error"] = "Ngân hàng không được để trống.";
            }

            if (string.IsNullOrWhiteSpace(BankAccount))
            {
                TempData["Error"] = "Số tài khoản không được để trống.";
            }
            else if (!BankAccount.All(char.IsDigit))
            {
                TempData["Error"] = "Số tài khoản chỉ được chứa số.";
            }

            var userId = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var store = await _storeService.GetStoreByUserIdAsync(int.Parse(userId));
            if (store == null)
            {
                TempData["Error"] = "Không tìm thấy cửa hàng.";
                return RedirectToAction("Index");
            }

            store.Bank = Bank;
            store.BankAccount = BankAccount;
            await _storeService.UpdateStoreAsync(store.Id, store);

            TempData["Success"] = "Cập nhật thông tin thanh toán thành công!";
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

                TempData["Success"] = "Cập nhật ảnh thành công!";
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật hình ảnh.");
                return BadRequest("Đã xảy ra lỗi khi cập nhật hình ảnh.");
            }
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
    }
}