using DATN_API.Data;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DecoratesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DecoratesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/decorates
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var decorates = await _context.Decorates
                .Include(d => d.User)
                .ToListAsync();
            return Ok(decorates);
        }

        // GET: api/decorates/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var decorate = await _context.Decorates
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (decorate == null) return NotFound();

            return Ok(decorate);
        }

        // POST: api/decorates
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Decorates model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Decorates.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        // PUT: api/decorates/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Decorates model)
        {
            if (id != model.Id)
                return BadRequest("ID không khớp");

            var decorate = await _context.Decorates.FindAsync(id);
            if (decorate == null) return NotFound();

            decorate.UserId = model.UserId;
            decorate.Title = model.Title;
            decorate.Image = model.Image;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/decorates/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var decorate = await _context.Decorates.FindAsync(id);
            if (decorate == null) return NotFound();

            _context.Decorates.Remove(decorate);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
