using DATN_GO.Service;
using DATN_GO.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_GO.Controllers
{
    public class OrderController : Controller
    {
        private readonly OrderService _orderService;

        public OrderController(OrderService orderService)
        {
            _orderService = orderService;
        }

        public async Task<IActionResult> Index()
        {
            var userIdStr = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userIdStr))
                return Redirect("https://localhost:7180/Login");

            if (!int.TryParse(userIdStr, out int userId))
                return Redirect("https://localhost:7180/Login");

            var result = await _orderService.GetOrdersByUserAsync(userId);

            if (!result.Success)
            {
                ViewBag.ErrorMessage = result.Message;
                return View(new List<OrderViewModel>());  // Tên ViewModel chuẩn
            }

            ViewBag.UserId = userId;
            return View(result.Data);  // Đây là List<OrderViewModel>
        }
    }
}