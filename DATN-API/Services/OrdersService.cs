using DATN_API.Data;
using DATN_API.Models;
using DATN_API.Services.Interfaces;
using DATN_API.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services
{
    public class OrdersService : IOrdersService
    {
        private readonly ApplicationDbContext _context;
        public OrdersService(ApplicationDbContext context)
        {
            _context = context;
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
            order.Status = model.Status;
            order.UserId = model.UserId;
            // ... add other properties as needed
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
            if (order == null) return (false, "Không t?m th?y ðõn hàng");

            order.Status = status;

            await _context.SaveChangesAsync();
            return (true, "C?p nh?t thành công");
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

            return new OrderViewModel
            {
                Id = o.Id,
                CustomerName = o.User?.FullName ?? string.Empty,
                CustomerPhone = o.User?.Phone,
                StoreName = o.ShippingMethod?.store?.Name,
                ShippingMethodName = o.ShippingMethod?.MethodName ?? string.Empty,
                ShippingFee = o.ShippingMethod?.Price ?? 0,
                VoucherName = o.Voucher?.Type.ToString(),
                VoucherReduce = o.Voucher?.Reduce,
                CreatedAt = o.OrderDate,
                TotalPrice = o.TotalPrice,
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
        // thành:
        public async Task<List<OrderViewModel>> GetOrdersByStoreUserAsync(int userId)
        {
            // L?y store c?a seller theo UserId
            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.UserId == userId);
            if (store == null) return new List<OrderViewModel>();

            // L?y orders có ShippingMethod.StoreId týõng ?ng
            var orders = await _context.Orders
                .Where(o => o.ShippingMethod != null && o.ShippingMethod.StoreId == store.Id)
                .Include(o => o.User)
                .Include(o => o.Voucher)
                .Include(o => o.ShippingMethod)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .ToListAsync();

            // Map sang ViewModel
            return orders.Select(o => new OrderViewModel
            {
                Id = o.Id,
                CustomerName = o.User?.FullName ?? string.Empty,
                CustomerPhone = o.User?.Phone,
                StoreName = o.ShippingMethod?.store?.Name,
                ShippingMethodName = o.ShippingMethod?.MethodName ?? string.Empty,
                ShippingFee = o.ShippingMethod?.Price ?? 0,
                VoucherName = o.Voucher?.Type.ToString(),
                VoucherReduce = o.Voucher?.Reduce,
                CreatedAt = o.OrderDate,
                TotalPrice = o.TotalPrice,
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
        public async Task<object> GetStatisticsAsync(int storeId, DateTime? start, DateTime? end, DateTime? startCompare, DateTime? endCompare)
        {
            // L?y th?ng kê k? hi?n t?i
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

            // L?y th?ng kê k? so sánh
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

            // Hàm tính % thay ð?i an toàn
            double CalculatePercentChange(int current, int previous)
            {
                if (previous == 0)
                    return current == 0 ? 0 : 100; // N?u k? trý?c 0 và k? này có s? li?u => 100%
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
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .AsNoTracking()
                .OrderByDescending(o => o.OrderDate) // Order mới nhất lên trên
                .ToListAsync();

            var result = orders.Select(o => MapToOrderViewModel(o)).ToList();

            return result;
        }
        private OrderViewModel MapToOrderViewModel(Orders entity)
        {
            return new OrderViewModel
            {
                Id = entity.Id,
                CreatedAt = entity.OrderDate,
                CustomerName = entity.User?.FullName ?? "Khách hàng",
                CustomerPhone = entity.User?.Phone,
                StoreName = entity.ShippingMethod?.MethodName ?? "Không rõ",
                VoucherName = GetVoucherDisplayName(entity.Voucher),
                VoucherReduce = entity.Voucher?.Reduce,
                ShippingMethodName = entity.ShippingMethod?.MethodName ?? "",
                ShippingFee = entity.DeliveryFee,
                TotalPrice = entity.TotalPrice,
                PaymentMethod = entity.PaymentMethod ?? "",
                PaymentStatus = entity.PaymentStatus,
                Status = entity.Status.ToString(), // Hoặc map sang string hiển thị thân thiện
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

    }
}