using DATN_GO.Models;
using DATN_GO.Service;
using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Areas.Seller.Controllers
{
    [Area("Seller")]
    public class VoucherController : Controller
    {
        private readonly VoucherService _voucherService;
        private readonly UserService _userService;

        public VoucherController(VoucherService voucherService, UserService userService)
        {
            _voucherService = voucherService;
            _userService = userService;
        }

        // Kiểm tra vai trò trước khi vào area Seller (nếu cần dùng)
        private async Task<bool> IsUserSeller(int userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            return user != null && user.RoleId == 1;
        }

        public async Task<IActionResult> Voucher(string search, string sort, int page = 1, int pageSize = 4)
        {
            var userIdStr = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userIdStr))
            {
                TempData["ToastMessage"] = "Vui lòng đăng nhập để tiếp tục!";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            int userId = Convert.ToInt32(userIdStr);
            var storeInfo = await _voucherService.GetStoreInfoByUserIdAsync(userId);
            int storeId = storeInfo.StoreId;

            // Lấy StoreId và StoreName của người dùng đang đăng nhập
            var storeInfo = await _voucherService.GetStoreInfoByUserIdAsync(userIdInt);
            ViewBag.StoreId = storeId;
            ViewBag.StoreName = storeInfo.StoreName;

            // Lấy voucher của CHÍNH shop
            var vouchers = await _voucherService.GetVouchersByStoreOrAdminAsync(storeId) ?? new List<Vouchers>();

            // ❶ Ẩn voucher đã hết hạn / chưa hiệu lực / hết lượt
            var nowUtc = DateTime.UtcNow;
            vouchers = vouchers
                .Where(v =>
                    v.StartDate <= nowUtc &&
                    v.EndDate >= nowUtc &&
                    v.Status == VoucherStatus.Valid &&
               (v.UsedCount < v.Quantity)


                )
                .ToList();

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                vouchers = vouchers.Where(v =>
                    v.Reduce.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    v.MinOrder.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    v.Quantity.ToString().Contains(search, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            ViewBag.Categories = await _voucherService.GetAllCategoriesAsync();
            ViewBag.Stores = await _voucherService.GetAllStoresAsync();

            // Sort
            if (!string.IsNullOrWhiteSpace(sort))
            {
                vouchers = sort switch
                {
                    "newest" => vouchers.OrderByDescending(v => v.StartDate).ToList(),
                    "oldest" => vouchers.OrderBy(v => v.StartDate).ToList(),
                    "value-desc" => vouchers.OrderByDescending(v => v.Reduce).ToList(),
                    "value-asc" => vouchers.OrderBy(v => v.Reduce).ToList(),
                    _ => vouchers
                };
            }

            // Paging (trên danh sách đã lọc)
            int totalItems = vouchers.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var paginatedVouchers = vouchers.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = search;
            ViewBag.Sort = sort;

            return View(paginatedVouchers);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVoucher(Vouchers request)
        {
            var userIdStr = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userIdStr))
            {
                TempData["Error"] = "Vui lòng đăng nhập!";
                return RedirectToAction("Index", "Home", new { area = "" });
            }
            int userId = Convert.ToInt32(userIdStr);

            // Lấy store của user
            var storeInfo = await _voucherService.GetStoreInfoByUserIdAsync(userId);
            int storeId = storeInfo.StoreId; // ví dụ: 2

            // BẮT BUỘC phạm vi áp dụng: phải có CategoryId (nếu bạn chưa dùng ProductVouchers)
            if (request.CategoryId == null)
            {
                TempData["Error"] = "Vui lòng chọn danh mục áp dụng.";
                return RedirectToAction("Voucher");
            }

            // Force các field theo rule API dành cho SHOP
            request.StoreId = storeId;                     // voucher của shop
            request.CreatedByRoleId = 2;                   // SHOP = 2 (QUAN TRỌNG)
            request.Type = VoucherType.Shop;
            request.CreatedByUserId = userId;
            request.Status = VoucherStatus.Valid;
            request.UsedCount = 0;

            // Chuẩn hóa timezone sang UTC (API dùng UtcNow kiểm tra)
            request.StartDate = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc);
            request.EndDate = DateTime.SpecifyKind(request.EndDate, DateTimeKind.Utc);

            // Gọi API
            var ok = await _voucherService.CreateVoucherAsync(request);
            TempData[ok ? "Success" : "Error"] = ok ? "Thêm voucher thành công!" : "Thêm voucher thất bại. Vui lòng kiểm tra lại thông tin!";
            return RedirectToAction("Voucher");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateVoucher(Vouchers request)
        {
            var userIdStr = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userIdStr))
            {
                TempData["Error"] = "Vui lòng đăng nhập!";
                return RedirectToAction("Index", "Home", new { area = "" });
            }
            int userId = Convert.ToInt32(userIdStr);

            var storeInfo = await _voucherService.GetStoreInfoByUserIdAsync(userId);
            int storeId = storeInfo.StoreId;

            if (request.CategoryId == null)
            {
                TempData["Error"] = "Vui lòng chọn danh mục áp dụng.";
                return RedirectToAction("Voucher");
            }

            // Force các field theo rule API dành cho SHOP
            request.StoreId = storeId;
            request.CreatedByRoleId = 2;                   // SHOP = 2
            request.Type = VoucherType.Shop;
            request.CreatedByUserId = userId;

            request.StartDate = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc);
            request.EndDate = DateTime.SpecifyKind(request.EndDate, DateTimeKind.Utc);

            var ok = await _voucherService.UpdateVoucherAsync(request);
            TempData[ok ? "Success" : "Error"] = ok ? "Cập nhật voucher thành công!" : "Cập nhật voucher thất bại!";
            return RedirectToAction("Voucher");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVoucherConfirmed(int id)
        {
            var ok = await _voucherService.DeleteVoucherAsync(id);
            TempData[ok ? "Success" : "Error"] = ok ? "Xoá voucher thành công!" : "Xoá voucher thất bại!";
            return RedirectToAction("Voucher");
        }

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
