using DATN_API.Data;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OptionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OptionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/options
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var options = await _context.Options
                .Include(o => o.Product)
                .Include(o => o.OptionValues)
                .ToListAsync();

            return Ok(options);
        }

        // GET: api/options/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var option = await _context.Options
                .Include(o => o.Product)
                .Include(o => o.OptionValues)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (option == null) return NotFound();

            return Ok(option);
        }

        // GET: api/options/product/3
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetByProductId(int productId)
        {
            var options = await _context.Options
                .Where(o => o.ProductId == productId)
                .Include(o => o.OptionValues)
                .ToListAsync();

            return Ok(options);
        }

        // POST: api/options
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Options model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Options.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        // PUT: api/options/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Options model)
        {
            if (id != model.Id)
                return BadRequest("ID không khớp");

            var option = await _context.Options.FindAsync(id);
            if (option == null) return NotFound();

            option.ProductId = model.ProductId;
            option.OptionName = model.OptionName;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/options/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var option = await _context.Options.FindAsync(id);
            if (option == null) return NotFound();

            _context.Options.Remove(option);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
