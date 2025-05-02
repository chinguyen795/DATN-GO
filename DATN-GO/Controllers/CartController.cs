using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Controllers
{
    public class CartController : Controller
    {
        public IActionResult Cart()
        {
            return View();
        }
    }
}
