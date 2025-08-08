using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Controllers
{
    public class CheckoutController : Controller
    {
        [HttpGet]
        public IActionResult Success(int orderId)
        {
            ViewBag.OrderId = orderId;     // pass qua view
            return View();
        }

        [HttpGet]
        public IActionResult Failure(int? orderId, string? message)
        {
            ViewBag.OrderId = orderId;
            ViewBag.Message = message;
            return View();
        }
    }
}
