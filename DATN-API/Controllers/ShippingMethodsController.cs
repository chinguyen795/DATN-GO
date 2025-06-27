using DATN_API.Data;
using DATN_API.Models;
using DATN_API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShippingMethodsController : ControllerBase
    {
        private readonly IShippingMethodsService _service;

        public ShippingMethodsController(IShippingMethodsService service)
        {
            _service = service;
        }

        // GET: api/shippingmethods
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var methods = await _service.GetAllAsync();
            return Ok(methods);
        }

        // GET: api/shippingmethods/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var method = await _service.GetByIdAsync(id);
            if (method == null) return NotFound();
            return Ok(method);
        }

        // GET: api/shippingmethods/diner/3
        [HttpGet("diner/{dinerId}")]
        public async Task<IActionResult> GetByDinerId(int dinerId)
        {
            // Nếu cần, có thể thêm hàm GetByDinerIdAsync vào service
            return BadRequest("Chức năng này chưa được hỗ trợ ở service");
        }

        // POST: api/shippingmethods
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ShippingMethods model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var created = await _service.CreateAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/shippingmethods/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ShippingMethods model)
        {
            if (!await _service.UpdateAsync(id, model))
                return BadRequest("ID không khớp hoặc không tìm thấy shipping method");
            return NoContent();
        }

        // DELETE: api/shippingmethods/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _service.DeleteAsync(id))
                return NotFound();
            return NoContent();
        }
    }
}
