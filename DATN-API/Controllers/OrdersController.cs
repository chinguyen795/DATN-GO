using DATN_API.Data;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/orders
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Voucher)
                .Include(o => o.ShippingMethod)
                .Include(o => o.Reviews)
                .Include(o => o.DeliveryTracking)
                .Include(o => o.OrderDetails)
                .ToListAsync();

            return Ok(orders);
        }

        // GET: api/orders/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Voucher)
                .Include(o => o.ShippingMethod)
                .Include(o => o.Reviews)
                .Include(o => o.DeliveryTracking)
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return Ok(order);
        }

        // GET: api/orders/user/3
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUserId(int userId)
        {
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.Voucher)
                .Include(o => o.ShippingMethod)
                .Include(o => o.Reviews)
                .Include(o => o.DeliveryTracking)
                .Include(o => o.OrderDetails)
                .ToListAsync();

            return Ok(orders);
        }

        // POST: api/orders
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Orders model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            model.OrderDate = DateTime.UtcNow;

            _context.Orders.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        // PUT: api/orders/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Orders model)
        {
            if (id != model.Id)
                return BadRequest("ID không khớp");

            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.UserId = model.UserId;
            order.VoucherId = model.VoucherId;
            order.ShippingMethodId = model.ShippingMethodId;
            order.OrderDate = model.OrderDate;
            order.TotalPrice = model.TotalPrice;
            order.Status = model.Status;
            order.PaymentMethod = model.PaymentMethod;
            order.TransactionId = model.TransactionId;
            order.PaymentDate = model.PaymentDate;
            order.PaymentStatus = model.PaymentStatus;
            order.DeliveryFee = model.DeliveryFee;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/orders/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
