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
        private readonly IMomoService _momo;

        public PaymentController(ApplicationDbContext db, ICartService cartService, IVNPayService vnp, IOrdersService orders, IMomoService momo)
        {
            _db = db; _cartService = cartService; _vnp = vnp; _orders = orders; _momo = momo;
        }

        // DTO từ MVC gửi lên khi bấm thanh toán
        public class CreateVnpOrderRequest
        {
            public int UserId { get; set; }
            public int AddressId { get; set; }
            public int? UserVoucherId { get; set; }
        }

        // Giờ Việt Nam (UTC+7), chạy được cả Windows/Linux/Docker
        private static DateTime NowVn()
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");   // Linux/Docker
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            }
            catch
            {
                try
                {
                    var tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); // Windows
                    return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
                }
                catch
                {
                    return DateTime.UtcNow.AddHours(7); // fallback
                }
            }
        }

        // =============== TRỪ KHO 1 LẦN (idempotent) ===============
        private async Task DeductStockIfNeededAsync(int orderId)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null || order.IsStockDeducted) return;

            var details = await _db.OrderDetails
                .Where(d => d.OrderId == orderId)
                .Select(d => new { d.ProductId, d.ProductVariantId, d.Quantity })
                .ToListAsync();

            if (details.Count == 0)
            {
                order.IsStockDeducted = true;
                await _db.SaveChangesAsync();
                return;
            }

            // 1) Trừ kho theo Variant trước
            var byVariant = details
                .Where(x => x.ProductVariantId.HasValue)
                .GroupBy(x => x.ProductVariantId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

            if (byVariant.Count > 0)
            {
                var vIds = byVariant.Keys.ToList();
                var variants = await _db.ProductVariants
                    .Where(v => vIds.Contains(v.Id))
                    .ToDictionaryAsync(v => v.Id);

                foreach (var (vid, qty) in byVariant)
                {
                    if (!variants.TryGetValue(vid, out var v)) continue;
                    var after = v.Quantity - qty;
                    v.Quantity = after < 0 ? 0 : after;
                }
            }

            // 2) Dòng không có variant -> trừ kho Product
            var byProduct = details
                .Where(x => !x.ProductVariantId.HasValue)
                .GroupBy(x => x.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

            if (byProduct.Count > 0)
            {
                var pIds = byProduct.Keys.ToList();
                var products = await _db.Products
                    .Where(p => pIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id);

                foreach (var (pid, qty) in byProduct)
                {
                    if (!products.TryGetValue(pid, out var p)) continue;
                    var after = p.Quantity - qty;
                    p.Quantity = after < 0 ? 0 : after;
                }
            }

            order.IsStockDeducted = true;
            await _db.SaveChangesAsync();
        }

        // ================= VNPay: CREATE (đa-store) =================
        [HttpPost("vnpay-create")]
        public async Task<IActionResult> CreateVnpay([FromBody] CreateVnpOrderRequest req)
        {
            var cart = await _cartService.GetCartByUserIdAsync(req.UserId);
            if (cart == null) return BadRequest("Cart not found");

            var shippingGroups = await _cartService.GetShippingGroupsByUserIdAsync(req.UserId, req.AddressId);
            var selected = cart.CartItems.Where(x => x.IsSelected).ToList();
            if (!selected.Any()) return BadRequest("Không có sản phẩm nào được chọn.");

            var groups = selected.GroupBy(x => x.StoreId);
            var subTotal = selected.Sum(x => x.TotalValue);

            decimal voucherReduce = 0;
            if (req.UserVoucherId.HasValue)
            {
                var voucher = cart.Vouchers.FirstOrDefault(v => v.Id == req.UserVoucherId.Value);
                if (voucher != null) voucherReduce = voucher.Reduce;
            }

            var createdOrders = new List<Orders>();
            long totalForAllOrders = 0;

            foreach (var group in groups)
            {
                var storeId = group.Key;
                var storeSubTotal = group.Sum(i => i.TotalValue);
                var storeShipFee = shippingGroups.FirstOrDefault(g => g.StoreId == storeId)?.ShippingFee ?? 0m;

                decimal storeVoucherReduce = 0;
                if (voucherReduce > 0 && subTotal > 0)
                    storeVoucherReduce = voucherReduce * (storeSubTotal / subTotal);

                var storeGrandTotal = (long)Math.Max(0, storeSubTotal + storeShipFee - storeVoucherReduce);
                totalForAllOrders += storeGrandTotal;
                var shipMethod = await _db.ShippingMethods
.FirstOrDefaultAsync(sm => sm.StoreId == storeId && sm.MethodName == "GHTK_AUTO");
                if (shipMethod == null)
                {
                    shipMethod = new ShippingMethods
                    {
                        StoreId = storeId,
                        MethodName = "GHTK_AUTO",
                        Price = 0
                    };
                    _db.ShippingMethods.Add(shipMethod);
                    await _db.SaveChangesAsync();
                }

                var order = new Orders
                {
                    UserId = req.UserId,
                    OrderDate = NowVn(),
                    PaymentMethod = "VNPay",
                    PaymentStatus = "Unpaid",
                    Status = OrderStatus.ChoXuLy,
                    TotalPrice = storeGrandTotal,
                    DeliveryFee = storeShipFee,
                    VoucherId = null,
                    ShippingMethodId = shipMethod.Id
                };
                _db.Orders.Add(order);
                await _db.SaveChangesAsync();

                // Ghi OrderDetails (có ProductVariantId)
                foreach (var item in group)
                {
                    int? pvId = await ResolveProductVariantIdAsync(item.ProductId, item.CartId);
                    _db.OrderDetails.Add(new OrderDetails
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        ProductVariantId = pvId,   // <-- thêm
                        Quantity = item.Quantity,
                        Price = item.Price
                    });
                }
                await _db.SaveChangesAsync();

                createdOrders.Add(order);
            }

            var mainOrder = createdOrders.First();
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var paymentUrl = _vnp.CreatePaymentUrl(new VnpCreatePaymentRequest
            {
                OrderId = mainOrder.Id.ToString(),
                Amount = totalForAllOrders,
                OrderInfo = $"Thanh toan don hang #{mainOrder.Id}",
                IpAddress = clientIp,
                Locale = "vn"
            });

            return Ok(new { paymentUrl });
        }

        // ================= VNPay: RETURN =================
        [HttpGet("vnpay-return")]
        public async Task<IActionResult> VnpReturn()
        {
            var raw = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
            var isValid = _vnp.ValidateReturn(raw);

            var orderId = raw.TryGetValue("vnp_TxnRef", out var refId) ? refId : null;
            var rspCode = raw.TryGetValue("vnp_ResponseCode", out var rc) ? rc : null;
            if (orderId == null) return BadRequest("Missing order id");

            var mainOrder = await _db.Orders.FirstOrDefaultAsync(o => o.Id.ToString() == orderId);
            if (mainOrder == null) return NotFound("Order not found");

            if (isValid && rspCode == "00")
            {
                var unpaidOrders = await _db.Orders
                    .Where(o => o.UserId == mainOrder.UserId && o.PaymentStatus == "Unpaid")
                    .ToListAsync();

                foreach (var order in unpaidOrders)
                {
                    order.PaymentStatus = "Paid";
                    order.Status = OrderStatus.ChoLayHang;
                    order.PaymentDate = NowVn();

                    await DeductStockIfNeededAsync(order.Id);

                    try
                    {
                        if (string.IsNullOrEmpty(order.LabelId))
                        {
                            var label = await _orders.PushOrderToGhtkAndSaveLabelAsync(order.Id);
                            order.LabelId = label;
                        }
                    }
                    catch { }
                }

                await _db.SaveChangesAsync();
                try { await _cartService.ClearSelectedAsync(mainOrder.UserId); } catch { }

                return Redirect($"https://localhost:7180/Checkout/Success?orderId={mainOrder.Id}");
            }
            else
            {
                if (mainOrder.PaymentStatus != "Paid")
                {
                    mainOrder.PaymentStatus = "Failed";
                    mainOrder.Status = OrderStatus.ChoXuLy;
                    await _db.SaveChangesAsync();
                }
                return Redirect($"https://localhost:7180/Checkout/Failure?orderId={mainOrder.Id}");
            }
        }

        // ================= VNPay: IPN =================
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
                    order.PaymentDate = NowVn();            // <-- dùng giờ VN

                    try { await _cartService.ClearSelectedAsync(order.UserId); } catch { }

                    await DeductStockIfNeededAsync(order.Id);
                    await _db.SaveChangesAsync();

                    try
                    {
                        if (string.IsNullOrEmpty(order.LabelId))
                        {
                            var label = await _orders.PushOrderToGhtkAndSaveLabelAsync(order.Id);
                            if (!string.IsNullOrWhiteSpace(label)) order.LabelId = label;
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

        // ================= GHTK TEST =================
        [HttpPost("test-ghtk/{orderId:int}")]
        public async Task<IActionResult> TestGhtk(int orderId)
        {
            var label = await _orders.PushOrderToGhtkAndSaveLabelAsync(orderId);
            return Ok(new { orderId, label });
        }

        // ================= COD: đẩy GHTK cho 1 order sẵn có =================
        [HttpPost("cod/{orderId}")]
        public async Task<IActionResult> CheckoutCOD(int orderId)
        {
            var label = await _orders.PushOrderToGhtkAndSaveLabelCodAsync(orderId);

            if (string.IsNullOrEmpty(label))
                return BadRequest(new { message = "Không tạo được đơn COD bên GHTK." });

            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order != null)
            {
                order.Status = OrderStatus.ChoLayHang;
                order.LabelId = label;
                await _db.SaveChangesAsync();

                await DeductStockIfNeededAsync(order.Id);
            }

            return Ok(new { message = "Đặt hàng COD thành công.", labelId = label });
        }

        // ================= COD: CREATE (đa-store) =================
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

                var selected = cart.CartItems.Where(x => x.IsSelected).ToList();
                if (!selected.Any()) return BadRequest(new { message = "Không có sản phẩm nào được chọn." });

                var subTotal = selected.Sum(x => x.TotalValue);
                decimal voucherReduce = 0;
                if (req.UserVoucherId.HasValue)
                {
                    var voucher = cart.Vouchers.FirstOrDefault(v => v.Id == req.UserVoucherId.Value);
                    if (voucher != null) voucherReduce = voucher.Reduce;
                }

                var groups = selected.GroupBy(x => x.StoreId).ToList();
                var createdOrders = new List<Orders>();

                foreach (var group in groups)
                {
                    var storeId = group.Key;
                    var storeSubTotal = group.Sum(i => i.TotalValue);
                    var storeShippingFee = shippingGroups.FirstOrDefault(g => g.StoreId == storeId)?.ShippingFee ?? 0m;

                    decimal storeVoucherReduce = 0;
                    if (voucherReduce > 0 && subTotal > 0)
                        storeVoucherReduce = voucherReduce * (storeSubTotal / subTotal);

                    var storeGrandTotal = (long)Math.Max(0, storeSubTotal + storeShippingFee - storeVoucherReduce);

                    var shipMethod = await _db.ShippingMethods
                   .FirstOrDefaultAsync(sm => sm.StoreId == storeId && sm.MethodName == "GHTK_AUTO");
                    if (shipMethod == null)
                    {
                        shipMethod = new ShippingMethods { StoreId = storeId, MethodName = "GHTK_AUTO", Price = 0 };
                        _db.ShippingMethods.Add(shipMethod);
                        await _db.SaveChangesAsync();
                    }
                    var order = new Orders
                    {
                        UserId = req.UserId,
                        OrderDate = NowVn(),
                        PaymentMethod = "COD",
                        PaymentStatus = "Unpaid",
                        Status = OrderStatus.ChoXuLy,
                        TotalPrice = storeGrandTotal,
                        DeliveryFee = storeShippingFee,
                        VoucherId = null,
                        ShippingMethodId = shipMethod.Id
                    };
                    _db.Orders.Add(order);
                    await _db.SaveChangesAsync();

                    foreach (var item in group)
                    {
                        int? pvId = await ResolveProductVariantIdAsync(item.ProductId, item.CartId);
                        _db.OrderDetails.Add(new OrderDetails
                        {
                            OrderId = order.Id,
                            ProductId = item.ProductId,
                            ProductVariantId = pvId,   // <-- thêm
                            Quantity = item.Quantity,
                            Price = item.Price
                        });
                    }
                    await _db.SaveChangesAsync();

                    string? label;
                    try
                    {
                        label = await _orders.PushOrderToGhtkAndSaveLabelCodAsync(order.Id);
                    }
                    catch (Exception ex)
                    {
                        await tx.RollbackAsync();
                        return BadRequest(new { message = $"GHTK lỗi: {ex.Message}" });
                    }

                    if (string.IsNullOrWhiteSpace(label))
                    {
                        await tx.RollbackAsync();
                        return BadRequest(new { message = "Không tạo được vận đơn GHTK cho COD." });
                    }

                    order.Status = OrderStatus.ChoXuLy;
                    order.LabelId = label;
                    await _db.SaveChangesAsync();

                    await DeductStockIfNeededAsync(order.Id);

                    createdOrders.Add(order);
                }

                try { await _cartService.ClearSelectedAsync(req.UserId); } catch { }

                await tx.CommitAsync();
                return Ok(new
                {
                    message = "Tạo đơn COD thành công",
                    orders = createdOrders.Select(o => new { o.Id, o.TotalPrice, o.LabelId })
                });
            }
            catch (Exception e)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { message = "Lỗi hệ thống khi tạo đơn COD.", detail = e.Message });
            }
        }

        // ================= MoMo: CREATE (đa-store) =================
        [HttpPost("momo-create")]
        public async Task<IActionResult> CreateMomo([FromBody] CreateVnpOrderRequest req)
        {
            var cart = await _cartService.GetCartByUserIdAsync(req.UserId);
            if (cart == null) return BadRequest("Cart not found");

            var shippingGroups = await _cartService.GetShippingGroupsByUserIdAsync(req.UserId, req.AddressId);
            var selected = cart.CartItems.Where(x => x.IsSelected).ToList();
            if (!selected.Any()) return BadRequest("Không có sản phẩm nào được chọn.");

            var groups = selected.GroupBy(x => x.StoreId).ToList();
            var subTotal = selected.Sum(x => x.TotalValue);

            decimal voucherReduce = 0;
            if (req.UserVoucherId.HasValue)
            {
                var voucher = cart.Vouchers.FirstOrDefault(v => v.Id == req.UserVoucherId.Value);
                if (voucher != null) voucherReduce = voucher.Reduce;
            }

            var createdOrders = new List<Orders>();
            long totalForAllOrders = 0;

            foreach (var group in groups)
            {
                var storeId = group.Key;
                var storeSubTotal = group.Sum(i => i.TotalValue);
                var storeShipFee = shippingGroups.FirstOrDefault(g => g.StoreId == storeId)?.ShippingFee ?? 0m;

                decimal storeVoucherReduce = 0;
                if (voucherReduce > 0 && subTotal > 0)
                    storeVoucherReduce = voucherReduce * (storeSubTotal / subTotal);

                var storeGrandTotal = (long)Math.Max(0, storeSubTotal + storeShipFee - storeVoucherReduce);
                totalForAllOrders += storeGrandTotal;

                var shipMethod = await _db.ShippingMethods
                    .FirstOrDefaultAsync(sm => sm.StoreId == storeId && sm.MethodName == "GHTK_AUTO");
                if (shipMethod == null)
                {
                    shipMethod = new ShippingMethods { StoreId = storeId, MethodName = "GHTK_AUTO", Price = 0 };
                    _db.ShippingMethods.Add(shipMethod);
                    await _db.SaveChangesAsync();
                }

                var order = new Orders
                {
                    UserId = req.UserId,
                    OrderDate = NowVn(),
                    PaymentMethod = "GO",          // <-- đổi tên nhà cung cấp
                    PaymentStatus = "Unpaid",
                    Status = OrderStatus.ChoXuLy,
                    TotalPrice = storeGrandTotal,
                    DeliveryFee = storeShipFee,
                    ShippingMethodId = shipMethod.Id
                };
                _db.Orders.Add(order);
                await _db.SaveChangesAsync();

                foreach (var item in group)
                {
                    int? pvId = await ResolveProductVariantIdAsync(item.ProductId, item.CartId);
                    _db.OrderDetails.Add(new OrderDetails
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        ProductVariantId = pvId,   // <-- thêm
                        Quantity = item.Quantity,
                        Price = item.Price
                    });
                }
                await _db.SaveChangesAsync();

                createdOrders.Add(order);
            }

            var mainOrder = createdOrders.First();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var info = $"Thanh toán MoMo cho đơn #{mainOrder.Id}";
            var (ok, payUrl, message) = await _momo.CreatePaymentAsync(mainOrder.Id.ToString(), totalForAllOrders, info, ip);

            if (!ok || string.IsNullOrWhiteSpace(payUrl))
                return BadRequest(new { message = message ?? "Không tạo được thanh toán MoMo" });

            return Ok(new { payUrl });
        }

        // ================= MoMo: CALLBACK =================
        [HttpGet("momo-callback")]
        public async Task<IActionResult> MomoCallback()
        {
            var dict = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
            var signature = dict.TryGetValue("signature", out var sig) ? sig : "";
            var valid = _momo.ValidateSignature(dict, signature);

            var errorCode = dict.GetValueOrDefault("errorCode");
            var momoOrderId = dict.GetValueOrDefault("orderId");
            if (string.IsNullOrWhiteSpace(momoOrderId))
                return Redirect("https://localhost:7180/Checkout/Failure?message=Missing%20orderId");

            var realIdStr = momoOrderId.Split('_')[0];
            if (!int.TryParse(realIdStr, out var realOrderId))
                return Redirect("https://localhost:7180/Checkout/Failure?message=Invalid%20orderId");

            var mainOrder = await _db.Orders.FirstOrDefaultAsync(o => o.Id == realOrderId);
            if (mainOrder == null)
                return Redirect("https://localhost:7180/Checkout/Failure?message=Order%20not%20found");

            if (valid && errorCode == "0")
            {
                var unpaidOrders = await _db.Orders
                    .Where(o => o.UserId == mainOrder.UserId && o.PaymentStatus == "Unpaid")
                    .ToListAsync();

                foreach (var order in unpaidOrders)
                {
                    order.PaymentStatus = "Paid";
                    order.Status = OrderStatus.ChoLayHang;
                    order.PaymentDate = NowVn();

                    await DeductStockIfNeededAsync(order.Id);

                    try
                    {
                        if (string.IsNullOrEmpty(order.LabelId))
                        {
                            var label = await _orders.PushOrderToGhtkAndSaveLabelAsync(order.Id);
                            order.LabelId = label;
                        }
                    }
                    catch { }
                }

                await _db.SaveChangesAsync();
                try { await _cartService.ClearSelectedAsync(mainOrder.UserId); } catch { }

                return Redirect($"https://localhost:7180/Checkout/Success?orderId={mainOrder.Id}");
            }
            else
            {
                if (mainOrder.PaymentStatus != "Paid")
                {
                    mainOrder.PaymentStatus = "Failed";
                    await _db.SaveChangesAsync();
                }
                return Redirect($"https://localhost:7180/Checkout/Failure?orderId={mainOrder.Id}&message=MoMo%20Failed");
            }
        }

        // ================= MoMo: IPN =================
        [HttpPost("momo-ipn")]
        public async Task<IActionResult> MomoIpn()
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in Request.Query) data[kv.Key] = kv.Value.ToString();
            if (Request.HasFormContentType)
                foreach (var kv in Request.Form) data[kv.Key] = kv.Value.ToString();

            var signature = data.GetValueOrDefault("signature") ?? "";
            var valid = _momo.ValidateSignature(data, signature);

            var errorCode = data.GetValueOrDefault("errorCode");
            var momoOrderId = data.GetValueOrDefault("orderId") ?? "";
            var realStr = momoOrderId.Split('_').FirstOrDefault();
            if (!int.TryParse(realStr, out var orderId)) return Ok(new { Result = 0 });

            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) return Ok(new { Result = 0 });

            if (valid && errorCode == "0" && order.PaymentStatus != "Paid")
            {
                order.PaymentStatus = "Paid";
                order.Status = OrderStatus.ChoLayHang;
                order.PaymentDate = NowVn();

                try { await _cartService.ClearSelectedAsync(order.UserId); } catch { }

                await _db.SaveChangesAsync();

                await DeductStockIfNeededAsync(order.Id);
            }

            return Ok(new { Result = 1 });
        }

        // ======= Resolve ProductVariantId từ CartItemVariants (giữ nguyên) =======
        private async Task<int?> ResolveProductVariantIdAsync(int productId, int cartId)
        {
            var chosen = await _db.CartItemVariants
                .Where(x => x.CartId == cartId)
                .Select(x => x.VariantValueId)
                .OrderBy(x => x)
                .ToListAsync();

            if (chosen.Count == 0) return null;

            var candidates = await _db.VariantCompositions
                .Where(vc => vc.ProductId == productId && vc.ProductVariantId != null && vc.VariantValueId != null)
                .GroupBy(vc => vc.ProductVariantId!.Value)
                .Select(g => new
                {
                    PvId = g.Key,
                    Values = g.Select(x => x.VariantValueId!.Value).OrderBy(v => v).ToList()
                })
                .ToListAsync();

            var hit = candidates.FirstOrDefault(c => c.Values.SequenceEqual(chosen));
            return hit?.PvId;
        }
        [HttpPost("balance-create")]
        public async Task<IActionResult> CreateBalanceOrder([FromBody] CreateVnpOrderRequest req)
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

                var selected = cart.CartItems.Where(x => x.IsSelected).ToList();
                if (!selected.Any()) return BadRequest(new { message = "Không có sản phẩm nào được chọn." });

                var subTotal = selected.Sum(x => x.TotalValue);
                decimal voucherReduce = 0;
                if (req.UserVoucherId.HasValue)
                {
                    var voucher = cart.Vouchers.FirstOrDefault(v => v.Id == req.UserVoucherId.Value);
                    if (voucher != null) voucherReduce = voucher.Reduce;
                }

                // Group theo store + phân bổ voucher theo tỷ lệ subtotal mỗi store
                var groups = selected.GroupBy(x => x.StoreId).ToList();

                // Tính tổng phải charge từ ví dựa trên tổng các storeGrandTotal để tránh lệch
                var createdOrders = new List<Orders>();
                var storeTotals = new List<decimal>();

                foreach (var group in groups)
                {
                    var storeSubTotal = group.Sum(i => i.TotalValue);
                    var storeShippingFee = shippingGroups.FirstOrDefault(g => g.StoreId == group.Key)?.ShippingFee ?? 0m;

                    decimal storeVoucherReduce = 0;
                    if (voucherReduce > 0 && subTotal > 0)
                        storeVoucherReduce = voucherReduce * (storeSubTotal / subTotal);

                    var storeGrandTotal = Math.Max(0m, storeSubTotal + storeShippingFee - storeVoucherReduce);
                    storeTotals.Add(storeGrandTotal);
                }

                var totalAmount = storeTotals.Sum();

                // check & trừ ví
                var user = await _db.Users.FindAsync(req.UserId);
                if (user == null) return BadRequest(new { message = "User not found" });
                if (user.Balance < totalAmount)
                    return BadRequest(new { message = "Số dư không đủ để thanh toán." });

                user.Balance -= totalAmount;
                await _db.SaveChangesAsync();

                // Tạo đơn theo store và PUSH GHTK để lấy label
                int idx = 0;
                foreach (var group in groups)
                {
                    var storeId = group.Key;
                    var storeSubTotal = group.Sum(i => i.TotalValue);
                    var storeShippingFee = shippingGroups.FirstOrDefault(g => g.StoreId == storeId)?.ShippingFee ?? 0m;

                    decimal storeVoucherReduce = 0;
                    if (voucherReduce > 0 && subTotal > 0)
                        storeVoucherReduce = voucherReduce * (storeSubTotal / subTotal);

                    var storeGrandTotal = (long)Math.Round(Math.Max(0m, storeSubTotal + storeShippingFee - storeVoucherReduce));

                    var shipMethod = await _db.ShippingMethods
                       .FirstOrDefaultAsync(sm => sm.StoreId == storeId && sm.MethodName == "GHTK_AUTO");
                    if (shipMethod == null)
                    {
                        shipMethod = new ShippingMethods { StoreId = storeId, MethodName = "GHTK_AUTO", Price = 0 };
                        _db.ShippingMethods.Add(shipMethod);
                        await _db.SaveChangesAsync();
                    }

                    var order = new Orders
                    {
                        UserId = req.UserId,
                        OrderDate = NowVn(),
                        PaymentMethod = "BALANCE",
                        PaymentStatus = "Paid",               // ✅ ví = Paid ngay
                        Status = OrderStatus.ChoXuLy,
                        TotalPrice = storeGrandTotal,
                        DeliveryFee = storeShippingFee,
                        VoucherId = null,
                        ShippingMethodId = shipMethod.Id
                    };
                    _db.Orders.Add(order);
                    await _db.SaveChangesAsync();

                    foreach (var item in group)
                    {
                        int? pvId = await ResolveProductVariantIdAsync(item.ProductId, item.CartId);
                        _db.OrderDetails.Add(new OrderDetails
                        {
                            OrderId = order.Id,
                            ProductId = item.ProductId,
                            ProductVariantId = pvId,
                            Quantity = item.Quantity,
                            Price = item.Price
                        });
                    }
                    await _db.SaveChangesAsync();

                    // 🔥 Push GHTK để lấy mã vận đơn (label)
                    string? label;
                    try
                    {
                        label = await _orders.PushOrderToGhtkAndSaveLabelCodAsync(order.Id);
                    }
                    catch (Exception ex)
                    {
                        // Rollback cả transaction -> hoàn tiền ví
                        await tx.RollbackAsync();
                        return BadRequest(new { message = $"GHTK lỗi: {ex.Message}" });
                    }

                    if (string.IsNullOrWhiteSpace(label))
                    {
                        await tx.RollbackAsync();
                        return BadRequest(new { message = "Không tạo được vận đơn GHTK cho Balance." });
                    }

                    order.Status = OrderStatus.ChoLayHang; // giống COD sau khi có label
                    order.LabelId = label;
                    await _db.SaveChangesAsync();

                    await DeductStockIfNeededAsync(order.Id);

                    createdOrders.Add(order);
                    idx++;
                }

                try { await _cartService.ClearSelectedAsync(req.UserId); } catch { }

                await tx.CommitAsync();
                return Ok(new
                {
                    message = "Thanh toán bằng Balance thành công",
                    orders = createdOrders.Select(o => new { o.Id, o.TotalPrice, o.LabelId })
                });
            }
            catch (Exception e)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { message = "Lỗi hệ thống khi tạo đơn Balance.", detail = e.Message });
            }
        }

    }
}