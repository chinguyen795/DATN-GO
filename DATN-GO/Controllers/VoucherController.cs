using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Controllers
{
    public class VoucherController : Controller
    {
        public IActionResult Voucher()
        {
            return View();
        }
    }
}