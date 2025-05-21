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

        [HttpPost("import-wards")]
        public async Task<IActionResult> ImportWardsFromJson()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "tree_mien_nam.json");

            if (!System.IO.File.Exists(filePath))
                return NotFound("File tree_mien_nam.json không tồn tại.");

            var json = await System.IO.File.ReadAllTextAsync(filePath);

            List<CityDto>? cityDtos;
            try
            {
                cityDtos = System.Text.Json.JsonSerializer.Deserialize<List<CityDto>>(json);
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi khi đọc JSON: {ex.Message}");
            }

            if (cityDtos == null || !cityDtos.Any())
                return BadRequest("Dữ liệu JSON rỗng hoặc không hợp lệ.");

            int count = 0;

            foreach (var cityDto in cityDtos)
            {
                var city = await _context.Cities.FirstOrDefaultAsync(c => c.CityName == cityDto.CityName);
                if (city == null) continue;

                foreach (var districtDto in cityDto.Districts)
                {
                    var district = await _context.Districts
                        .FirstOrDefaultAsync(d => d.DistrictName == districtDto.DistrictName && d.CityId == city.Id);
                    if (district == null) continue;

                    foreach (var wardDto in districtDto.Wards)
                    {
                        bool exists = await _context.Wards.AnyAsync(w =>
                            w.WardName == wardDto.WardName && w.DistrictId == district.Id);

                        if (exists) continue;

                        var ward = new Wards
                        {
                            DistrictId = district.Id,
                            WardName = wardDto.WardName
                        };

                        _context.Wards.Add(ward);
                        count++;
                    }
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã lưu {count} phường/xã vào DB." });
        }

        public class CityDto
        {
            public string CityName { get; set; } = string.Empty;
            public List<DistrictDto> Districts { get; set; } = new();
        }

        public class DistrictDto
        {
            public string DistrictName { get; set; } = string.Empty;
            public List<WardDto> Wards { get; set; } = new();
        }

        public class WardDto
        {
            public string WardName { get; set; } = string.Empty;
        }


    }
}
