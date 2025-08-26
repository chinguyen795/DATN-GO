using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Controllers
{
    public class UserVouchersController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
