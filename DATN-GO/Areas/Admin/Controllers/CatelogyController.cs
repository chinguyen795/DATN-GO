using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CatelogyController : Controller
    {
        public IActionResult Catelogy()
        {
            return View();
        }
    }
}
