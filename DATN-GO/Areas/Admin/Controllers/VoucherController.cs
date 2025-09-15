using DATN_GO.Models;
using DATN_GO.Service;
using DATN_GO.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DATN_GO.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class VoucherController : Controller
    {
        private readonly VoucherService _voucherService;
        public VoucherController(VoucherService voucherService) => _voucherService = voucherService;

        // ================== LIST ==================
        public async Task<IActionResult> Voucher(string? search, string? sort, int page = 1, int pageSize = 4)
        {
            page = page < 1 ? 1 : page;
            pageSize = Math.Clamp(pageSize, 1, 200);

            var vouchers = await _voucherService.GetVouchersByStoreOrAdminAsync(null) ?? new List<Vouchers>();

            // search
            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim();
                vouchers = vouchers.Where(v =>
                       v.Reduce.ToString().Contains(q, StringComparison.OrdinalIgnoreCase)
                    || v.MinOrder.ToString().Contains(q, StringComparison.OrdinalIgnoreCase)
                    || v.Quantity.ToString().Contains(q, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // master data cho view
            ViewBag.Categories = await _voucherService.GetAllCategoriesAsync() ?? new List<Categories>();
            ViewBag.Stores = await _voucherService.GetAllStoresAsync() ?? new List<Stores>();
            ViewBag.Products = await _voucherService.GetAllProductsAsync() ?? new List<Products>();

            // sort
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

            // phân trang
            var totalItems = vouchers.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var paginated = vouchers.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = search;
            ViewBag.Sort = sort;
            ViewBag.PageSize = pageSize;

            return View(paginated);
        }

        // ================== CREATE ==================
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateVoucherDto dto)
        {
            dto.CreatedByUserId = GetUserIdOrDefault();
            dto.CreatedByRoleId = 3; // admin
            dto.StoreId = null;      // sàn

            if (!IsValidVoucher(dto))
            {
                TempData["Error"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction("Voucher");
            }

            var result = await _voucherService.CreateVoucherAsync(dto);
            TempData[result.Ok ? "Success" : "Error"] = result.Message;
            return RedirectToAction("Voucher");
        }

        // ================== EDIT ==================
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateVoucherDto dto)
        {
            dto.CreatedByUserId = GetUserIdOrDefault();
            dto.CreatedByRoleId = 3;
            dto.StoreId = null;

            if (!IsValidVoucher(dto))
            {
                TempData["Error"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction("Voucher");
            }

            var result = await _voucherService.UpdateVoucherAsync(dto);
            TempData[result.Ok ? "Success" : "Error"] = result.Message;
            return RedirectToAction("Voucher");
        }

        // ================== DELETE ==================
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _voucherService.DeleteVoucherAsync(id);
            TempData[result.Ok ? "Success" : "Error"] = result.Message;
            return RedirectToAction("Voucher");
        }

        // ================== HELPERS ==================
        private int GetUserIdOrDefault()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
                if (int.TryParse(idStr, out var uid)) return uid;
            }
            return 1; // fallback
        }

        private bool IsValidVoucher(CreateVoucherDto v)
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

            var hasAnyScope =
                v.ApplyAllCategories ||
                v.ApplyAllProducts ||
                (v.CategoryIds != null && v.CategoryIds.Any()) ||
                (v.SelectedProductIds != null && v.SelectedProductIds.Any());

            if (!hasAnyScope) return false;
            return true;
        }
    }
}