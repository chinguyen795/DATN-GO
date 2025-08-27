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
        private readonly IVouchersService _vouchers;

        public PaymentController(ApplicationDbContext db, ICartService cartService, IVNPayService vnp, IOrdersService orders, IVouchersService vouchers) // <-- inject)
        {
            _db = db; _cartService = cartService; _vnp = vnp; _orders = orders; _vouchers = vouchers;
        }

        // DTO từ MVC gửi lên khi bấm thanh toán
        public class CreateVnpOrderRequest
        {
            public int UserId { get; set; }
            public int AddressId { get; set; }
            public int? UserVoucherId { get; set; }
        }

        // ===================== VNPay =====================
        [HttpPost("vnpay-create")]
        public async Task<IActionResult> CreateVnpay([FromBody] CreateVnpOrderRequest req)
        {
            // 1) Lấy giỏ đã chọn + tính ship theo nhóm store
            var cart = await _cartService.GetCartByUserIdAsync(req.UserId);
            if (cart == null) return BadRequest("Cart not found");

            var shippingGroups = await _cartService.GetShippingGroupsByUserIdAsync(req.UserId, req.AddressId);
            if (shippingGroups == null || !shippingGroups.Any())
                return BadRequest("Không tính được phí vận chuyển.");

            var shippingFee = shippingGroups.Sum(g => g.ShippingFee);

            var selected = cart.CartItems.Where(x => x.IsSelected).ToList();
            if (!selected.Any()) return BadRequest("Không có sản phẩm nào được chọn.");

            var subTotal = selected.Sum(x => x.TotalValue);

            // 2) Áp voucher (nếu có)
            // 2) Áp voucher (nếu có) – KIỂM TRA TỪ DB để biết UsedCount/Quantity, hạn dùng...
            // ===== Validate voucher dựa trên req.UserVoucherId =====
            decimal voucherReduce = 0m;
            int? voucherIdToSave = null;

            if (req.UserVoucherId is int vid)
            {
                var v = await _db.Vouchers.FirstOrDefaultAsync(x => x.Id == vid);
                if (v == null)
                    return BadRequest(new { message = "Voucher không tồn tại." });

                if (v.UsedCount >= v.Quantity)
                    return BadRequest(new { message = "Voucher đã hết lượt sử dụng." });

                var now = DateTime.UtcNow;
                if (now < v.StartDate || now > v.EndDate)
                    return BadRequest(new { message = "Voucher đã hết hạn hoặc chưa bắt đầu." });

                if (subTotal < v.MinOrder)
                    return BadRequest(new { message = $"Đơn tối thiểu {v.MinOrder:N0} đ mới dùng được voucher." });

                // OK: áp dụng
                voucherReduce = v.Reduce;   // (nếu dùng % thì đổi sang công thức % của bạn)
                voucherIdToSave = vid;      // LƯU Ý: vẫn là req.UserVoucherId
            }

            var grandTotal = (long)Math.Max(0, subTotal + shippingFee - voucherReduce);



            // 2.5) ShippingMethod đại diện
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

            // 3) Tạo Order (pending) – giữ tổng CUỐI SAU GIẢM
            var order = new Orders
            {
                UserId = req.UserId,
                OrderDate = DateTime.UtcNow,
                PaymentMethod = "VNPay",
                PaymentStatus = "Unpaid",
                Status = OrderStatus.ChoXuLy,
                TotalPrice = grandTotal,     // ✅ tổng cuối
                DeliveryFee = shippingFee,
                VoucherId = voucherIdToSave,       // ✅ lưu voucher đã chọn
                ShippingMethodId = shipMethod.Id
            };
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            // 3.5) Tạo OrderDetails từ cart đã chọn
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

            // 4) Tạo paymentUrl VNPay
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var paymentUrl = _vnp.CreatePaymentUrl(new VnpCreatePaymentRequest
            {
                OrderId = order.Id.ToString(),     // vnp_TxnRef
                Amount = grandTotal,               // VND
                OrderInfo = $"Thanh toan don hang #{order.Id}",
                IpAddress = clientIp,
                Locale = "vn"
            });

            return Ok(new { paymentUrl });
        }

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
                order.Status = OrderStatus.ChoLayHang;
                order.PaymentDate = DateTime.UtcNow;
                // ✅ trừ 1 lượt voucher nếu có
                if (order.VoucherId.HasValue)
                    try { await _vouchers.RedeemVoucherAsync(order.VoucherId.Value); } catch { }
                try { await _cartService.ClearSelectedAsync(order.UserId); } catch { }
                await _db.SaveChangesAsync();

                try
                {
                    if (string.IsNullOrEmpty(order.LabelId))
                    {
                        var label = await _orders.PushOrderToGhtkAndSaveLabelAsync(order.Id);
                    }
                }
                catch { }

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
                    // ✅ trừ 1 lượt voucher nếu có
                    if (order.VoucherId.HasValue)
                        try { await _vouchers.RedeemVoucherAsync(order.VoucherId.Value); } catch { }
                    try { await _cartService.ClearSelectedAsync(order.UserId); } catch { }
                    await _db.SaveChangesAsync();

                    try
                    {
                        if (string.IsNullOrEmpty(order.LabelId))
                        {
                            var label = await _orders.PushOrderToGhtkAndSaveLabelAsync(order.Id);
                        }
                    }
                    catch { }
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

        // ===================== Test / COD =====================
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

            return Ok(new { message = "Đặt hàng COD thành công.", labelId = label });
        }

        // Tạo đơn COD từ giỏ (giống VNPay nhưng không qua cổng thanh toán)
        [HttpPost("cod-create")]
        public async Task<IActionResult> CreateCod([FromBody] CreateVnpOrderRequest req)
        {
            if (req == null || req.UserId <= 0 || req.AddressId <= 0)
                return BadRequest(new { message = "Thiếu UserId/AddressId" });

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var cart = await _cartService.GetCartByUserIdAsync(req.UserId);
                if (cart == null) return BadRequest(new { message = "Cart not found" });

                var shippingGroups = await _cartService.GetShippingGroupsByUserIdAsync(req.UserId, req.AddressId);
                if (shippingGroups == null || !shippingGroups.Any())
                    return BadRequest(new { message = "Không tính được phí vận chuyển." });

                var shippingFee = shippingGroups.Sum(g => g.ShippingFee);

                var selected = cart.CartItems.Where(x => x.IsSelected).ToList();
                if (!selected.Any()) return BadRequest(new { message = "Không có sản phẩm nào được chọn." });

                var subTotal = selected.Sum(x => x.TotalValue);

                // Voucher
                // ===== Validate voucher dựa trên req.UserVoucherId =====
                decimal voucherReduce = 0m;
                int? voucherIdToSave = null;

                if (req.UserVoucherId is int vid)
                {
                    var v = await _db.Vouchers.FirstOrDefaultAsync(x => x.Id == vid);
                    if (v == null)
                        return BadRequest(new { message = "Voucher không tồn tại." });

                    if (v.UsedCount >= v.Quantity)
                        return BadRequest(new { message = "Voucher đã hết lượt sử dụng." });

                    var now = DateTime.UtcNow;
                    if (now < v.StartDate || now > v.EndDate)
                        return BadRequest(new { message = "Voucher đã hết hạn hoặc chưa bắt đầu." });

                    if (subTotal < v.MinOrder)
                        return BadRequest(new { message = $"Đơn tối thiểu {v.MinOrder:N0} đ mới dùng được voucher." });

                    // OK: áp dụng
                    voucherReduce = v.Reduce;   // (nếu dùng % thì đổi sang công thức % của bạn)
                    voucherIdToSave = vid;      // LƯU Ý: vẫn là req.UserVoucherId
                }

                var grandTotal = (long)Math.Max(0, subTotal + shippingFee - voucherReduce);


                // ShippingMethod
                var representativeStoreId = selected.First().StoreId;
                var shipMethod = await _db.ShippingMethods
                    .FirstOrDefaultAsync(sm => sm.StoreId == representativeStoreId && sm.MethodName == "GHTK_AUTO");

                if (shipMethod == null)
                {
                    shipMethod = new ShippingMethods
                    {
                        StoreId = representativeStoreId,
                        MethodName = "GHTK_AUTO",
                        Price = 0
                    };
                    _db.ShippingMethods.Add(shipMethod);
                    await _db.SaveChangesAsync();
                }

                // Tạo đơn COD (pending) – tổng CUỐI
                var order = new Orders
                {
                    UserId = req.UserId,
                    OrderDate = DateTime.UtcNow,
                    PaymentMethod = "COD",
                    PaymentStatus = "Unpaid",
                    Status = OrderStatus.ChoXuLy,
                    TotalPrice = grandTotal,   // ✅ tổng cuối (đã trừ voucher)
                    DeliveryFee = shippingFee,
                    VoucherId = voucherIdToSave,       // ✅ lưu voucher
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

                // Đẩy đơn COD sang GHTK
                string? label;
                try
                {
                    label = await _orders.PushOrderToGhtkAndSaveLabelCodAsync(order.Id);
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    try
                    {
                        _db.OrderDetails.RemoveRange(_db.OrderDetails.Where(d => d.OrderId == order.Id));
                        _db.Orders.Remove(order);
                        await _db.SaveChangesAsync();
                    }
                    catch { }
                    return BadRequest(new { message = $"GHTK lỗi: {ex.Message}" });
                }

                if (string.IsNullOrWhiteSpace(label))
                {
                    await tx.RollbackAsync();
                    try
                    {
                        _db.OrderDetails.RemoveRange(_db.OrderDetails.Where(d => d.OrderId == order.Id));
                        _db.Orders.Remove(order);
                        await _db.SaveChangesAsync();
                    }
                    catch { }
                    return BadRequest(new { message = "Không tạo được đơn COD bên GHTK (không có labelId)." });
                }
                // ✅ Trừ 1 lượt voucher khi COD tạo vận đơn thành công
                if (order.VoucherId.HasValue)
                {
                    var (ok, reason) = await _vouchers.RedeemVoucherAsync(order.VoucherId.Value);
                    if (!ok)
                    {
                        // Nếu voucher hết lượt => hủy đơn & hủy label cho sạch
                        try { await _orders.CancelOrderAsync(order.Id, req.UserId); } catch { }
                        await tx.RollbackAsync();
                        return BadRequest(new { message = $"Voucher không còn lượt: {reason}" });
                    }
                }
                // Cập nhật & dọn giỏ
                order.Status = OrderStatus.ChoXuLy;
                order.LabelId = label;
                await _db.SaveChangesAsync();

                try { await _cartService.ClearSelectedAsync(order.UserId); } catch { }

                await tx.CommitAsync();
                return Ok(new { orderId = order.Id, labelId = label });
            }
            catch (Exception e)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { message = "Lỗi hệ thống khi tạo đơn COD.", detail = e.Message });
            }
        }
    }
}
