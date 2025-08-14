using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using DATN_API.Services.Interfaces;
using DATN_API.ViewModels;
using DATN_API.ViewModels.Orders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IGHTKService _ghtkService;
        private readonly IOrdersService _service;

        
        public OrdersController(ApplicationDbContext db, IOrdersService service, IGHTKService ghtkService )
        {
            _db = db;
            _service = service;
            _ghtkService = ghtkService;
        }

        // GET: api/orders
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var orders = await _service.GetAllAsync();
            return Ok(orders);
        }

        // GET: api/orders/{id}
        // ✅ Trả OrderDetailDto để MVC gọi /orders/{id} là dùng được
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var dto = await BuildOrderDetailDtoAsync(id);
            if (dto == null) return NotFound("Order not found");
            return Ok(dto);
        }

        // GET: api/orders/{id}/detail
        // ✅ Alias sang cùng kết quả như GetById
        [HttpGet("{id:int}/detail")]
        public async Task<IActionResult> GetOrderDetail(int id)
        {
            // ví dụ dùng _db hoặc gọi _service
            var dto = await _service.GetOrderDetailAsync(id);
            if (dto == null) return NotFound("Order not found");
            return Ok(dto);
        }

        // POST: api/orders
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Orders model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _service.CreateAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/orders/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Orders model)
        {
            var ok = await _service.UpdateAsync(id, model);
            if (!ok) return BadRequest("ID không khớp hoặc không tìm thấy order");
            return NoContent();
        }

        // DELETE: api/orders/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _service.DeleteAsync(id);
            if (!ok) return NotFound();
            return NoContent();
        }

        // GET: api/orders/all-by-user/{userId}
        [HttpGet("all-by-user/{userId:int}")]
        public async Task<IActionResult> GetAllByUser(int userId)
            => Ok(await _service.GetOrdersByUserIdAsync(userId));

        // GET: api/orders/all-by-store/{userId}
        [HttpGet("all-by-store/{userId:int}")]
        public async Task<List<OrderViewModel>> GetOrdersByStoreUserWithStatusAsync(int userId)
        {
            // Lấy danh sách đơn hàng theo store-user
            var orders = await _service.GetOrdersByStoreUserAsync(userId);  

            // Nếu không có GHTKService thì inject vào OrdersService
            foreach (var order in orders)
            {
                if (!string.IsNullOrEmpty(order.LabelId))
                {
                    order.GHTKStatus = await _ghtkService.GetStatusByLabelIdAsync(order.LabelId);
                }
            }

            return orders;
        }

        // PATCH: api/orders/updatestatus/{id}?status=ChoLayHang
        [HttpPatch("updatestatus/{id:int}")]
        public async Task<IActionResult> UpdateStatus(int id, [FromQuery] OrderStatus status)
        {
            var (success, message) = await _service.UpdateStatusAsync(id, status);
            if (!success) return NotFound(message);
            return Ok(message);
        }

        // GET: api/orders/statistics?storeId=1&start=...&end=...&startCompare=...&endCompare=...
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics(
            [FromQuery] int storeId,
            [FromQuery] DateTime? start,
            [FromQuery] DateTime? end,
            [FromQuery] DateTime? startCompare = null,
            [FromQuery] DateTime? endCompare = null)
        {
            if (storeId <= 0)
                return BadRequest("Thiếu hoặc sai StoreId");

            var result = await _service.GetStatisticsAsync(storeId, start, end, startCompare, endCompare);
            return Ok(result);
        }
         
        private async Task<OrderDetailDto?> BuildOrderDetailDtoAsync(int id)
        {
            var order = await _db.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.ShippingMethod)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return null;

            return new OrderDetailDto
            {
                OrderId = order.Id,
                OrderDate = order.OrderDate,
                PaymentMethod = order.PaymentMethod ?? "",
                PaymentStatus = order.PaymentStatus ?? "",
                Status = order.Status,
                DeliveryFee = order.DeliveryFee,
                ItemsTotal = order.OrderDetails?.Sum(d => d.Price * d.Quantity) ?? 0,
                TotalPrice = order.TotalPrice,
                ShippingMethodName = order.ShippingMethod?.MethodName,
                CustomerName = order.User?.FullName ?? "",
                CustomerPhone = order.User?.Phone ?? "",
                Items = order.OrderDetails?.Select(d => new OrderDetailItemDto
                {
                    ProductId = d.ProductId,
                    ProductName = d.Product?.Name ?? "Sản phẩm",
                    Image = d.Product?.MainImage,
                    Quantity = d.Quantity,
                    Price = d.Price
                }).ToList() ?? new List<OrderDetailItemDto>()
            };
        }
        [HttpGet("{id}/user/{userId}")]
        public async Task<IActionResult> GetByIdForUser(int id, int userId)
        {
            var order = await _service.GetOrderDetailByIdAsync(id, userId);

            if (order == null)
                return NotFound();

            return Ok(order);
        }
        // OrdersController (API)
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetOrdersByUser(int userId)
        {
            var data = await _db.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new
                {
                    o.Id,
                    o.OrderDate,
                    o.TotalPrice,
                    o.PaymentMethod,
                    o.PaymentStatus,
                    Status = o.Status.ToString() // ChoXuLy, ChoLayHang, DangGiao, DaHoanThanh, DaHuy
                })
                .ToListAsync();

            return Ok(data);
        }

    }
}
