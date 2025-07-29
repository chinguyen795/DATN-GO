using DATN_API.Data;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using static DATN_API.Controllers.CitiesController;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DistrictsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DistrictsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/districts
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var districts = await _context.Districts
                .Include(d => d.City)
                .Include(d => d.Wards)
                .ToListAsync();

            return Ok(districts);
        }

        // GET: api/districts/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var district = await _context.Districts
                .Include(d => d.City)
                .Include(d => d.Wards)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (district == null)
                return NotFound();

            return Ok(district);
        }

        // GET: api/districts/city/3
        [HttpGet("city/{cityId}")]
        public async Task<IActionResult> GetByCityId(int cityId)
        {
            var districts = await _context.Districts
                .Where(d => d.CityId == cityId)
                .Include(d => d.Wards)
                .ToListAsync();

            return Ok(districts);
        }

        // POST: api/districts

        //[HttpPost]
        //public async Task<IActionResult> Create([FromBody] Districts model)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    model.City = null;
        //    model.Wards = null;

        //    _context.Districts.Add(model);
        //    await _context.SaveChangesAsync();

        //    return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        //}



        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Districts model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Districts.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        // PUT: api/districts/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Districts model)
        {
            if (id != model.Id)
                return BadRequest("ID không khớp");

            var district = await _context.Districts.FindAsync(id);
            if (district == null)
                return NotFound();

            district.CityId = model.CityId;
            district.DistrictName = model.DistrictName;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // FIX CODE ĐỂ XÓA THÀNH CÔNG
        // DELETE: api/districts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var district = await _context.Districts
                .Include(d => d.Wards)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (district == null)
                return NotFound();

            // ❌ Nếu còn Wards thì xoá từng cái trước
            if (district.Wards != null && district.Wards.Any())
                _context.Wards.RemoveRange(district.Wards);

            _context.Districts.Remove(district);
            await _context.SaveChangesAsync();

            return NoContent();
        }


    }
}