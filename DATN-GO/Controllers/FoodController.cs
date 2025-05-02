using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Controllers
{
    public class FoodController : Controller
    {
        public IActionResult Food()
        {
            return View();
        }

        public IActionResult DetailFood()
        {
            return View();
        }
    }
}
