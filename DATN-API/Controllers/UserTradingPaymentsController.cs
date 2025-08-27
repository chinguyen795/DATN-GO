using DATN_API.Interfaces;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserTradingPaymentsController : ControllerBase
    {
        private readonly IUserTradingPaymentService _service;

        public UserTradingPaymentsController(IUserTradingPaymentService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUserId(int userId)
        {
            var result = await _service.GetByUserIdAsync(userId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(UserTradingPayment payment)
        {
            var created = await _service.CreateAsync(payment);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UserTradingPayment payment)
        {
            var updated = await _service.UpdateAsync(id, payment);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }

        [HttpPost("{id}/confirm")]
        public async Task<IActionResult> Confirm(int id)
        {
            var success = await _service.ConfirmAsync(id);
            if (!success) return NotFound();
            return Ok(new { message = "Payment confirmed" });
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> Reject(int id)
        {
            var success = await _service.RejectAsync(id);
            if (!success) return NotFound();
            return Ok(new { message = "Payment rejected" });
        }
    }
}
