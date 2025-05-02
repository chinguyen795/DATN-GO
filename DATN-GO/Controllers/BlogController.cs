using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Controllers
{
    public class BlogController : Controller
    {
        public IActionResult Blog()
        {
            return View();
        }
    }
}
