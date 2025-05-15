using DATN_API.Data;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OptionValuesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OptionValuesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/optionvalues
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var optionValues = await _context.OptionValues
                .Include(ov => ov.Option)
                .Include(ov => ov.Product)
                .ToListAsync();

            return Ok(optionValues);
        }

        // GET: api/optionvalues/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var optionValue = await _context.OptionValues
                .Include(ov => ov.Option)
                .Include(ov => ov.Product)
                .FirstOrDefaultAsync(ov => ov.Id == id);

            if (optionValue == null) return NotFound();

            return Ok(optionValue);
        }

        // GET: api/optionvalues/option/3
        [HttpGet("option/{optionId}")]
        public async Task<IActionResult> GetByOptionId(int optionId)
        {
            var optionValues = await _context.OptionValues
                .Where(ov => ov.OptionId == optionId)
                .Include(ov => ov.Product)
                .ToListAsync();

            return Ok(optionValues);
        }

        // POST: api/optionvalues
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OptionValues model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.OptionValues.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        // PUT: api/optionvalues/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] OptionValues model)
        {
            if (id != model.Id)
                return BadRequest("ID không khớp");

            var optionValue = await _context.OptionValues.FindAsync(id);
            if (optionValue == null) return NotFound();

            optionValue.OptionId = model.OptionId;
            optionValue.ProductId = model.ProductId;
            optionValue.ValueName = model.ValueName;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/optionvalues/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var optionValue = await _context.OptionValues.FindAsync(id);
            if (optionValue == null) return NotFound();

            _context.OptionValues.Remove(optionValue);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
