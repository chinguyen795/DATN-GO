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

        private async Task<bool> IsUserSeller(int userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            return user != null && user.RoleId == 2;
        }

        // ---------- Helpers ----------
        private static bool IsValidSellerVoucher(Vouchers v)
        {
            if (v.StartDate >= v.EndDate) return false;
            if (v.Quantity < 1) return false;
            if (v.MinOrder < 0) return false;

            if (v.IsPercentage)
            {
                if (v.Reduce <= 0 || v.Reduce > 100) return false;
                if (v.MaxDiscount is decimal md && md < 0) return false;
            }
            else
            {
                if (v.Reduce <= 0) return false;
            }

            // Ít nhất một phạm vi: all products / category / selected products
            var hasScope =
                v.ApplyAllProducts ||
                v.CategoryId.HasValue ||
                (v.SelectedProductIds != null && v.SelectedProductIds.Any());

            return hasScope;
        }

        private static void NormalizeScopeFlags(Vouchers v)
        {
            // Nếu áp dụng tất cả sản phẩm thì clear list chọn lẻ
            if (v.ApplyAllProducts)
            {
                v.SelectedProductIds = new List<int>();
            }
        }

        private static void ForceSellerMeta(Vouchers v, int storeId, int userId, bool isCreate)
        {
            v.StoreId = storeId;
            v.CreatedByRoleId = 2; // Seller
            v.Type = VoucherType.Shop;
            v.CreatedByUserId = userId;

            if (isCreate)
            {
                v.Status = VoucherStatus.Valid;
                v.UsedCount = 0;
            }

            // Đồng bộ UTC (server side validation đang dùng UtcNow)
            v.StartDate = DateTime.SpecifyKind(v.StartDate, DateTimeKind.Utc);
            v.EndDate = DateTime.SpecifyKind(v.EndDate, DateTimeKind.Utc);
        }

        private async Task<List<int>> SanitizeSelectedProductIdsForStoreAsync(IEnumerable<int> ids, int storeId)
        {
            var shopProducts = await _voucherService.GetProductsByStoreAsync(storeId);
            var allowed = new HashSet<int>(shopProducts.Select(p => p.Id));
            return ids.Where(allowed.Contains).Distinct().ToList();
        }

        // ---------- LIST ----------
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

            ViewBag.StoreId = storeId;
            ViewBag.StoreName = storeInfo.StoreName;

            // VOUCHERS của shop
            var vouchers = await _voucherService.GetVouchersByStoreOrAdminAsync(storeId) ?? new List<Vouchers>();

            // Chỉ còn hiệu lực
            var nowUtc = DateTime.UtcNow;
            vouchers = vouchers
                .Where(v => v.StartDate <= nowUtc &&
                            v.EndDate >= nowUtc &&
                            v.Status == VoucherStatus.Valid &&
                            (v.UsedCount < v.Quantity))
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

            // Master data
            ViewBag.Categories = await _voucherService.GetAllCategoriesAsync();

            // 🔴 CHỈ LẤY SẢN PHẨM CỦA SHOP ĐÓ
            ViewBag.Products = await _voucherService.GetProductsByStoreIdAsync(storeId);

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

            // Paging
            int totalItems = vouchers.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var paginatedVouchers = vouchers.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = search;
            ViewBag.Sort = sort;

            return View(paginatedVouchers);
        }


        // ---------- CREATE ----------
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

            var storeInfo = await _voucherService.GetStoreInfoByUserIdAsync(userId);
            int storeId = storeInfo.StoreId;

            NormalizeScopeFlags(request);

            // chặn gian lận: list sản phẩm phải thuộc shop
            if (request.SelectedProductIds != null && request.SelectedProductIds.Any())
                request.SelectedProductIds = await SanitizeSelectedProductIdsForStoreAsync(request.SelectedProductIds, storeId);

            ForceSellerMeta(request, storeId, userId, isCreate: true);

            if (!IsValidSellerVoucher(request))
            {
                TempData["Error"] = "Phạm vi/giá trị voucher không hợp lệ. Chọn danh mục hoặc sản phẩm (hoặc áp dụng tất cả sản phẩm).";
                return RedirectToAction("Voucher");
            }

            var ok = await _voucherService.CreateVoucherAsync(request);
            TempData[ok ? "Success" : "Error"] = ok ? "Thêm voucher thành công!" : "Thêm voucher thất bại. Vui lòng kiểm tra lại thông tin!";
            return RedirectToAction("Voucher");
        }

        // ---------- UPDATE ----------
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

            NormalizeScopeFlags(request);

            if (request.SelectedProductIds != null && request.SelectedProductIds.Any())
                request.SelectedProductIds = await SanitizeSelectedProductIdsForStoreAsync(request.SelectedProductIds, storeId);

            ForceSellerMeta(request, storeId, userId, isCreate: false);

            if (!IsValidSellerVoucher(request))
            {
                TempData["Error"] = "Phạm vi/giá trị voucher không hợp lệ. Chọn danh mục hoặc sản phẩm (hoặc áp dụng tất cả sản phẩm).";
                return RedirectToAction("Voucher");
            }

            var ok = await _voucherService.UpdateVoucherAsync(request);
            TempData[ok ? "Success" : "Error"] = ok ? "Cập nhật voucher thành công!" : "Cập nhật voucher thất bại!";
            return RedirectToAction("Voucher");
        }

        // ---------- DELETE ----------
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