using DATN_API.Interfaces;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace DATN_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WardsController : ControllerBase
    {
        private readonly IWardsService _service;

        public WardsController(IWardsService service)
        {
            _service = service;
        }

        // GET: api/wards
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var wards = await _service.GetAllAsync();
            return Ok(wards);
        }

        // GET: api/wards/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var ward = await _service.GetByIdAsync(id);
            if (ward == null) return NotFound();
            return Ok(ward);
        }

        // GET: api/wards/district/3
        [HttpGet("district/{districtId}")]
        public async Task<IActionResult> GetByDistrictId(int districtId)
        {
            var wards = await _service.GetByDistrictIdAsync(districtId);
            return Ok(wards);
        }

        // POST: api/wards

        //[HttpPost]
        //public async Task<IActionResult> Create([FromBody] Wards model)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    model.District = null;

        //    var created = await _service.CreateAsync(model);
        //    return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        //}


        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Wards model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var created = await _service.CreateAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/wards/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Wards model)
        {
            if (!await _service.UpdateAsync(id, model))
                return BadRequest("ID không khớp hoặc không tìm thấy ward");
            return NoContent();
        }

        // DELETE: api/wards/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _service.DeleteAsync(id))
                return NotFound();
            return NoContent();
        }

        // Mới

        [HttpGet("by-name")]
        public async Task<IActionResult> GetByName([FromQuery] string name, [FromQuery] int districtId)
        {
            if (string.IsNullOrWhiteSpace(name) || districtId <= 0)
                return BadRequest("Thiếu name/districtId");

            string target = Canonical(name);

            var wards = await _service.GetByDistrictIdAsync(districtId);
            var found = wards.FirstOrDefault(w => Canonical(w.WardName) == target);

            if (found == null) return NotFound();
            return Ok(found);
        }

        // --- Helpers: copy y như ở DistrictsController ---
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

            var prefixes = new[]
            { "quan ", "quận ", "huyen ", "huyện ", "thi xa ", "thị xã ",
      "thi tran ", "thị trấn ", "xa ", "xã ", "phuong ", "phường ",
      "p. ", "q. ", "h. ", "tt. " };

            s = s.Replace(".", " ").Replace(",", " ").Replace("-", " ");
            while (s.Contains("  ")) s = s.Replace("  ", " ");
            foreach (var p in prefixes)
                if (s.StartsWith(p, StringComparison.Ordinal)) { s = s[p.Length..]; break; }

            s = s.Trim();
            if (int.TryParse(s, out var num)) s = num.ToString();
            return s;
        }

    }
}