using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Areas.Seller.Controllers
{
    [Area("Seller")]

    public class FoodController : Controller
    {
        public IActionResult Food()
        {
            return View();
        }

        public IActionResult CreateFood()
        {
            return View();
        }
    }
}
