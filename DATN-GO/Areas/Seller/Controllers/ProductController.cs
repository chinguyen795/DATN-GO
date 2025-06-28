using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Areas.Seller.Controllers
{
    [Area("Seller")]

    public class ProductController : Controller
    {
        public IActionResult Product()
        {
            return View();
        }
        public IActionResult CreateProduct()
        {
            return View();
        }

    }
}
