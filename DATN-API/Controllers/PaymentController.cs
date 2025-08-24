using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using DATN_API.Services.Interfaces;
using DATN_API.ViewModels.Vnpay;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ICartService _cartService;
        private readonly IVNPayService _vnp;
        private readonly IOrdersService _orders;

        public PaymentController(ApplicationDbContext db, ICartService cartService, IVNPayService vnp, IOrdersService orders)
        {
            _db = db; _cartService = cartService; _vnp = vnp; _orders = orders;
        }

        // DTO từ MVC gửi lên khi bấm thanh toán
        public class CreateVnpOrderRequest
        {
            public int UserId { get; set; }
            public int AddressId { get; set; }
            public int? UserVoucherId { get; set; }
        }

        [HttpPost("vnpay-create")]
        public async Task<IActionResult> CreateVnpay([FromBody] CreateVnpOrderRequest req)
        {
            // 1) Lấy giỏ đã chọn + tính ship theo nhóm store
            var cart = await _cartService.GetCartByUserIdAsync(req.UserId);
            if (cart == null) return BadRequest("Cart not found");

            var shippingGroups = await _cartService.GetShippingGroupsByUserIdAsync(req.UserId, req.AddressId);
            var shippingFee = shippingGroups.Sum(g => g.ShippingFee);

            var selected = cart.CartItems.Where(x => x.IsSelected).ToList();
            if (!selected.Any()) return BadRequest("Không có sản phẩm nào được chọn.");

            var subTotal = selected.Sum(x => x.TotalValue);
            decimal voucherReduce = 0;

            // 2) Áp voucher (nếu có)
            if (req.UserVoucherId.HasValue)
            {
                var voucher = cart.Vouchers.FirstOrDefault(v => v.Id == req.UserVoucherId.Value);
                if (voucher != null) voucherReduce = voucher.Reduce;
            }

            var grandTotal = (long)Math.Max(0, subTotal + shippingFee - voucherReduce);

            // 2.5) Đảm bảo có ShippingMethodId hợp lệ (FK NOT NULL)
            // Lấy đại diện 1 store từ item đã chọn
            var representativeStoreId = selected.First().StoreId;
            var shipMethod = await _db.ShippingMethods
                .FirstOrDefaultAsync(sm => sm.StoreId == representativeStoreId && sm.MethodName == "GHTK_AUTO");

            if (shipMethod == null)
            {
                shipMethod = new ShippingMethods
                {
                    StoreId = representativeStoreId,
                    MethodName = "GHTK_AUTO",
                    Price = 0 // phí thực sẽ nằm ở DeliveryFee của Order
                };
                _db.ShippingMethods.Add(shipMethod);
                await _db.SaveChangesAsync();
            }

            // 3) Tạo Order (pending)
            var order = new Orders
            {
                UserId = req.UserId,
                OrderDate = DateTime.UtcNow,
                PaymentMethod = "VNPay",
                PaymentStatus = "Unpaid",          // string
                Status = OrderStatus.ChoXuLy,      // enum
                TotalPrice = grandTotal,
                DeliveryFee = shippingFee,
                VoucherId = null,
                ShippingMethodId = shipMethod.Id   // <-- quan trọng
            };
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            // (option) Tạo OrderDetails từ cart đã chọn
            foreach (var item in selected)
            {
                _db.OrderDetails.Add(new OrderDetails
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price
                });
            }
            await _db.SaveChangesAsync();

            // 4) Gọi service tạo paymentUrl
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var paymentUrl = _vnp.CreatePaymentUrl(new VnpCreatePaymentRequest
            {
                OrderId = order.Id.ToString(),                 // vnp_TxnRef
                Amount = grandTotal,                           // VND
                OrderInfo = $"Thanh toan don hang #{order.Id}",
                IpAddress = clientIp,
                Locale = "vn"
            });

            return Ok(new { paymentUrl });
        }

        // Controllers/PaymentController.cs

        [HttpGet("vnpay-return")]
        public async Task<IActionResult> VnpReturn()
        {
            var raw = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
            var isValid = _vnp.ValidateReturn(raw);

            var orderId = raw.TryGetValue("vnp_TxnRef", out var refId) ? refId : null;
            var rspCode = raw.TryGetValue("vnp_ResponseCode", out var rc) ? rc : null;
            if (orderId == null) return BadRequest("Missing order id");

            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id.ToString() == orderId);
            if (order == null) return NotFound("Order not found");

            if (isValid && rspCode == "00")
            {
                order.PaymentStatus = "Paid";
                order.Status = OrderStatus.DaHoanThanh;   // hoặc trạng thái bạn dùng
                order.PaymentDate = DateTime.UtcNow;

                // 💥 xoá các item đã tick trong giỏ
                try { await _cartService.ClearSelectedAsync(order.UserId); } catch { }

                await _db.SaveChangesAsync();
                // 🔗 Tạo đơn GHTK & lưu LabelId (tránh trùng)
                try
                {
                    if (string.IsNullOrEmpty(order.LabelId))
                    {
                        var label = await _orders.PushOrderToGhtkAndSaveLabelAsync(order.Id);
                        // optional: log label
                    }
                }
                catch { /* log nếu cần */ }

                return Redirect($"https://localhost:7180/Checkout/Success?orderId={order.Id}");
            }
            else
            {
                if (order.PaymentStatus != "Paid")
                {
                    order.PaymentStatus = "Failed";
                    order.Status = OrderStatus.ChoXuLy;
                    await _db.SaveChangesAsync();
                }
                return Redirect($"https://localhost:7180/Checkout/Failure?orderId={order.Id}");
            }
        }

        [HttpGet("vnpay-ipn")]
        public async Task<IActionResult> VnpIpn()
        {
            var query = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
            var valid = _vnp.ValidateReturn(query);
            var rspCode = query.GetValueOrDefault("vnp_ResponseCode");
            var txnRef = query.GetValueOrDefault("vnp_TxnRef");

            if (!valid) return new JsonResult(new { RspCode = "97", Message = "Invalid signature" });

            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id.ToString() == txnRef);
            if (order == null) return new JsonResult(new { RspCode = "01", Message = "Order not found" });

            if (rspCode == "00")
            {
                if (order.PaymentStatus != "Paid")
                {
                    order.PaymentStatus = "Paid";
                    order.Status = OrderStatus.DaHoanThanh;
                    order.PaymentDate = DateTime.UtcNow;

                    // 💥 idempotent: cứ thử xoá, nếu hết rồi thì count=0
                    try { await _cartService.ClearSelectedAsync(order.UserId); } catch { }

                    await _db.SaveChangesAsync();
                    // 🔗 Tạo đơn GHTK & lưu LabelId (tránh trùng)
                    try
                    {
                        if (string.IsNullOrEmpty(order.LabelId))
                        {
                            var label = await _orders.PushOrderToGhtkAndSaveLabelAsync(order.Id);
                        }
                    }
                    catch { /* log nếu cần */ }
                }
                return new JsonResult(new { RspCode = "00", Message = "Success" });
            }
            else
            {
                if (order.PaymentStatus != "Paid")
                {
                    order.PaymentStatus = "Failed";
                    await _db.SaveChangesAsync();
                }
                return new JsonResult(new { RspCode = "00", Message = "Success" });
            }
        }
        [HttpPost("test-ghtk/{orderId:int}")]
        public async Task<IActionResult> TestGhtk(int orderId)
        {
            var label = await _orders.PushOrderToGhtkAndSaveLabelAsync(orderId);
            return Ok(new { orderId, label });
        }
        [HttpPost("cod/{orderId}")]
        public async Task<IActionResult> CheckoutCOD(int orderId)
        {
            var label = await _orders.PushOrderToGhtkAndSaveLabelCodAsync(orderId);

            if (string.IsNullOrEmpty(label))
                return BadRequest(new { message = "Không tạo được đơn COD bên GHTK." });

            return Ok(new
            {
                message = "Đặt hàng COD thành công.",
                labelId = label
            });
        }
        [HttpPost("cod-create")]
        public async Task<IActionResult> CreateCod([FromBody] CreateVnpOrderRequest req)
        {
            // 1) Lấy giỏ đã tick + phí ship
            var cart = await _cartService.GetCartByUserIdAsync(req.UserId);
            if (cart == null) return BadRequest("Cart not found");

            var shippingGroups = await _cartService.GetShippingGroupsByUserIdAsync(req.UserId, req.AddressId);
            var shippingFee = shippingGroups.Sum(g => g.ShippingFee);

            var selected = cart.CartItems.Where(x => x.IsSelected).ToList();
            if (!selected.Any()) return BadRequest("Không có sản phẩm nào được chọn.");

            var subTotal = selected.Sum(x => x.TotalValue);
            decimal voucherReduce = 0;
            if (req.UserVoucherId.HasValue)
            {
                var voucher = cart.Vouchers.FirstOrDefault(v => v.Id == req.UserVoucherId.Value);
                if (voucher != null) voucherReduce = voucher.Reduce;
            }
            var grandTotal = (long)Math.Max(0, subTotal + shippingFee - voucherReduce);

            // 2) ShippingMethod đại diện (store của item đầu tiên)
            var representativeStoreId = selected.First().StoreId;
            var shipMethod = await _db.ShippingMethods
                .FirstOrDefaultAsync(sm => sm.StoreId == representativeStoreId && sm.MethodName == "GHTK_AUTO");
            if (shipMethod == null)
            {
                shipMethod = new ShippingMethods
                {
                    StoreId = representativeStoreId,
                    MethodName = "GHTK_AUTO",
                    Price = 0 // phí thực tế nằm trong Orders.DeliveryFee
                };
                _db.ShippingMethods.Add(shipMethod);
                await _db.SaveChangesAsync();
            }

            // 3) Tạo đơn COD
            var order = new Orders
            {
                UserId = req.UserId,
                OrderDate = DateTime.UtcNow,
                PaymentMethod = "COD",
                PaymentStatus = "Unpaid",          // COD: chưa thanh toán
                Status = OrderStatus.ChoXuLy,      // vừa đặt, chuẩn bị đẩy hãng vận chuyển
                TotalPrice = grandTotal,
                DeliveryFee = shippingFee,
                VoucherId = null,
                ShippingMethodId = shipMethod.Id
            };
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            foreach (var item in selected)
            {
                _db.OrderDetails.Add(new OrderDetails
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price
                });
            }
            await _db.SaveChangesAsync();

            // 4) Đẩy đơn sang GHTK với COD (thu hộ)
            string? label = await _orders.PushOrderToGhtkAndSaveLabelCodAsync(order.Id);
            if (string.IsNullOrWhiteSpace(label))
                return BadRequest(new { message = "Không tạo được đơn COD bên GHTK." });

            // 5) Cập nhật trạng thái hợp lý & dọn giỏ
            order.Status = OrderStatus.ChoLayHang;   // đã có vận đơn, chờ GHTK đến lấy hàng
            await _db.SaveChangesAsync();

            try { await _cartService.ClearSelectedAsync(order.UserId); } catch { /* optional log */ }

            // 6) Trả về cho MVC
            return Ok(new { orderId = order.Id, labelId = label });
        }


    }
}