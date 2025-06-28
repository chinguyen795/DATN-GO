using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class NotificationController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
