using DATN_API.Interfaces;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;

namespace DATN_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TradingPaymentController : ControllerBase
    {
        private readonly ITradingPaymentService _service;

        public TradingPaymentController(ITradingPaymentService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var payments = await _service.GetAllAsync();
            foreach (var p in payments)
            {
                Console.WriteLine($"{p.Id} | StoreId={p.StoreId} | StoreName={p.Store?.Name} | Bank={p.Store?.Bank}");
            }
            return Ok(payments);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var payment = await _service.GetByIdAsync(id);
            if (payment == null) return NotFound();
            return Ok(payment);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TradingPayment payment)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var created = await _service.CreateAsync(payment);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] TradingPayment payment)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var updated = await _service.UpdateAsync(id, payment);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
        [HttpPut("{id}/reject")]
        public async Task<IActionResult> Reject(int id)
        {
            var result = await _service.RejectAsync(id);
            if (!result) return NotFound("TradingPayment not found");
            return Ok("TradingPayment has been rejected");
        }

        [HttpPut("{id}/confirm")]
        public async Task<IActionResult> Confirm(int id)
        {
            var result = await _service.ConfirmAsync(id);
            if (!result) return NotFound("TradingPayment not found");
            return Ok("TradingPayment confirmed, Store's MoneyAmount reset to 0");
        }

        [HttpGet("store/{storeId}")]
        public async Task<IActionResult> GetByStoreId(int storeId)
        {
            var payments = await _service.GetByStoreIdAsync(storeId);
            return Ok(payments);
        }
    }
}
