using DATN_API.Data;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
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

        // Mới
        [HttpGet("by-name")]
        public async Task<IActionResult> GetByName([FromQuery] string name, [FromQuery] int cityId)
        {
            if (string.IsNullOrWhiteSpace(name) || cityId <= 0)
                return BadRequest("Thiếu name/cityId");

            string target = Canonical(name);

            var list = await _context.Districts
                .Where(d => d.CityId == cityId)
                .ToListAsync();

            var found = list.FirstOrDefault(d => Canonical(d.DistrictName) == target);
            if (found == null) return NotFound();

            return Ok(found);
        }

        // --- Helpers: normalize & compare (copy dùng chung trong controller) ---
        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (var c in normalized)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string Canonical(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            s = s.Trim().ToLowerInvariant();
            s = RemoveDiacritics(s);

            // bỏ tiền tố phổ biến
            var prefixes = new[]
            { "tinh ", "tỉnh ", "thanh pho ", "thành phố ", "tp ", "tp. ",
      "quan ", "quận ", "huyen ", "huyện ", "thi xa ", "thị xã ",
      "thi tran ", "thị trấn ", "xa ", "xã ", "phuong ", "phường ",
      "p. ", "q. ", "h. ", "tt. " };

            s = s.Replace(".", " ").Replace(",", " ").Replace("-", " ");
            while (s.Contains("  ")) s = s.Replace("  ", " ");
            foreach (var p in prefixes)
                if (s.StartsWith(p, StringComparison.Ordinal)) { s = s[p.Length..]; break; }

            s = s.Trim();
            if (int.TryParse(s, out var num)) s = num.ToString(); // "01" -> "1"
            return s;
        }


    }
}