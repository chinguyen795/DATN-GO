using DATN_GO.Models;
using DATN_GO.Service;
using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Areas.Seller.Controllers
{
    [Area("Seller")] // Khai báo Area "Seller"
    public class VoucherController : Controller
    {
        private readonly VoucherService _voucherService;

        public VoucherController(VoucherService voucherService)
        {
            _voucherService = voucherService;
        }

        // GET: Voucher
        public async Task<IActionResult> Voucher()
        {
            var allVouchers = await _voucherService.GetAllVouchersAsync();
            var lowerVouchers = allVouchers.Select(v =>
            {
                v.Type = v.Type?.ToLowerInvariant();
                return v;
            }).ToList();

            return View(lowerVouchers);
        }

        // GET: Voucher/Create
        public IActionResult Create()
        {
            return View(new Vouchers());
        }

        // POST: Voucher/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Vouchers model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Gán mặc định là voucher shop
            model.Type = "shop";

            // Validate giá trị giảm
            if (model.Type == "percent")
            {
                if (model.Reduce <= 0 || model.Reduce > 100)
                {
                    ModelState.AddModelError("Reduce", "Giảm phần trăm phải từ 1% đến 100%");
                    return View(model);
                }
            }
            else if (model.Type == "amount")
            {
                if (model.Reduce < 1000)
                {
                    ModelState.AddModelError("Reduce", "Giảm tiền phải lớn hơn 1.000₫");
                    return View(model);
                }
            }

            // Validate đơn hàng tối thiểu
            if (model.MinOrder < 1000)
            {
                ModelState.AddModelError("MinOrder", "Đơn hàng tối thiểu phải lớn hơn 1.000₫");
                return View(model);
            }

            var success = await _voucherService.CreateVoucherAsync(model);
            if (success)
                return RedirectToAction("Voucher");

            ModelState.AddModelError(string.Empty, "Lỗi tạo voucher.");

            return View(model);
        }

        // POST: Voucher/Delete
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var allVouchers = await _voucherService.GetAllVouchersAsync();
            var voucher = allVouchers?.FirstOrDefault(v => v.Id == id);

            if (voucher == null)
                return NotFound();

            // Chỉ cho xóa nếu là voucher shop
            if (voucher.Type?.ToLowerInvariant() != "shop")
                return Forbid();

            var success = await _voucherService.DeleteVoucherAsync(id);
            if (success)
                return RedirectToAction("Voucher");

            return BadRequest("Xoá voucher thất bại.");
        }
    }
}
