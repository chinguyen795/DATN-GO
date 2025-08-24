using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Controllers
{
    public class ChatController : Controller
    {
        public IActionResult Index() => View();
    }
}