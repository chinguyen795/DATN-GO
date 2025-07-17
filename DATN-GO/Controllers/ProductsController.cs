using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Controllers
{
    public class ProductsController : Controller
    {
        [HttpGet]
        public IActionResult Products()
        {
            return View();
        }

        public IActionResult DetailProducts()
        {
            return View();
        }
    }
}
