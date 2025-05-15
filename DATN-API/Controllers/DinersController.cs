using DATN_API.Data;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DinersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DinersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/diners
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var diners = await _context.Diners
                .Include(d => d.User)
                .Include(d => d.ReportsReceived)
                .Include(d => d.Products)
                .Include(d => d.ShippingMethods)
                .ToListAsync();

            return Ok(diners);
        }

        // GET: api/diners/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var diner = await _context.Diners
                .Include(d => d.User)
                .Include(d => d.ReportsReceived)
                .Include(d => d.Products)
                .Include(d => d.ShippingMethods)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (diner == null) return NotFound();

            return Ok(diner);
        }

        // POST: api/diners
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Diners model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            model.CreateAt = DateTime.UtcNow;

            _context.Diners.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        // PUT: api/diners/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Diners model)
        {
            if (id != model.Id)
                return BadRequest("ID không khớp");

            var diner = await _context.Diners.FindAsync(id);
            if (diner == null) return NotFound();

            diner.UserId = model.UserId;
            diner.DinerName = model.DinerName;
            diner.DinerAddress = model.DinerAddress;
            diner.Longitude = model.Longitude;
            diner.Latitude = model.Latitude;
            diner.Avatar = model.Avatar;
            diner.Status = model.Status;
            diner.CoverPhoto = model.CoverPhoto;
            // Không update CreateAt để giữ thời gian tạo ban đầu

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/diners/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var diner = await _context.Diners.FindAsync(id);
            if (diner == null) return NotFound();

            _context.Diners.Remove(diner);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
