using DATN_API.Data;
using DATN_API.Models;
using DATN_API.Services;
using DATN_API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrdersService _service;

        public OrdersController(IOrdersService service)
        {
            _service = service;
        }

        // GET: api/orders
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var orders = await _service.GetAllAsync();

            return Ok(orders);
        }

        // GET: api/orders/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var order = await _service.GetOrderDetailAsync(id);

            if (order == null) return NotFound();

            return Ok(order);
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

        // PUT: api/orders/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Orders model)
        {
            if (!await _service.UpdateAsync(id, model))
                return BadRequest("ID không khớp hoặc không tìm thấy order");

            return NoContent();
        }

        // DELETE: api/orders/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _service.DeleteAsync(id))
                return NotFound();

            return NoContent();
        }
        [HttpGet("all-by-user/{userId}")]
        public async Task<IActionResult> GetAllByUser(int userId)
            => Ok(await _service.GetOrdersByUserIdAsync(userId));


        [HttpGet("all-by-store/{userId}")]
        public async Task<IActionResult> GetAllByStore(int userId)
             => Ok(await _service.GetOrdersByStoreUserAsync(userId));
        [HttpPatch("updatestatus/{id}")]
        public async Task<IActionResult> UpdateStatus(int id, [FromQuery] OrderStatus status)
        {
            var (success, message) = await _service.UpdateStatusAsync(id, status);

            if (!success)
                return NotFound(message);

            return Ok(message);
        }
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
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetOrdersByUser(int userId)
        {
            var orders = await _service.GetOrdersByUserIdAsync(userId);
            return Ok(orders);
        }

    }
}