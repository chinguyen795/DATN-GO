using DATN_GO.Models;
using DATN_GO.Service;
using DATN_GO.ViewModels;
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

        // ================== LIST ==================
        public async Task<IActionResult> Voucher(string? search, string? sort, int page = 1, int pageSize = 4)
        {
            var userIdStr = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Index", "Home", new { area = "" });

            int userId = Convert.ToInt32(userIdStr);
            var storeInfo = await _voucherService.GetStoreInfoByUserIdAsync(userId);
            int storeId = storeInfo.StoreId;

            var vouchers = await _voucherService.GetVouchersByStoreOrAdminAsync(storeId) ?? new List<Vouchers>();

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
            ViewBag.Products = await _voucherService.GetProductsByStoreAsync(storeId);
            ViewBag.StoreId = storeId;
            ViewBag.StoreName = storeInfo.StoreName;

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
            var userIdStr = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Index", "Home", new { area = "" });

            int userId = Convert.ToInt32(userIdStr);
            var storeInfo = await _voucherService.GetStoreInfoByUserIdAsync(userId);

            // ép meta cho seller
            dto.StoreId = storeInfo.StoreId;
            dto.CreatedByUserId = userId;
            dto.CreatedByRoleId = 2; // seller

            if (!IsValidVoucher(dto))
            {
                TempData["Error"] = "Phạm vi/giá trị voucher không hợp lệ.";
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
            var userIdStr = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Index", "Home", new { area = "" });

            int userId = Convert.ToInt32(userIdStr);
            var storeInfo = await _voucherService.GetStoreInfoByUserIdAsync(userId);

            dto.StoreId = storeInfo.StoreId;
            dto.CreatedByUserId = userId;
            dto.CreatedByRoleId = 2;

            if (!IsValidVoucher(dto))
            {
                TempData["Error"] = "Phạm vi/giá trị voucher không hợp lệ.";
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

            return hasAnyScope;
        }
    }
}