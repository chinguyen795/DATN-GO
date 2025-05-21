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

        // DELETE: api/districts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var district = await _context.Districts.FindAsync(id);
            if (district == null)
                return NotFound();

            _context.Districts.Remove(district);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("import-districts")]
        public async Task<IActionResult> ImportDistrictsFromJson()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "tree_mien_nam.json");

            if (!System.IO.File.Exists(filePath))
                return NotFound("File tree_mien_nam.json không tồn tại.");

            var json = await System.IO.File.ReadAllTextAsync(filePath);

            List<CityDto>? cityDtos;
            try
            {
                cityDtos = JsonSerializer.Deserialize<List<CityDto>>(json);
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi khi đọc JSON: {ex.Message}");
            }

            if (cityDtos == null || !cityDtos.Any())
                return BadRequest("Dữ liệu JSON không hợp lệ hoặc rỗng.");

            int count = 0;

            foreach (var cityDto in cityDtos)
            {
                var city = await _context.Cities.FirstOrDefaultAsync(c => c.CityName == cityDto.CityName);
                if (city == null)
                    continue;

                foreach (var districtDto in cityDto.Districts)
                {
                    // Tránh trùng tên quận trong cùng thành phố
                    bool exists = await _context.Districts.AnyAsync(d =>
                        d.DistrictName == districtDto.DistrictName &&
                        d.CityId == city.Id);

                    if (exists) continue;

                    var district = new Districts
                    {
                        CityId = city.Id,
                        DistrictName = districtDto.DistrictName
                    };

                    _context.Districts.Add(district);
                    count++;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã lưu {count} quận/huyện vào DB." });
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
