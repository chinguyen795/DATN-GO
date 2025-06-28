using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Areas.Seller.Controllers
{
    [Area("Seller")]
    public class PostArticleController : Controller
    {
        public IActionResult PostArticle()
        {
            return View();
        }
    }
}
