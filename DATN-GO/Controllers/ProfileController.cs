using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Controllers
{
    public class ProfileController : Controller
    {
        public IActionResult Profile()
        {
            return View();
        }
    }
}
