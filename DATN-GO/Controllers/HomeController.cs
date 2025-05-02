using DATN_GO.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace DATN_GO.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
