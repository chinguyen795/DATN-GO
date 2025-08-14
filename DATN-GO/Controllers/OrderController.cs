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
        private readonly UserService _userService;

        public OrderController(OrderService orderService, UserService userService)
        {
            _orderService = orderService;
            _userService = userService;
        }
        public async Task<IActionResult> DetailOrder(int id)
        {
            var userIdStr = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userIdStr))
                return Redirect("https://localhost:7180/Login");

            if (!int.TryParse(userIdStr, out int userId))
                return Redirect("https://localhost:7180/Login");

            var order = await _orderService.GetOrderDetailByIdAsync(id, userId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng hoặc bạn không có quyền truy cập.";
                return RedirectToAction("Index");
            }

            // Lấy thêm thông tin user (giả sử bạn có một service UserService)
            var userName = HttpContext.Session.GetString("FullName") ?? "N1";
            var userNick = HttpContext.Session.GetString("Email") ?? "u2";

            ViewBag.UserFullName = userName;
            ViewBag.UserNickName = userNick;

            return View(order);
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
            var userName = HttpContext.Session.GetString("FullName") ?? "N1";
            var userNick = HttpContext.Session.GetString("Email") ?? "u2";

            ViewBag.UserFullName = userName;
            ViewBag.UserNickName = userNick;
            ViewBag.UserId = userId;
            return View(result.Data);  
        }
    }
}