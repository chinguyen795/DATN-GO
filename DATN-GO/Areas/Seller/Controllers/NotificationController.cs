using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Areas.Seller.Controllers
{
    [Area("Seller")]
    public class NotificationController : Controller
    {
        public IActionResult Notification()
        {
            return View();
        }
    }
}
