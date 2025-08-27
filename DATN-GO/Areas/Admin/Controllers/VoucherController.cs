using DATN_GO.Models;
using DATN_GO.Service;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DATN_GO.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class VoucherController : Controller
    {
        private readonly VoucherService _voucherService;
        private readonly DecoratesService _decorationService;

        public VoucherController(VoucherService voucherService, DecoratesService decorationService)
        {
            _voucherService = voucherService;
            _decorationService = decorationService;
        }

        public async Task<IActionResult> Voucher(string search, string sort, int page = 1, int pageSize = 4)
        {
            // Admin: lấy voucher sàn (StoreId == null)
            var vouchers = await _voucherService.GetVouchersByStoreOrAdminAsync(null) ?? new List<Vouchers>();

            if (!string.IsNullOrWhiteSpace(search))
            {
                vouchers = vouchers
      .Where(v =>
          v.Reduce.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
          v.MinOrder.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
          v.Quantity.ToString().Contains(search, StringComparison.OrdinalIgnoreCase))
      .ToList();
            }

            ViewBag.Categories = await _voucherService.GetAllCategoriesAsync();
            ViewBag.Stores = await _voucherService.GetAllStoresAsync();

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Vouchers request)
        {
            // Force admin fields
            request.StoreId = null;
            request.CreatedByRoleId = 3;
            request.Type = VoucherType.Platform;
            request.CreatedByUserId = GetUserIdOrDefault();

            if (!IsValidVoucher(request))
            {
                TempData["Error"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction("Voucher");
            }

            var success = await _voucherService.CreateVoucherAsync(request);
            TempData[success ? "Success" : "Error"] = success ? "Thêm voucher thành công." : "Thêm voucher thất bại.";
            return RedirectToAction("Voucher");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Vouchers request)
        {
            // Force admin fields
            request.StoreId = null;
            request.CreatedByRoleId = 3;
            request.Type = VoucherType.Platform;
            request.CreatedByUserId = GetUserIdOrDefault();

            if (!IsValidVoucher(request))
            {
                TempData["Error"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction("Voucher");
            }

            var success = await _voucherService.UpdateVoucherAsync(request);
            TempData[success ? "Success" : "Error"] = success ? "Cập nhật voucher thành công." : "Cập nhật thất bại.";
            return RedirectToAction("Voucher");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _voucherService.DeleteVoucherAsync(id);
            TempData[success ? "Success" : "Error"] = success ? "Xóa voucher thành công." : "Xóa thất bại.";
            return RedirectToAction("Voucher");
        }

        private int GetUserIdOrDefault()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User.FindFirstValue("sub");
                if (int.TryParse(idStr, out var uid)) return uid;
            }
            return 1; 
        }

        private bool IsValidVoucher(Vouchers v)
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
            if (v.CategoryId == null) return false;

            if (v.StoreId != null) return false;
            if (v.CreatedByRoleId != 3) return false;

            return true;
        }

    }
}
