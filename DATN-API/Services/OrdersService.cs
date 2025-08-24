using DATN_API.Data;
using DATN_API.Models;
using DATN_API.Services.Interfaces;
using DATN_API.ViewModels;
using DATN_API.Interfaces;                 // <-- thêm
using DATN_API.ViewModels.GHTK;           // <-- thêm
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace DATN_API.Services
{
    public class OrdersService : IOrdersService
    {
        private readonly ApplicationDbContext _context;
        private readonly IGHTKService _ghtk;   // <-- thêm
        private readonly IEmailService _emailService;

        public OrdersService(ApplicationDbContext context, IGHTKService ghtk, IEmailService emailService) // <-- inject GHTK
        {
            _context = context;
            _ghtk = ghtk;
            _emailService = emailService;   
        }

        public async Task<IEnumerable<Orders>> GetAllAsync()
        {
            return await _context.Orders.ToListAsync();
        }

        public async Task<Orders> GetByIdAsync(int id)
        {
            return await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<Orders> CreateAsync(Orders model)
        {
            _context.Orders.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<bool> UpdateAsync(int id, Orders model)
        {
            if (id != model.Id) return false;
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return false;

            // cập nhật các field cần thiết
            order.Status = model.Status;
            order.UserId = model.UserId;
            order.PaymentMethod = model.PaymentMethod;
            order.PaymentStatus = model.PaymentStatus;
            order.TotalPrice = model.TotalPrice;
            order.VoucherId = model.VoucherId;
            order.ShippingMethodId = model.ShippingMethodId;
            order.DeliveryFee = model.DeliveryFee;
            order.PaymentDate = model.PaymentDate;
            order.OrderDate = model.OrderDate; 

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return false;
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool, string)> UpdateStatusAsync(int orderId, OrderStatus status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return (false, "Không tìm thấy đơn hàng");

            order.Status = status;
            await _context.SaveChangesAsync();
            return (true, "Cập nhật thành công");
        }

        public async Task<OrderViewModel?> GetOrderDetailAsync(int orderId)
        {
            var o = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Voucher)
                .Include(o => o.ShippingMethod)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);
            if (o == null) return null;

            var totalPrice = o.OrderDetails.Sum(od => od.Quantity * od.Price);

            decimal voucherReduce = 0;
            if (o.Voucher != null && o.Voucher.Status == VoucherStatus.Valid && totalPrice >= o.Voucher.MinOrder)
            {
                voucherReduce = o.Voucher.Reduce;
            }

            return new OrderViewModel
            {
                Id = o.Id,
                CustomerName = o.User?.FullName ?? string.Empty,
                CustomerPhone = o.User?.Phone,
                StoreName = o.ShippingMethod?.store?.Name,
                ShippingMethodName = o.ShippingMethod?.MethodName ?? string.Empty,

                // ✅ LẤY TỪ BẢNG ORDERS
                ShippingFee = o.DeliveryFee,
                TotalPrice = o.TotalPrice,
                LabelId = o.LabelId,

                VoucherName = o.Voucher?.Type.ToString(),
                VoucherReduce = voucherReduce,
                CreatedAt = o.OrderDate,
                PaymentMethod = o.PaymentMethod,
                PaymentStatus = o.PaymentStatus,
                Status = o.Status.ToString(),
                OrderDetails = o.OrderDetails.Select(od => new OrderDetailViewModel
                {
                    ProductId = od.ProductId,
                    ProductName = od.Product?.Name ?? string.Empty,
                    ProductImage = od.Product?.MainImage,
                    Quantity = od.Quantity,
                    UnitPrice = od.Price
                }).ToList()
            };
        }


        public async Task<List<OrderViewModel>> GetOrdersByStoreUserAsync(int userId)
        {
            var store = await _context.Stores.FirstOrDefaultAsync(s => s.UserId == userId);
            if (store == null) return new List<OrderViewModel>();

            var orders = await _context.Orders
                .Where(o => o.ShippingMethod != null && o.ShippingMethod.StoreId == store.Id)
                .Include(o => o.User)
                .Include(o => o.Voucher)
                .Include(o => o.ShippingMethod)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .ToListAsync();

            return orders.Select(o => new OrderViewModel
            {
                Id = o.Id,
                CustomerName = o.User?.FullName ?? string.Empty,
                CustomerPhone = o.User?.Phone,
                StoreName = o.ShippingMethod?.store?.Name,
                ShippingMethodName = o.ShippingMethod?.MethodName ?? string.Empty,

                // ✅ TỪ ORDERS
                ShippingFee = o.DeliveryFee,
                TotalPrice = o.TotalPrice,
                LabelId = o.LabelId,

                VoucherName = o.Voucher?.Type.ToString(),
                VoucherReduce = o.Voucher?.Reduce,
                CreatedAt = o.OrderDate,
                PaymentMethod = o.PaymentMethod,
                PaymentStatus = o.PaymentStatus,
                Status = o.Status.ToString(),
                OrderDetails = o.OrderDetails.Select(od => new OrderDetailViewModel
                {
                    ProductId = od.ProductId,
                    ProductName = od.Product?.Name ?? string.Empty,
                    ProductImage = od.Product?.MainImage,
                    Quantity = od.Quantity,
                    UnitPrice = od.Price
                }).ToList()
            }).ToList();

        }
        public async Task<object> GetStatisticsByUserAsync(int userId, DateTime? start, DateTime? end, DateTime? startCompare, DateTime? endCompare)
        {
            // ✅ Tìm store theo userId
            var store = await _context.Stores.FirstOrDefaultAsync(s => s.UserId == userId);
            if (store == null)
            {
                return new
                {
                    totalOrders = 0,
                    pendingOrders = 0,
                    shippingOrders = 0,
                    completedOrders = 0,
                    totalOrdersPercentChange = 0,
                    pendingOrdersPercentChange = 0,
                    shippingOrdersPercentChange = 0,
                    completedOrdersPercentChange = 0
                };
            }

            // ✅ Query current range
            var queryCurrent = _context.Orders
                .Where(o => o.ShippingMethod != null && o.ShippingMethod.StoreId == store.Id);

            if (start.HasValue)
                queryCurrent = queryCurrent.Where(o => o.OrderDate >= start.Value);

            if (end.HasValue)
                queryCurrent = queryCurrent.Where(o => o.OrderDate <= end.Value);

            var currentStats = await queryCurrent
                .GroupBy(o => 1)
                .Select(g => new
                {
                    TotalOrders = g.Count(),
                    PendingOrders = g.Count(o => o.Status == OrderStatus.ChoXuLy),
                    ShippingOrders = g.Count(o => o.Status == OrderStatus.DangGiao),
                    CompletedOrders = g.Count(o => o.Status == OrderStatus.DaHoanThanh)
                })
                .FirstOrDefaultAsync() ?? new { TotalOrders = 0, PendingOrders = 0, ShippingOrders = 0, CompletedOrders = 0 };

            // ✅ Query compare range
            var queryCompare = _context.Orders
                .Where(o => o.ShippingMethod != null && o.ShippingMethod.StoreId == store.Id);

            if (startCompare.HasValue)
                queryCompare = queryCompare.Where(o => o.OrderDate >= startCompare.Value);

            if (endCompare.HasValue)
                queryCompare = queryCompare.Where(o => o.OrderDate <= endCompare.Value);

            var compareStats = await queryCompare
                .GroupBy(o => 1)
                .Select(g => new
                {
                    TotalOrders = g.Count(),
                    PendingOrders = g.Count(o => o.Status == OrderStatus.ChoXuLy),
                    ShippingOrders = g.Count(o => o.Status == OrderStatus.DangGiao),
                    CompletedOrders = g.Count(o => o.Status == OrderStatus.DaHoanThanh)
                })
                .FirstOrDefaultAsync() ?? new { TotalOrders = 0, PendingOrders = 0, ShippingOrders = 0, CompletedOrders = 0 };

            // ✅ Tính % thay đổi
            double CalculatePercentChange(int current, int previous)
            {
                if (previous == 0) return current == 0 ? 0 : 100;
                return ((double)(current - previous) / previous) * 100;
            }

            return new
            {
                totalOrders = currentStats.TotalOrders,
                pendingOrders = currentStats.PendingOrders,
                shippingOrders = currentStats.ShippingOrders,
                completedOrders = currentStats.CompletedOrders,

                totalOrdersPercentChange = CalculatePercentChange(currentStats.TotalOrders, compareStats.TotalOrders),
                pendingOrdersPercentChange = CalculatePercentChange(currentStats.PendingOrders, compareStats.PendingOrders),
                shippingOrdersPercentChange = CalculatePercentChange(currentStats.ShippingOrders, compareStats.ShippingOrders),
                completedOrdersPercentChange = CalculatePercentChange(currentStats.CompletedOrders, compareStats.CompletedOrders),
            };
        }

        public async Task<object> GetStatisticsAsync(int storeId, DateTime? start, DateTime? end, DateTime? startCompare, DateTime? endCompare)
        {
            var queryCurrent = _context.Orders
                .Where(o => o.ShippingMethod != null && o.ShippingMethod.StoreId == storeId);

            if (start.HasValue)
                queryCurrent = queryCurrent.Where(o => o.OrderDate >= start.Value);

            if (end.HasValue)
                queryCurrent = queryCurrent.Where(o => o.OrderDate <= end.Value);

            var currentStats = await queryCurrent
                .GroupBy(o => 1)
                .Select(g => new
                {
                    TotalOrders = g.Count(),
                    PendingOrders = g.Count(o => o.Status == OrderStatus.ChoXuLy),
                    ShippingOrders = g.Count(o => o.Status == OrderStatus.DangGiao),
                    CompletedOrders = g.Count(o => o.Status == OrderStatus.DaHoanThanh)
                })
                .FirstOrDefaultAsync() ?? new { TotalOrders = 0, PendingOrders = 0, ShippingOrders = 0, CompletedOrders = 0 };

            var queryCompare = _context.Orders
                .Where(o => o.ShippingMethod != null && o.ShippingMethod.StoreId == storeId);

            if (startCompare.HasValue)
                queryCompare = queryCompare.Where(o => o.OrderDate >= startCompare.Value);

            if (endCompare.HasValue)
                queryCompare = queryCompare.Where(o => o.OrderDate <= endCompare.Value);

            var compareStats = await queryCompare
                .GroupBy(o => 1)
                .Select(g => new
                {
                    TotalOrders = g.Count(),
                    PendingOrders = g.Count(o => o.Status == OrderStatus.ChoXuLy),
                    ShippingOrders = g.Count(o => o.Status == OrderStatus.DangGiao),
                    CompletedOrders = g.Count(o => o.Status == OrderStatus.DaHoanThanh)
                })
                .FirstOrDefaultAsync() ?? new { TotalOrders = 0, PendingOrders = 0, ShippingOrders = 0, CompletedOrders = 0 };

            double CalculatePercentChange(int current, int previous)
            {
                if (previous == 0) return current == 0 ? 0 : 100;
                return ((double)(current - previous) / previous) * 100;
            }

            return new
            {
                totalOrders = currentStats.TotalOrders,
                pendingOrders = currentStats.PendingOrders,
                shippingOrders = currentStats.ShippingOrders,
                completedOrders = currentStats.CompletedOrders,

                totalOrdersPercentChange = CalculatePercentChange(currentStats.TotalOrders, compareStats.TotalOrders),
                pendingOrdersPercentChange = CalculatePercentChange(currentStats.PendingOrders, compareStats.PendingOrders),
                shippingOrdersPercentChange = CalculatePercentChange(currentStats.ShippingOrders, compareStats.ShippingOrders),
                completedOrdersPercentChange = CalculatePercentChange(currentStats.CompletedOrders, compareStats.CompletedOrders),
            };
        }

        public async Task<List<OrderViewModel>> GetOrdersByUserIdAsync(int userId)
        {
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.User)
                .Include(o => o.Voucher)
                .Include(o => o.ShippingMethod)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .AsNoTracking()
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return orders.Select(o => MapToOrderViewModel(o)).ToList();
        }

        private OrderViewModel MapToOrderViewModel(Orders entity)
        {
            var totalPriceCalc = entity.OrderDetails?.Sum(od => od.Quantity * od.Price) ?? 0;

            decimal voucherReduce = 0;
            if (entity.Voucher != null && entity.Voucher.Status == VoucherStatus.Valid && totalPriceCalc >= entity.Voucher.MinOrder)
                voucherReduce = entity.Voucher.Reduce;

            return new OrderViewModel
            {
                Id = entity.Id,
                CreatedAt = entity.OrderDate,
                CustomerName = entity.User?.FullName ?? "Khách hàng",
                CustomerPhone = entity.User?.Phone,
                StoreName = entity.ShippingMethod?.store?.Name ?? "Không rõ",
                VoucherName = GetVoucherDisplayName(entity.Voucher),
                VoucherReduce = voucherReduce,
                ShippingMethodName = entity.ShippingMethod?.MethodName ?? "",

                // ✅ DỮ LIỆU TỪ ORDERS
                ShippingFee = entity.DeliveryFee,
                TotalPrice = entity.TotalPrice,
                LabelId = entity.LabelId,

                PaymentMethod = entity.PaymentMethod ?? "",
                PaymentStatus = entity.PaymentStatus,
                Status = entity.Status.ToString(),
                OrderDetails = entity.OrderDetails?.Select(od => new OrderDetailViewModel
                {
                    ProductId = od.ProductId,
                    ProductName = od.Product?.Name ?? "Sản phẩm",
                    ProductImage = od.Product?.MainImage,
                    Quantity = od.Quantity,
                    UnitPrice = od.Price
                }).ToList() ?? new List<OrderDetailViewModel>()
            };
        }


        public async Task<OrderViewModel?> GetOrderDetailByIdAsync(int orderId, int userId)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Voucher)
                .Include(o => o.ShippingMethod).ThenInclude(sm => sm.store)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null) return null;

            var totalPrice = order.OrderDetails.Sum(od => od.Quantity * od.Price);

            decimal voucherReduce = 0;
            if (order.Voucher != null && order.Voucher.Status == VoucherStatus.Valid && totalPrice >= order.Voucher.MinOrder)
                voucherReduce = order.Voucher.Reduce;

            return new OrderViewModel
            {
                Id = order.Id,
                CustomerName = order.User?.FullName ?? string.Empty,
                CustomerPhone = order.User?.Phone,
                StoreName = order.ShippingMethod?.store?.Name,
                ShippingMethodName = order.ShippingMethod?.MethodName ?? string.Empty,

                // ✅ LẤY PHÍ SHIP TỪ ORDERS.DeliveryFee
                ShippingFee = order.DeliveryFee,

                // ✅ TRẢ MÃ VẬN ĐƠN
                LabelId = order.LabelId,

                VoucherName = GetVoucherDisplayName(order.Voucher),
                VoucherReduce = voucherReduce,
                CreatedAt = order.OrderDate,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,
                Status = order.Status.ToString(),
                OrderDetails = order.OrderDetails.Select(od => new OrderDetailViewModel
                {
                    ProductId = od.ProductId,
                    ProductName = od.Product?.Name ?? string.Empty,
                    ProductImage = od.Product?.MainImage,
                    Quantity = od.Quantity,
                    UnitPrice = od.Price
                }).ToList(),

                // bạn có thể giữ TotalPrice = order.TotalPrice (đã gồm ship) – khuyến nghị
                TotalPrice = order.TotalPrice
            };
        }

        private string? GetVoucherDisplayName(Vouchers? voucher)
        {
            if (voucher == null) return null;
            return voucher.Type switch
            {
                VoucherType.Platform => "Mã của sàn",
                VoucherType.Shop => "Mã của shop",
                _ => null
            };
        }

        // ===== NEW: Đẩy đơn lên GHTK & lưu LabelId =====
        public async Task<string?> PushOrderToGhtkAndSaveLabelAsync(int orderId)
        {
            var o = await _context.Orders
                .Include(x => x.User)
                .Include(x => x.OrderDetails).ThenInclude(od => od.Product)
                .Include(x => x.ShippingMethod)
                .FirstOrDefaultAsync(x => x.Id == orderId);

            if (o == null) return null;

            // 1) Địa chỉ nhận: lấy bản ghi mới nhất của user (tối thiểu cho chạy)
            var addr = await _context.Addresses
                .Include(a => a.City)
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync(a => a.UserId == o.UserId);
            if (addr == null) return null;

            // Tối thiểu: suy ra district/ward đơn giản (giống CalculateGHTKFee bạn đã làm)
            // addr đã Include(a => a.City)
            int cityId = (addr.City != null) ? addr.City.Id : 0;

            var district = await _context.Districts
                .FirstOrDefaultAsync(d => d.CityId == cityId);

            int districtId = (district != null) ? district.Id : 0;

            var ward = await _context.Wards
                .FirstOrDefaultAsync(w => w.DistrictId == districtId);

            // Chuỗi tên hiển thị (ngoài expression tree nên dùng ?. thoải mái)
            var receiverProvince = addr.City?.CityName ?? "";
            var receiverDistrict = district != null ? district.DistrictName : "";
            var receiverWard = ward != null ? ward.WardName : "";
            // tên phường/xã

            // 2) Điểm lấy hàng: lấy từ Store của ShippingMethod
            Stores? store = null;
            if (o.ShippingMethodId != 0)
            {
                store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == o.ShippingMethod!.StoreId);
            }
            if (store == null)
            {
                // fallback: để có gì đó gửi – KHÔNG tối ưu (nên cấu hình Store chuẩn)
                store = new Stores
                {
                    Name = "Shop GO",
                    RepresentativeName = "Shop GO",
                    Phone = "0900000000",
                    Address = "Kho tổng",
                    PickupAddress = "Kho tổng",
                    Province = "TP. Hồ Chí Minh",
                    District = "Quận 1",
                    Ward = "Phường Bến Nghé"
                };
            }

            // 3) Build products (kg tối thiểu 0.1)
            var products = o.OrderDetails.Select(od => new DATN_API.ViewModels.GHTK.GHTKProduct
            {
                Name = od.Product?.Name ?? "Sản phẩm",
                Weight = Math.Max(0.1m, ((decimal)(od.Product?.Weight ?? 0)) / 1000m),
                Quantity = od.Quantity
            }).ToList();

            // 4) Payload GHTK
            var payload = new DATN_API.ViewModels.GHTK.GHTKCreateOrderRequest
            {
                Products = products,
                Order = new DATN_API.ViewModels.GHTK.GHTKOrder
                {
                    /*Id = $"ORD-{o.Id}",*/
                    Id = $"ORD-{o.Id}-{DateTime.UtcNow.Ticks}",
                    // pick_ (điểm lấy)
                    PickName = store.RepresentativeName ?? store.Name ?? "Shop",
                    PickAddress = store.PickupAddress ?? store.Address ?? "",
                    PickProvince = store.Province ?? "",
                    PickDistrict = store.District ?? "",
                    PickWard = store.Ward ?? "",
                    PickTel = store.Phone ?? "0000000000",

                    // receiver (người nhận)
                    Name = o.User?.FullName ?? "Khách hàng",
                    Address = addr.Description ?? "Địa chỉ nhận",
                    Province = receiverProvince,
                    District = receiverDistrict,
                    Ward = receiverWard,
                    Tel = o.User?.Phone ?? "0000000000",

                    Hamlet = "Khác",
                    DeliverOption = "none",
                    Transport = "road",

                    // VNPay đã thanh toán => KHÔNG thu hộ
                    PickMoney = 0,
                    Value = o.TotalPrice,
                    Note = ""
                }
            };

            // 5) Gọi GHTK
            var label = await _ghtk.CreateOrderAsync(payload);
            if (string.IsNullOrWhiteSpace(label))
            {
                // Nếu tạo thất bại, trả null để controller log lỗi
                return null;
            }

            // 6) Lưu label vào đơn
            o.LabelId = label;
            await _context.SaveChangesAsync();
            return label;
        }
        public async Task<Dictionary<string, decimal>> GetTotalPriceByMonthAsync(int year, int storeId)
        {
            // Lấy các order theo năm & storeId
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Where(o => o.OrderDate.Year == year &&
                            o.OrderDetails.Any(od => od.Product.StoreId == storeId))
                .ToListAsync();

            // Tạo dictionary mặc định 12 tháng = 0
            var result = Enumerable.Range(1, 12)
                .ToDictionary(m => m.ToString(), m => 0m);

            foreach (var order in orders)
            {
                int month = order.OrderDate.Month;
                result[month.ToString()] += order.TotalPrice;
            }

            return result;
        }
        public async Task<int> GetTotalOrdersByStoreIdAsync(int storeId)
        {
            return await _context.Orders
                .Where(o => o.ShippingMethod != null && o.ShippingMethod.StoreId == storeId)
                .CountAsync();
        }
        public async Task SendRevenueReportAllStoresCurrentMonthAsync()
        {
            int year = DateTime.Now.Year;
            int month = DateTime.Now.Month;

            var stores = await _context.Stores
                .Include(s => s.User) // join lấy user
                .ToListAsync();

            if (stores == null || stores.Count == 0)
                throw new Exception("Không có store nào trong hệ thống");

            var tasks = stores
                .Where(s => s.User != null && !string.IsNullOrEmpty(s.User.Email))
                .Select(async store =>
                {
                    var revenueData = await GetTotalPriceByMonthAsync(year, store.Id);
                    decimal revenue = revenueData.ContainsKey(month.ToString())
                        ? revenueData[month.ToString()]
                        : 0;
                    var totalRevenue = revenue; // doanh thu gốc
                    var platformFee = totalRevenue * 0.05m; // phí 5%
                    var netRevenue = totalRevenue - platformFee; // cửa hàng nhận
                    string subject = $"Báo cáo doanh thu tháng {month}/{year}";
                    string body = $@"
                            <h2 style='font-size:22px; color:#333;'>📊 Báo cáo doanh thu tháng {month}/{year}</h2>
                            <p style='font-size:18px;'>Kính gửi cửa hàng: <b>{store.Name}</b></p>
                            <p style='font-size:18px;'>Tổng doanh thu tháng {month}/{year} của quý cửa hàng: 
                                <b style='color:blue;'>{totalRevenue:N0} VNĐ</b></p>
                            <p style='font-size:18px;'>Phí kinh doanh (5%): 
                                <b style='color:red;'>{platformFee:N0} VNĐ</b></p>
                            <p style='font-size:20px;'>💰 Doanh thu thực nhận: 
                                <b style='color:green;'>{netRevenue:N0} VNĐ</b></p>
                            <p style='font-size:16px; color:#555;'>Doanh thu của quý cửa hàng sẽ được gửi trước ngày 10/{month}/{year}</p>
                                <hr/>
                            <p style='font-size:14px; color:#888;'>Hệ thống quản lý bán hàng GoTeam</p>
                    ";

                    await _emailService.SendEmailAsync(store.User.Email, subject, body);
                });

            await Task.WhenAll(tasks);
        }
    }
}