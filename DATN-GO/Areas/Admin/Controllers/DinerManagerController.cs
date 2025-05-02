using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Areas.Admin.Controllers
{
    [Area("Admin")]

    public class DinerManagerController : Controller
    {
        public IActionResult DinerManager()
        {
            return View();
        }

        public IActionResult DineDetail()
        {
            return View();
        }
    }
}
