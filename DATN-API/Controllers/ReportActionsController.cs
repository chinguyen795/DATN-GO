using DATN_API.Data;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportActionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportActionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/reportactions
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var actions = await _context.ReportActions
                .Include(ra => ra.Report)
                .Include(ra => ra.User)
                .ToListAsync();

            return Ok(actions);
        }

        // GET: api/reportactions/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var action = await _context.ReportActions
                .Include(ra => ra.Report)
                .Include(ra => ra.User)
                .FirstOrDefaultAsync(ra => ra.Id == id);

            if (action == null) return NotFound();

            return Ok(action);
        }

        // GET: api/reportactions/report/10
        [HttpGet("report/{reportId}")]
        public async Task<IActionResult> GetByReportId(int reportId)
        {
            var actions = await _context.ReportActions
                .Where(ra => ra.ReportId == reportId)
                .Include(ra => ra.User)
                .ToListAsync();

            return Ok(actions);
        }

        // POST: api/reportactions
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ReportActions model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            model.CreatedAt = DateTime.UtcNow;

            _context.ReportActions.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        // PUT: api/reportactions/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ReportActions model)
        {
            if (id != model.Id)
                return BadRequest("ID không khớp");

            var action = await _context.ReportActions.FindAsync(id);
            if (action == null) return NotFound();

            action.ReportId = model.ReportId;
            action.UserId = model.UserId;
            action.Action = model.Action;
            action.Notes = model.Notes;
            action.CreatedAt = model.CreatedAt;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/reportactions/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var action = await _context.ReportActions.FindAsync(id);
            if (action == null) return NotFound();

            _context.ReportActions.Remove(action);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
