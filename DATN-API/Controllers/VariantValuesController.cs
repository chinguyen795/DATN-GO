using DATN_API.Models;
using DATN_API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DATN_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VariantValuesController : ControllerBase
    {
        private readonly IVariantValuesService _service;

        public VariantValuesController(IVariantValuesService service)
        {
            _service = service;
        }

        // GET: api/variantvalues
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var values = await _service.GetAllAsync();
            return Ok(values);
        }

        // GET: api/variantvalues/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var value = await _service.GetByIdAsync(id);
            if (value == null) return NotFound();
            return Ok(value);
        }

        // POST: api/variantvalues
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] VariantValues model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var created = await _service.CreateAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/variantvalues/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] VariantValues model)
        {
            if (!await _service.UpdateAsync(id, model))
                return BadRequest("ID không khớp hoặc không tìm thấy variant value");
            return NoContent();
        }

        // DELETE: api/variantvalues/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _service.DeleteAsync(id))
                return NotFound();
            return NoContent();
        }
    }
}