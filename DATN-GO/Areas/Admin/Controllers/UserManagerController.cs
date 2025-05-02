using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Areas.Admin.Controllers
{
    [Area("Admin")]

    public class UserManagerController : Controller
    {
        public IActionResult UserManager()
        {
            return View();
        }
    }
}
