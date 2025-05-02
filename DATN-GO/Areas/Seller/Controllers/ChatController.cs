using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Areas.Seller.Controllers
{
    [Area("Seller")]

    public class ChatController : Controller
    {
        public IActionResult Chat()
        {
            return View();
        }
    }
}
