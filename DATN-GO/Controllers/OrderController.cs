using DATN_GO.Service;
using DATN_GO.ViewModels;
using DATN_GO.ViewModels.Orders;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_GO.Controllers
{
    public class OrderController : Controller
    {
        private readonly OrderService _orderService;
        private readonly UserService _userService;
        private readonly StoreService _storeService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(OrderService orderService, UserService userService, ILogger<OrderController> logger, StoreService storeService)
        {
            _orderService = orderService;
            _userService = userService;
            _logger = logger;
            _storeService = storeService;
        }
        private async Task SetUserHeaderAsync(int userId)
        {
            // Lấy từ service (ưu tiên)
            var user = await _userService.GetUserByIdAsync(userId); // đổi tên method theo service của bạn
            var session = HttpContext.Session;

            // Dự phòng từ Session (khi service null hoặc thiếu)
            var sessionFullName = session.GetString("FullName");
            var sessionEmail = session.GetString("Email");
            var sessionAvatar = session.GetString("Avatar");

            var fullName = !string.IsNullOrWhiteSpace(user?.FullName) ? user.FullName : (sessionFullName ?? string.Empty);
            var email = !string.IsNullOrWhiteSpace(user?.Email) ? user.Email : (sessionEmail ?? string.Empty);
            var avatar = !string.IsNullOrWhiteSpace(user?.Avatar) ? user.Avatar : (sessionAvatar ?? string.Empty);

            // Đổ ra ViewBag cho View dùng ngay
            ViewBag.UserFullName = string.IsNullOrWhiteSpace(fullName) ? "Người dùng" : fullName;
            ViewBag.UserNickName = string.IsNullOrWhiteSpace(email) ? null : email;   // View sẽ hiển thị nhỏ bên dưới
            ViewBag.Avatar = string.IsNullOrWhiteSpace(avatar)
                ? null
                : avatar; // View đã có fallback ảnh, không ép tại controller

            // Bổ sung/đồng bộ Session nếu thiếu (không override khi đã có)
            if (string.IsNullOrWhiteSpace(sessionFullName) && !string.IsNullOrWhiteSpace(fullName))
                session.SetString("FullName", fullName);

            if (string.IsNullOrWhiteSpace(sessionEmail) && !string.IsNullOrWhiteSpace(email))
                session.SetString("Email", email);

            if (string.IsNullOrWhiteSpace(sessionAvatar) && !string.IsNullOrWhiteSpace(avatar))
                session.SetString("Avatar", avatar);
        }

        // GET: /Order/DetailOrder/{id}
        public async Task<IActionResult> DetailOrder(int id)
        {
            var userIdStr = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Redirect("https://localhost:7180/Login");
            await SetUserHeaderAsync(userId);
            var store = await _storeService.GetStoreByUserIdAsync(userId);
            ViewData["StoreStatus"] = store?.Status; // enum StoreStatus
            // Lấy OrderDetailVM trong DATN_GO.ViewModels
            OrderDetailVM order = await _orderService.GetOrderDetailByIdAsync(id, userId);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng hoặc bạn không có quyền truy cập.";
                return RedirectToAction("Index");
            }

            if (order.TotalPrice <= 0)
            {
                var itemsTotal = order.ItemsTotal;
                order.TotalPrice = itemsTotal + order.DeliveryFee;
            }

            ViewBag.UserFullName = HttpContext.Session.GetString("FullName") ?? "N1";
            ViewBag.UserNickName = HttpContext.Session.GetString("Email") ?? "u2";
            ViewBag.Crimson = "#dc143c";

            if (!string.IsNullOrWhiteSpace(order.LabelId))
            {
                ViewBag.TrackingUrl = $"https://khachhang-staging.ghtklab.com/web/";
            }

            return View(order); // trả về OrderDetailVM (DATN_GO.ViewModels)
        }

        // GET: /Order
        public async Task<IActionResult> Index()
        {
            var userIdStr = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Redirect("https://localhost:7180/Login");
            await SetUserHeaderAsync(userId);
            var result = await _orderService.GetOrdersByUserAsync(userId);
            if (!result.Success)
            {
                ViewBag.ErrorMessage = result.Message;
                return View(new List<OrderViewModel>());
            }

                var store = await _storeService.GetStoreByUserIdAsync(userId);
                ViewData["StoreStatus"] = store?.Status; // enum StoreStatus
            

            ViewBag.UserFullName = HttpContext.Session.GetString("FullName") ?? "N1";
            ViewBag.UserNickName = HttpContext.Session.GetString("Email") ?? "u2";
            ViewBag.UserId = userId;

            return View(result.Data); // View dùng @model List<OrderViewModel>
        }
        [HttpPost]
        public async Task<IActionResult> CheckoutCOD(int id)
        {
            var (success, msg) = await _orderService.CheckoutCODAsync(id);
            if (!success)
            {
                TempData["ErrorMessage"] = msg;
                return RedirectToAction("DetailOrder", new { id });
            }

            TempData["SuccessMessage"] = "Đặt hàng COD thành công!";
            return RedirectToAction("DetailOrder", new { id });
        }

        
        public async Task<IActionResult> CancelOrderConfirm(int id)
        {
            var userIdStr = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Redirect("https://localhost:7180/Login");
            var store = await _storeService.GetStoreByUserIdAsync(userId);
            ViewData["StoreStatus"] = store?.Status; // enum StoreStatus
            var order = await _orderService.GetOrderDetailByIdAsync(id, userId);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng hoặc bạn không có quyền hủy.";
                return RedirectToAction("Index");
            }

            return View(order); // View dùng @model OrderDetailVM
        }

        public async Task<IActionResult> CancelOrder(int id)
        {
            var userIdStr = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized(new { message = "Bạn cần đăng nhập để hủy đơn." });

            var result = await _orderService.CancelOrderAsync(id, userId);

            // Ghi log
            if (result.Success)
            {
                _logger.LogInformation("User {UserId} đã hủy đơn hàng {OrderId} thành công.", userId, id);
            }
            else
            {
                _logger.LogWarning("User {UserId} hủy đơn hàng {OrderId} thất bại. {Message}", userId, id, result.Message);
            }

            return RedirectToAction("DetailOrder", new { id });
        }


    }
}