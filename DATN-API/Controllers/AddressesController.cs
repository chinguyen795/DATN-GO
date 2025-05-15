using DATN_API.Data;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AddressesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AddressesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/addresses
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var addresses = await _context.Addresses
                .Include(a => a.User)
                .ToListAsync();
            return Ok(addresses);
        }

        // GET: api/addresses/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var address = await _context.Addresses
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (address == null) return NotFound();

            return Ok(address);
        }

        // POST: api/addresses
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Addresses model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Addresses.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        // PUT: api/addresses/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Addresses model)
        {
            if (id != model.Id)
                return BadRequest("ID không khớp");

            var address = await _context.Addresses.FindAsync(id);
            if (address == null)
                return NotFound();

            address.Longitude = model.Longitude;
            address.Latitude = model.Latitude;
            address.Discription = model.Discription;
            address.Status = model.Status;
            address.UserId = model.UserId;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/addresses/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var address = await _context.Addresses.FindAsync(id);
            if (address == null)
                return NotFound();

            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
