using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Controllers
{
    public class DinerController : Controller
    {
        public IActionResult Diner()
        {
            return View();
        }

        public IActionResult DetailDiner()
        {
            return View();
        }
    }
}
