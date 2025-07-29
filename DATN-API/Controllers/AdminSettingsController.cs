using DATN_API.Data;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminSettingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminSettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/adminsettings
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var adminSettings = await _context.AdminSettings
                .Include(a => a.User)
                .ToListAsync();

            return Ok(adminSettings);
        }

        // GET: api/adminsettings/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var adminSetting = await _context.AdminSettings
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (adminSetting == null) return NotFound();

            return Ok(adminSetting);
        }

        // POST: api/adminsettings
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AdminSettings model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            model.UpdateAt = DateTime.UtcNow; // Set current date time for UpdateAt
            _context.AdminSettings.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        // PUT: api/adminsettings/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] AdminSettings model)
        {
            if (id != model.Id)
                return BadRequest("ID không khớp");

            var adminSetting = await _context.AdminSettings.FindAsync(id);
            if (adminSetting == null) return NotFound();

            adminSetting.Theme = model.Theme;
            adminSetting.Logo = model.Logo;
            adminSetting.UpdateAt = DateTime.UtcNow;  // Update the time of update

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/adminsettings/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var adminSetting = await _context.AdminSettings.FindAsync(id);
            if (adminSetting == null) return NotFound();

            _context.AdminSettings.Remove(adminSetting);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}