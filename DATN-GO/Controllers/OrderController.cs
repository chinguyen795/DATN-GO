using DATN_GO.Service;
using DATN_GO.ViewModels;
using DATN_GO.ViewModels.Orders;   // <-- THÊM: để dùng OrderDetailVM
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

        // GET: /Order/DetailOrder/{id}
        public async Task<IActionResult> DetailOrder(int id)
        {
            // Lấy userId từ session
            var userIdStr = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Redirect("https://localhost:7180/Login");

            // Lấy OrderDetailVM đã map sẵn (Items, ItemsTotal, DeliveryFee, TotalPrice, LabelId...)
            var order = await _orderService.GetOrderDetailByIdAsync(id, userId);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng hoặc bạn không có quyền truy cập.";
                return RedirectToAction("Index");
            }

            // Fallback: nếu TotalPrice từ API = 0, tự cộng cho chắc
            if (order.TotalPrice <= 0)
            {
                var itemsTotal = order.ItemsTotal;
                order.TotalPrice = itemsTotal + order.DeliveryFee;
            }

            // Thông tin hiển thị phụ
            ViewBag.UserFullName = HttpContext.Session.GetString("FullName") ?? "N1";
            ViewBag.UserNickName = HttpContext.Session.GetString("Email") ?? "u2";
            ViewBag.Crimson = "#dc143c";

            // (tuỳ chọn) Tracking link nếu có LabelId
            // Ví dụ với GHTK web: https://khachhang-staging.ghtklab.com/web/ (bạn có thể map theo môi trường)
            if (!string.IsNullOrWhiteSpace(order.LabelId))
            {
                ViewBag.TrackingUrl = $"https://khachhang-staging.ghtklab.com/web/"; // có thể thay bằng URL tracking cụ thể nếu bạn có
            }

            return View(order); // View dùng @model DATN_GO.ViewModels.Orders.OrderDetailVM
        }

        // GET: /Order
        public async Task<IActionResult> Index()
        {
            var userIdStr = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Redirect("https://localhost:7180/Login");

            var result = await _orderService.GetOrdersByUserAsync(userId);
            if (!result.Success)
            {
                ViewBag.ErrorMessage = result.Message;
                return View(new List<OrderViewModel>());
            }

            ViewBag.UserFullName = HttpContext.Session.GetString("FullName") ?? "N1";
            ViewBag.UserNickName = HttpContext.Session.GetString("Email") ?? "u2";
            ViewBag.UserId = userId;

            return View(result.Data); // View dùng @model List<OrderViewModel>
        }
    }
}