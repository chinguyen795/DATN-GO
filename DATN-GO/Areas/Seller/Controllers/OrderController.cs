using DATN_GO.ViewModels;
using DATN_GO.Service;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DATN_GO.Models;

namespace DATN_GO.Areas.Seller.Controllers
{
    [Area("Seller")]
    public class OrderController : Controller
    {
        private readonly OrderService _orderService;
        private readonly VoucherService _voucherService;

        public OrderController(OrderService orderService, VoucherService voucherService)
        {
            _orderService = orderService;
            _voucherService = voucherService;
        }

        // Action hiện danh sách đơn hàng, Model truyền lên View là List<OrderViewModel>
        public async Task<IActionResult> Order()
        {
            var userIdStr = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userIdStr))
                return Redirect("https://localhost:7180/Login");

            int userId = int.Parse(userIdStr);

            // ── NEW: Lấy StoreId/StoreName để hiển thị
            var storeInfo = await _voucherService.GetStoreInfoByUserIdAsync(userId);
            if (storeInfo != null)
            {
                ViewBag.StoreId = storeInfo.StoreId;     // NEW
                ViewBag.StoreName = storeInfo.StoreName; // NEW
            }

            var result = await _orderService.GetOrdersByStoreUserAsync(userId);

            if (!result.Success)
            {
                ViewBag.ErrorMessage = result.Message;
                return View(new List<OrderViewModel>());
            }

            ViewBag.UserId = userId;
            return View(result.Data);
        }


        // Cập nhật trạng thái đơn (giữ nguyên)
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            if (!Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
            {
                TempData["ToastMessage"] = "Trạng thái không hợp lệ";
                TempData["ToastType"] = "error";
                return RedirectToAction(nameof(Order));
            }
            var (success, message) = await _orderService.UpdateStatusAsync(id, parsedStatus.ToString());


            TempData["ToastMessage"] = message;
            TempData["ToastType"] = success ? "success" : "error";

            return RedirectToAction(nameof(Order));
        }

        // Lấy chi tiết đơn hàng, trả về JSON danh sách OrderDetailViewModel tối giản
        [HttpGet]
        public async Task<IActionResult> GetOrderDetails(int orderId)
        {
            var (success, data, message) = await _orderService.GetOrderDetailsByOrderIdAsync(orderId);
            if (!success || data == null) return Json(new List<object>());

            // Map lại cho front-end theo đúng structure frontend cần
            var result = data.Select(x => new
            {
                product = new { name = x.ProductName ?? "Không rõ" },
                quantity = x.Quantity,
                price = x.UnitPrice
            });

            return Json(result);
        }
        // Lấy thống kê
        [HttpGet]
        public async Task<IActionResult> Statistics(DateTime? start, DateTime? end, DateTime? startCompare, DateTime? endCompare)
        {
            var userIdStr = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userIdStr))
                return Redirect("https://localhost:7180/Login");

            int userId = int.Parse(userIdStr);

            // gọi service: userId chính là storeUserId
            var (success, data, message) = await _orderService.GetStatisticsAsync(userId, start, end, startCompare, endCompare);

            if (!success || data == null)
            {
                return Json(new
                {
                    totalOrders = 0,
                    pendingOrders = 0,
                    shippingOrders = 0,
                    completedOrders = 0,
                    totalOrdersPercentChange = 0,
                    pendingOrdersPercentChange = 0,
                    shippingOrdersPercentChange = 0,
                    completedOrdersPercentChange = 0
                });
            }

            return Json(new
            {
                totalOrders = data.TotalOrders,
                pendingOrders = data.PendingOrders,
                shippingOrders = data.ShippingOrders,
                completedOrders = data.CompletedOrders,

                totalOrdersPercentChange = data.TotalOrdersPercentChange,
                pendingOrdersPercentChange = data.PendingOrdersPercentChange,
                shippingOrdersPercentChange = data.ShippingOrdersPercentChange,
                completedOrdersPercentChange = data.CompletedOrdersPercentChange
            });
        }

        // Logout
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            TempData["ToastMessage"] = "Bạn đã đăng xuất thành công!";
            TempData["ToastType"] = "success";

            return RedirectToAction("Index", "Home", new { area = "" });
        }


    }
}