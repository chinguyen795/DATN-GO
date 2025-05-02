using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Areas.Seller.Controllers
{
    [Area("Seller")]

    public class VoucherController : Controller
    {
        public IActionResult Voucher()
        {
            return View();
        }
    }
}
