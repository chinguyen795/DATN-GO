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
        public VoucherController(VoucherService voucherService) => _voucherService = voucherService;

        public async Task<IActionResult> Voucher(string? search, string? sort, int page = 1, int pageSize = 4)
        {
            // Chuẩn hoá paging
            page = page < 1 ? 1 : page;
            pageSize = Math.Clamp(pageSize, 1, 200);

            // Lấy danh sách voucher admin (StoreId == null)
            var vouchers = await _voucherService.GetVouchersByStoreOrAdminAsync(null)
                           ?? new List<Vouchers>();

            // Search đơn giản theo 3 trường số (không lỗi null)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim();
                vouchers = vouchers.Where(v =>
                       v.Reduce.ToString().Contains(q, StringComparison.OrdinalIgnoreCase)
                    || v.MinOrder.ToString().Contains(q, StringComparison.OrdinalIgnoreCase)
                    || v.Quantity.ToString().Contains(q, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Nạp dữ liệu phụ cho View (đảm bảo không null để tránh NullReference ở view)
            var categories = await _voucherService.GetAllCategoriesAsync() ?? new List<Categories>();
            var stores = await _voucherService.GetAllStoresAsync() ?? new List<Stores>();
            var products = await _voucherService.GetAllProductsAsync() ?? new List<Products>();

            ViewBag.Categories = categories;
            ViewBag.Stores = stores;
            ViewBag.Products = products;

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

            // Paging (sau sort)
            var totalItems = vouchers.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var paginated = vouchers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // ViewBag cho UI
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = search;
            ViewBag.Sort = sort;
            ViewBag.PageSize = pageSize;

            return View(paginated);
        }


        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Vouchers request)
        {
            // Admin cố định
            request.StoreId = null;
            request.CreatedByRoleId = 3;
            request.Type = VoucherType.Platform;
            request.CreatedByUserId = GetUserIdOrDefault();

            // Chuẩn hoá theo flag
            if (request.ApplyAllCategories) request.CategoryId = null;
            if (request.ApplyAllProducts) request.SelectedProductIds = new List<int>();

            if (!IsValidVoucher(request))
            {
                TempData["Error"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction("Voucher");
            }

            var ok = await _voucherService.CreateVoucherAsync(request);
            TempData[ok ? "Success" : "Error"] = ok ? "Thêm voucher thành công." : "Thêm voucher thất bại.";
            return RedirectToAction("Voucher");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Vouchers request)
        {
            request.StoreId = null;
            request.CreatedByRoleId = 3;
            request.Type = VoucherType.Platform;
            request.CreatedByUserId = GetUserIdOrDefault();

            if (request.ApplyAllCategories) request.CategoryId = null;
            if (request.ApplyAllProducts) request.SelectedProductIds = new List<int>();

            if (!IsValidVoucher(request))
            {
                TempData["Error"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction("Voucher");
            }

            var ok = await _voucherService.UpdateVoucherAsync(request);
            TempData[ok ? "Success" : "Error"] = ok ? "Cập nhật voucher thành công." : "Cập nhật thất bại.";
            return RedirectToAction("Voucher");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _voucherService.DeleteVoucherAsync(id);
            TempData[ok ? "Success" : "Error"] = ok ? "Xóa voucher thành công." : "Xóa thất bại.";
            return RedirectToAction("Voucher");
        }

        private int GetUserIdOrDefault()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
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

            // PHẠM VI: ít nhất một trong bốn
            var hasAnyScope =
                v.ApplyAllCategories ||
                v.ApplyAllProducts ||
                v.CategoryId.HasValue ||
                (v.SelectedProductIds != null && v.SelectedProductIds.Any());
            if (!hasAnyScope) return false;

            // Admin
            if (v.StoreId != null) return false;
            if (v.CreatedByRoleId != 3) return false;

            return true;
        }
    }
}