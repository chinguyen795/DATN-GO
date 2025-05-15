using DATN_API.Data;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeliveryTrackingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DeliveryTrackingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/deliverytrackings
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var trackings = await _context.DeliveryTrackings
                .Include(dt => dt.Order)
                .ToListAsync();

            return Ok(trackings);
        }

        // GET: api/deliverytrackings/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var tracking = await _context.DeliveryTrackings
                .Include(dt => dt.Order)
                .FirstOrDefaultAsync(dt => dt.Id == id);

            if (tracking == null) return NotFound();

            return Ok(tracking);
        }

        // GET: api/deliverytrackings/order/10
        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetByOrderId(int orderId)
        {
            var tracking = await _context.DeliveryTrackings
                .Include(dt => dt.Order)
                .FirstOrDefaultAsync(dt => dt.OrderId == orderId);

            if (tracking == null) return NotFound();

            return Ok(tracking);
        }

        // POST: api/deliverytrackings
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DeliveryTrackings model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            model.CreateAt = DateTime.UtcNow;

            _context.DeliveryTrackings.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        // PUT: api/deliverytrackings/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] DeliveryTrackings model)
        {
            if (id != model.Id)
                return BadRequest("ID không khớp");

            var tracking = await _context.DeliveryTrackings.FindAsync(id);
            if (tracking == null) return NotFound();

            tracking.OrderId = model.OrderId;
            tracking.AhamoveOrderId = model.AhamoveOrderId;
            tracking.ServiceId = model.ServiceId;
            tracking.TrackingUrl = model.TrackingUrl;
            tracking.DriverName = model.DriverName;
            tracking.DriverPhone = model.DriverPhone;
            tracking.EstimatedTime = model.EstimatedTime;
            tracking.Status = model.Status;
            tracking.CreateAt = model.CreateAt;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/deliverytrackings/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var tracking = await _context.DeliveryTrackings.FindAsync(id);
            if (tracking == null) return NotFound();

            _context.DeliveryTrackings.Remove(tracking);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
