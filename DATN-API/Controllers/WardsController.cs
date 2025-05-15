using DATN_API.Data;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WardsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public WardsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/wards
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var wards = await _context.Wards
                .Include(w => w.District)
                .ToListAsync();

            return Ok(wards);
        }

        // GET: api/wards/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var ward = await _context.Wards
                .Include(w => w.District)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (ward == null) return NotFound();

            return Ok(ward);
        }

        // GET: api/wards/district/3
        [HttpGet("district/{districtId}")]
        public async Task<IActionResult> GetByDistrictId(int districtId)
        {
            var wards = await _context.Wards
                .Where(w => w.DistrictId == districtId)
                .ToListAsync();

            return Ok(wards);
        }

        // POST: api/wards
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Wards model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Wards.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        // PUT: api/wards/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Wards model)
        {
            if (id != model.Id)
                return BadRequest("ID không khớp");

            var ward = await _context.Wards.FindAsync(id);
            if (ward == null) return NotFound();

            ward.DistrictId = model.DistrictId;
            ward.WardName = model.WardName;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/wards/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ward = await _context.Wards.FindAsync(id);
            if (ward == null) return NotFound();

            _context.Wards.Remove(ward);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
