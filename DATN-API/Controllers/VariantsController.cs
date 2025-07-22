using DATN_API.Interfaces;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DATN_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VariantsController : ControllerBase
    {
        private readonly IVariantsService _service;

        public VariantsController(IVariantsService service)
        {
            _service = service;
        }

        // GET: api/variants
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var variants = await _service.GetAllAsync();
            return Ok(variants);
        }

        // GET: api/variants/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var variant = await _service.GetByIdAsync(id);
            if (variant == null) return NotFound();
            return Ok(variant);
        }

        // POST: api/variants
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Variants model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var created = await _service.CreateAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/variants/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Variants model)
        {
            if (!await _service.UpdateAsync(id, model))
                return BadRequest("ID không khớp hoặc không tìm thấy variant");
            return NoContent();
        }

        // DELETE: api/variants/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _service.DeleteAsync(id))
                return NotFound();
            return NoContent();
        }
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetByProductId(int productId)
        {
            var variants = await _service.GetByProductIdAsync(productId);
            return Ok(variants);
        }

    }
}