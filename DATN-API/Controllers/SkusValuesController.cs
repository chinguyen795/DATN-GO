using DATN_API.Data;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SkusValuesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SkusValuesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/skusvalues
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.SkusValues
                .Include(sv => sv.OptionValue)
                .Include(sv => sv.ProductSku)
                .Include(sv => sv.Product)
                .Include(sv => sv.Option)
                .ToListAsync();

            return Ok(list);
        }

        // GET: api/skusvalues/key?valueId=1&skuId=2&productId=3&optionId=4
        [HttpGet("key")]
        public async Task<IActionResult> GetByKey(int valueId, int skuId, int productId, int optionId)
        {
            var item = await _context.SkusValues.FindAsync(valueId, skuId, productId, optionId);
            if (item == null) return NotFound();

            return Ok(item);
        }

        // POST: api/skusvalues
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SkusValues model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var exists = await _context.SkusValues.FindAsync(model.ValueId, model.SkuId, model.ProductId, model.OptionId);
            if (exists != null)
                return Conflict("Entry already exists.");

            _context.SkusValues.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetByKey), new { valueId = model.ValueId, skuId = model.SkuId, productId = model.ProductId, optionId = model.OptionId }, model);
        }

        // DELETE: api/skusvalues/key?valueId=1&skuId=2&productId=3&optionId=4
        [HttpDelete("key")]
        public async Task<IActionResult> Delete(int valueId, int skuId, int productId, int optionId)
        {
            var item = await _context.SkusValues.FindAsync(valueId, skuId, productId, optionId);
            if (item == null) return NotFound();

            _context.SkusValues.Remove(item);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
