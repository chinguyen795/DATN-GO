using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Areas.Seller.Controllers
{
    [Area("Seller")]

    public class OrderController : Controller
    {
        public IActionResult Order()
        {
            return View();
        }
    }
}
