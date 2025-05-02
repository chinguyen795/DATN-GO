using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Areas.Seller.Controllers
{
    [Area("Seller")]
    public class CategoryController : Controller
    {
        public IActionResult Category()
        {
            return View();
        }
    }
}
