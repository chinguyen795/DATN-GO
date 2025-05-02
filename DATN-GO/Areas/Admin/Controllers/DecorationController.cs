using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Areas.Admin.Controllers
{
    [Area("Admin")]

    public class DecorationController : Controller
    {
        public IActionResult Decoration()
        {
            return View();
        }
    }
}
