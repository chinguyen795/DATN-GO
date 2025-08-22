using DATN_API.Data;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderDetailsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;


        public OrderDetailsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/orderdetails
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var details = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Product)
                .ToListAsync();

            return Ok(details);
        }

        // GET: api/orderdetails/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var detail = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Product)
                .FirstOrDefaultAsync(od => od.Id == id);

            if (detail == null) return NotFound();

            return Ok(detail);
        }

        // GET: api/orderdetails/order/10
        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetByOrderId(int orderId)
        {
            var details = await _context.OrderDetails
                .Where(od => od.OrderId == orderId)
                .Include(od => od.Product)
                .ToListAsync();

            return Ok(details);
        }

        // POST: api/orderdetails
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OrderDetails model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.OrderDetails.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        // PUT: api/orderdetails/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] OrderDetails model)
        {
            if (id != model.Id)
                return BadRequest("ID không khớp");

            var detail = await _context.OrderDetails.FindAsync(id);
            if (detail == null) return NotFound();

            detail.OrderId = model.OrderId;
            detail.ProductId = model.ProductId;
            detail.Quantity = model.Quantity;
            detail.Price = model.Price;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/orderdetails/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var detail = await _context.OrderDetails.FindAsync(id);
            if (detail == null) return NotFound();

            _context.OrderDetails.Remove(detail);
            await _context.SaveChangesAsync();

            return NoContent();
        }

    }
}
