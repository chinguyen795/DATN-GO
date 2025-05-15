using DATN_API.Data;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CitiesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CitiesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/cities
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var cities = await _context.Cities
                .Include(c => c.Districts)
                .ToListAsync();

            return Ok(cities);
        }

        // GET: api/cities/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var city = await _context.Cities
                .Include(c => c.Districts)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (city == null)
                return NotFound();

            return Ok(city);
        }

        // POST: api/cities
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Cities model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Cities.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        // PUT: api/cities/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Cities model)
        {
            if (id != model.Id)
                return BadRequest("ID không khớp");

            var city = await _context.Cities.FindAsync(id);
            if (city == null)
                return NotFound();

            city.CityName = model.CityName;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/cities/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var city = await _context.Cities.FindAsync(id);
            if (city == null)
                return NotFound();

            _context.Cities.Remove(city);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
