using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class StoreController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult BrowseStore()
        {
            return View();
        }
    }
}
