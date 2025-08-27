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
        // Kiểm tra vai trò trước khi vào area Seller
        private async Task<bool> IsUserSeller(int userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            return user != null && user.RoleId == 1;
        }

        public async Task<IActionResult> Voucher(string search, string sort, int page = 1, int pageSize = 4)
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

            // Thay vì chuyển thành AnonymousType, bạn trả về danh sách Vouchers đã chuyển đổi
            var paginatedVouchers = vouchers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Cập nhật ViewBag cho các giá trị cần thiết
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = search;
            ViewBag.Sort = sort;

            // Trả về danh sách Vouchers
            return View(paginatedVouchers);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVoucher(Vouchers model)
        {
            if (model == null)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ!";
                return RedirectToAction("Voucher");
            }

            // Gọi service để tạo voucher
            var result = await _voucherService.CreateVoucherAsync(model);

            if (result)
            {
                TempData["Success"] = "Thêm voucher thành công!";
            }
            else
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin và định dạng!";
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

        // Logout
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