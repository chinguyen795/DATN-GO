using DATN_API.Data;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/reports
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var reports = await _context.Reports
                .Include(r => r.User)
                .Include(r => r.Diner)
                .Include(r => r.Actions)
                .ToListAsync();

            return Ok(reports);
        }

        // GET: api/reports/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var report = await _context.Reports
                .Include(r => r.User)
                .Include(r => r.Diner)
                .Include(r => r.Actions)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null) return NotFound();

            return Ok(report);
        }

        // GET: api/reports/user/3
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUserId(int userId)
        {
            var reports = await _context.Reports
                .Where(r => r.UserId == userId)
                .Include(r => r.Diner)
                .Include(r => r.Actions)
                .ToListAsync();

            return Ok(reports);
        }

        // POST: api/reports
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Reports model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            model.CreatedAt = DateTime.UtcNow;

            _context.Reports.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        // PUT: api/reports/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Reports model)
        {
            if (id != model.Id)
                return BadRequest("ID không khớp");

            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound();

            report.UserId = model.UserId;
            report.DinerId = model.DinerId;
            report.Reanson = model.Reanson;
            report.Status = model.Status;
            report.CreatedAt = model.CreatedAt;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/reports/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound();

            _context.Reports.Remove(report);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
