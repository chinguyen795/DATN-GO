using DATN_API.Data;
using DATN_API.Interfaces;
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
        private readonly IGHTKService _ghtkService;
        public OrdersService(ApplicationDbContext context, IGHTKService ghtkService)
        {
            _context = context;
            _ghtkService = ghtkService;
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
                .Include(o => o.ShippingMethod).ThenInclude(sm => sm.store)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (o == null) return null;
            decimal unitPrice = 0;

            var totalPrice = o.OrderDetails.Sum(od => od.Quantity * od.Price);

            decimal voucherReduce = 0;
            if (o.Voucher != null && o.Voucher.Status == VoucherStatus.Valid && totalPrice >= o.Voucher.MinOrder)
            {
                voucherReduce = o.Voucher.Reduce;
            }

            var viewModel = new OrderViewModel
            {
                Id = o.Id,
                CustomerName = o.User?.FullName ?? string.Empty,
                CustomerPhone = o.User?.Phone,
                StoreName = o.ShippingMethod?.store?.Name,
                ShippingMethodName = o.ShippingMethod?.MethodName ?? string.Empty,
                ShippingFee = o.ShippingMethod?.Price ?? 0,
                VoucherName = o.Voucher?.Type.ToString(),
                VoucherReduce = voucherReduce,
                CreatedAt = o.OrderDate,
                PaymentMethod = o.PaymentMethod,
                PaymentStatus = o.PaymentStatus,
                Status = o.Status.ToString(),
                LabelId = o.LabelId,
                OrderDetails = o.OrderDetails.Select(od => new OrderDetailViewModel
                {
                    ProductId = od.ProductId,
                    ProductName = od.Product?.Name ?? string.Empty,
                    ProductImage = od.Product?.MainImage,
                    Quantity = od.Quantity,
                    UnitPrice = od.Price
                }).ToList(),
                TotalPrice = totalPrice
            };

            // ✅ Lấy status từ GHTK nếu có LabelId
            if (!string.IsNullOrEmpty(viewModel.LabelId))
            {
                var ghtkStatus = await _ghtkService.GetStatusByLabelIdAsync(viewModel.LabelId);
                viewModel.GHTKStatus = ghtkStatus;

                if (ghtkStatus != null)
                    viewModel.Status = ghtkStatus.StatusText ?? _ghtkService.MapStatusText(ghtkStatus.Status);
            }

            return viewModel;
        }

        // thành:
        public async Task<List<OrderViewModel>> GetOrdersByStoreUserAsync(int userId)
        {
            // Lấy store theo userId
            var store = await _context.Stores.FirstOrDefaultAsync(s => s.UserId == userId);
            if (store == null) return new List<OrderViewModel>();

            // Lấy orders liên quan store
            var orders = await _context.Orders
                .Where(o => o.ShippingMethod != null && o.ShippingMethod.StoreId == store.Id)
                .Include(o => o.User)
                .Include(o => o.Voucher)
                .Include(o => o.ShippingMethod)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .ToListAsync();

            // Map sang ViewModel, tính TotalPrice cho mỗi order
            var orderVMs = new List<OrderViewModel>();

            foreach (var o in orders)
            {
                decimal totalPrice = 0;

                var orderDetailsVM = o.OrderDetails.Select(od =>
                {
                    var unitPrice = GetUnitPrice(od);
                    

                    totalPrice += unitPrice * od.Quantity;

                    return new OrderDetailViewModel
                    {
                        ProductId = od.ProductId,
                        ProductName = od.Product?.Name ?? string.Empty,
                        ProductImage = od.Product?.MainImage,
                        Quantity = od.Quantity,
                        UnitPrice = unitPrice
                    };
                }).ToList();

                var orderVM = new OrderViewModel
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
                    PaymentMethod = o.PaymentMethod,
                    PaymentStatus = o.PaymentStatus,
                    Status = o.Status.ToString(),
                    LabelId = o.LabelId,
                    OrderDetails = orderDetailsVM,
                    TotalPrice = totalPrice   // Gán tổng tiền sản phẩm
                };

                orderVMs.Add(orderVM);
            }

            // Giới hạn song song: tránh gọi API quá nhiều
            var semaphore = new SemaphoreSlim(5); // max 5 request cùng lúc
            var tasks = orderVMs.Select(async o =>
            {
                if (!string.IsNullOrEmpty(o.LabelId))
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        // Gọi GHTK API trả về GHTKOrderStatusViewModel
                        var ghtkStatus = await _ghtkService.GetStatusByLabelIdAsync(o.LabelId);
                        o.GHTKStatus = ghtkStatus;
                        // Nếu muốn override Status text từ GHTK
                        if (ghtkStatus != null)
                            o.Status = _ghtkService.MapStatusText(ghtkStatus.Status);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }
            });
            await Task.WhenAll(tasks);

            return orderVMs;
        }

        public async Task<object> GetStatisticsAsync(int storeId, DateTime? start, DateTime? end, DateTime? startCompare, DateTime? endCompare)
        {
            var currentOrders = await _context.Orders
                .Where(o => o.ShippingMethod != null && o.ShippingMethod.StoreId == storeId &&
                            (!start.HasValue || o.OrderDate >= start.Value) &&
                            (!end.HasValue || o.OrderDate <= end.Value))
                .Select(o => o.LabelId)
                .ToListAsync();

            var compareOrders = await _context.Orders
                .Where(o => o.ShippingMethod != null && o.ShippingMethod.StoreId == storeId &&
                            (!startCompare.HasValue || o.OrderDate >= startCompare.Value) &&
                            (!endCompare.HasValue || o.OrderDate <= endCompare.Value))
                .Select(o => o.LabelId)
                .ToListAsync();

            // mapping status của GHTK
            var pendingStatuses = new[] { 1, 2, 8, 12, 123, 128 };
            var shippingStatuses = new[] { 3, 4, 10, 45, 410 };
            var completedStatuses = new[] { 5, 6, 21 };

            async Task<(int total, int pending, int shipping, int completed)> GetCountsAsync(List<string> labelIds)
            {
                int total = 0, pending = 0, shipping = 0, completed = 0;

                foreach (var label in labelIds.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    var statusVm = await _ghtkService.GetStatusByLabelIdAsync(label);
                    if (statusVm == null) continue;

                    total++;
                    if (pendingStatuses.Contains(statusVm.Status)) pending++;
                    else if (shippingStatuses.Contains(statusVm.Status)) shipping++;
                    else if (completedStatuses.Contains(statusVm.Status)) completed++;
                }
                return (total, pending, shipping, completed);
            }

            var currentStats = await GetCountsAsync(currentOrders);
            var compareStats = await GetCountsAsync(compareOrders);

            double CalculatePercentChange(int current, int previous)
                => previous == 0 ? (current == 0 ? 0 : 100) : ((double)(current - previous) / previous) * 100;

            return new
            {
                totalOrders = currentStats.total,
                pendingOrders = currentStats.pending,
                shippingOrders = currentStats.shipping,
                completedOrders = currentStats.completed,

                totalOrdersPercentChange = CalculatePercentChange(currentStats.total, compareStats.total),
                pendingOrdersPercentChange = CalculatePercentChange(currentStats.pending, compareStats.pending),
                shippingOrdersPercentChange = CalculatePercentChange(currentStats.shipping, compareStats.shipping),
                completedOrdersPercentChange = CalculatePercentChange(currentStats.completed, compareStats.completed),
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
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var result = orders.Select(o => MapToOrderViewModel(o)).ToList();
             
            var semaphore = new SemaphoreSlim(5);
            var tasks = result.Select(async orderVm =>
            {
                if (!string.IsNullOrEmpty(orderVm.LabelId))
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var ghtkStatus = await _ghtkService.GetStatusByLabelIdAsync(orderVm.LabelId);
                        orderVm.GHTKStatus = ghtkStatus;

                        if (ghtkStatus != null)
                            orderVm.Status = ghtkStatus.StatusText ?? _ghtkService.MapStatusText(ghtkStatus.Status);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }
            });

            await Task.WhenAll(tasks);

            return result;
        }
        private decimal GetUnitPrice(OrderDetails od)
        {
            if (od.Product == null) return 0;

            if (od.Product.ProductVariants == null || !od.Product.ProductVariants.Any())
            {
                if (od.Product.Prices != null && od.Product.Prices.Any())
                {
                    return od.Product.Prices.First().Price; // lấy giá đầu tiên nếu có nhiều
                }
            }

            // Product variant: giá đã lưu trong OrderDetail
            return od.Price;
        }

        private OrderViewModel MapToOrderViewModel(Orders entity)
        {

            var totalPrice = entity.OrderDetails?.Sum(od => od.Quantity * od.Price) ?? 0;
            decimal voucherReduce = 0;

            if (entity.Voucher != null &&
                entity.Voucher.Status == VoucherStatus.Valid &&
                totalPrice >= entity.Voucher.MinOrder)
            {
                voucherReduce = entity.Voucher.Reduce;
            }

            return new OrderViewModel
            {
                Id = entity.Id,
                CreatedAt = entity.OrderDate,
                CustomerName = entity.User?.FullName ?? "Khách hàng",
                CustomerPhone = entity.User?.Phone,
                StoreName = entity.ShippingMethod?.store?.Name ?? "Không rõ",
                VoucherName = GetVoucherDisplayName(entity.Voucher),
                VoucherReduce = voucherReduce,
                LabelId = entity.LabelId,
                ShippingMethodName = entity.ShippingMethod?.MethodName ?? "",
                ShippingFee = entity.ShippingMethod?.Price ?? 0,
                PaymentMethod = entity.PaymentMethod ?? "",
                PaymentStatus = entity.PaymentStatus,
                Status = entity.Status.ToString(),
                OrderDetails = entity.OrderDetails?.Select(od => new OrderDetailViewModel
                {
                    ProductId = od.ProductId,
                    ProductName = od.Product?.Name ?? "Sản phẩm",
                    ProductImage = od.Product?.MainImage,
                    Quantity = od.Quantity,
                    UnitPrice = GetUnitPrice(od)

                }).ToList() ?? new List<OrderDetailViewModel>(),
                TotalPrice = totalPrice
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

            if (order == null)
                return null;

            var totalPrice = order.OrderDetails.Sum(od => od.Quantity * od.Price);

            decimal voucherReduce = 0;
            if (order.Voucher != null && order.Voucher.Status == VoucherStatus.Valid && totalPrice >= order.Voucher.MinOrder)
            {
                voucherReduce = order.Voucher.Reduce;
            }

            var viewModel = new OrderViewModel
            {
                Id = order.Id,
                CustomerName = order.User?.FullName ?? string.Empty,
                CustomerPhone = order.User?.Phone,
                StoreName = order.ShippingMethod?.store?.Name,
                ShippingMethodName = order.ShippingMethod?.MethodName ?? string.Empty,
                ShippingFee = order.ShippingMethod?.Price ?? 0,
                VoucherName = GetVoucherDisplayName(order.Voucher),
                VoucherReduce = voucherReduce,
                CreatedAt = order.OrderDate,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,
                Status = order.Status.ToString(),
                LabelId = order.LabelId,
                OrderDetails = order.OrderDetails.Select(od => new OrderDetailViewModel
                {
                    ProductId = od.ProductId,
                    ProductName = od.Product?.Name ?? string.Empty,
                    ProductImage = od.Product?.MainImage,
                    Quantity = od.Quantity,
                    UnitPrice = od.Price
                }).ToList(),
                TotalPrice = totalPrice
            };

            // ✅ Lấy trạng thái mới nhất từ GHTK nếu có LabelId
            if (!string.IsNullOrEmpty(viewModel.LabelId))
            {
                var ghtkStatus = await _ghtkService.GetStatusByLabelIdAsync(viewModel.LabelId);
                viewModel.GHTKStatus = ghtkStatus;

                if (ghtkStatus != null)
                    viewModel.Status = ghtkStatus.StatusText ?? _ghtkService.MapStatusText(ghtkStatus.Status);
            }

            return viewModel;
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