using DATN_GO.Models;
using DATN_GO.Service;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

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
        public async Task<IActionResult> Voucher(string search, string sort, int page = 1, int pageSize = 4)
        {
            var userId = HttpContext.Session.GetString("Id");

            if (string.IsNullOrEmpty(userId))
            {
                TempData["ToastMessage"] = "Vui lòng đăng nhập hoặc đăng ký trước khi truy cập trang này!";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            // Lấy toàn bộ voucher
            var vouchers = await _voucherService.GetAllVouchersAsync();

            // 🔍 Lọc theo search
            if (!string.IsNullOrEmpty(search))
            {
                vouchers = vouchers
                    .Where(v => v.Reduce.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                v.MinOrder.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                v.Quantity.ToString().Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // 👤 Gán store name
            var user = await _voucherService.GetUserByIdAsync(userId);
            ViewBag.StoreName = user?.Store?.Name ?? "Huan Store";

            // 📦 Gán danh mục và store
            ViewBag.Categories = await _voucherService.GetAllCategoriesAsync();
            ViewBag.Stores = await _voucherService.GetAllStoresAsync();

            // 🔃 Sắp xếp nếu có
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVoucher(Vouchers model)
        {
            var result = await _voucherService.CreateVoucherAsync(model);

            if (result)
            {
                TempData["Success"] = "Thêm voucher thành công!";
            }
            else
            {
                TempData["Error"] = "Thêm voucher thất bại!";
            }

            return RedirectToAction("Voucher");
        }

        // GET: Lấy danh sách categories và stores
        public async Task<IActionResult> CreateVoucherModal()
        {
            var categories = await _voucherService.GetAllCategoriesAsync();
            var stores = await _voucherService.GetAllStoresAsync();

            ViewBag.Categories = categories;
            ViewBag.Stores = stores;

            return PartialView("_AddVoucherModal");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateVoucher(Vouchers voucher)
        {
            var success = await _voucherService.UpdateVoucherAsync(voucher);

            if (success)
            {
                TempData["Success"] = "Cập nhật voucher thành công!";
            }
            else
            {
                TempData["Error"] = "Cập nhật voucher thất bại!";
            }

            return RedirectToAction("Voucher");
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVoucherConfirmed(int id)
        {
            var success = await _voucherService.DeleteVoucherAsync(id);

            TempData[success ? "Success" : "Error"] = success
                ? "Xoá voucher thành công!"
                : "Xoá voucher thất bại!";

            return RedirectToAction("Voucher");
        }

    }
}