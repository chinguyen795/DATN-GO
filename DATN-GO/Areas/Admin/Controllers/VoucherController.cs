using DATN_GO.Models;
using DATN_GO.Service;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace DATN_GO.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class VoucherController : Controller
    {
        private readonly VoucherService _voucherService;

        public VoucherController(VoucherService voucherService)
        {
            _voucherService = voucherService;
        }

        public async Task<IActionResult> Voucher(string search, string sort, int page = 1, int pageSize = 4)
        {
            List<Vouchers>? vouchers = null;
            int? storeId = null;
            if (storeId == null)
            {
                // Trường hợp admin, lấy voucher chung StoreId == null
                vouchers = await _voucherService.GetVouchersByStoreOrAdminAsync(null);
            }
            else
            {
                // Lấy voucher của store cụ thể
                vouchers = await _voucherService.GetVouchersByStoreOrAdminAsync(storeId);
            }

            if (vouchers == null)
            {
                TempData["Error"] = "Không thể tải danh sách voucher.";
                return View(Enumerable.Empty<Vouchers>());
            }
            if (!string.IsNullOrEmpty(search))
            {
                vouchers = vouchers
                    .Where(v => v.Reduce.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                v.MinOrder.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                v.Quantity.ToString().Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            ViewBag.Categories = await _voucherService.GetAllCategoriesAsync();
            ViewBag.Stores = await _voucherService.GetAllStoresAsync();
            if (!string.IsNullOrEmpty(sort))
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

            // 🧮 Phân trang
            int totalItems = vouchers.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var paginatedVouchers = vouchers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = search;
            ViewBag.Sort = sort;

            return View(paginatedVouchers);
        }
        [HttpPost]
        public async Task<IActionResult> Create(Vouchers request)
        {
            if (!IsValidVoucher(request))
            {
                TempData["Error"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";

                // ✅ In lỗi model ra để debug
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["Debug"] = string.Join(" | ", errors);

                return RedirectToAction("Voucher");
            }

            var success = await _voucherService.CreateVoucherAsync(request);
            TempData[success ? "Success" : "Error"] = success ? "Thêm voucher thành công." : "Thêm voucher thất bại.";

            return RedirectToAction("Voucher");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Vouchers request)
        {
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
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _voucherService.DeleteVoucherAsync(id);
            TempData[success ? "Success" : "Error"] = success ? "Xóa voucher thành công." : "Xóa thất bại.";
            return RedirectToAction("Voucher");
        }
        private bool IsValidVoucher(Vouchers v)
        {

            if (v.Reduce < 0) return false;
            if (v.StartDate >= v.EndDate) return false;
            return true;
        }


    }
}