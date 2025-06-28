using DATN_API.Interfaces;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DATN_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeliveryTrackingsController : ControllerBase
    {
        private readonly IDeliveryTrackingsService _service;

        public DeliveryTrackingsController(IDeliveryTrackingsService service)
        {
            _service = service;
        }

        // GET: api/deliverytrackings
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var trackings = await _service.GetAllAsync();
            return Ok(trackings);
        }

        // GET: api/deliverytrackings/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var tracking = await _service.GetByIdAsync(id);
            if (tracking == null) return NotFound();
            return Ok(tracking);
        }

        // GET: api/deliverytrackings/order/10
        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetByOrderId(int orderId)
        {
            var tracking = await _service.GetByOrderIdAsync(orderId);
            if (tracking == null) return NotFound();
            return Ok(tracking);
        }

        // POST: api/deliverytrackings
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DeliveryTrackings model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var created = await _service.CreateAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/deliverytrackings/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] DeliveryTrackings model)
        {
            if (!await _service.UpdateAsync(id, model))
                return BadRequest("ID không khớp hoặc không tìm thấy delivery tracking");
            return NoContent();
        }

        // DELETE: api/deliverytrackings/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _service.DeleteAsync(id))
                return NotFound();
            return NoContent();
        }
    }
}
