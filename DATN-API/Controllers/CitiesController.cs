using DATN_API.Data;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CitiesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CitiesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/cities
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var cities = await _context.Cities
                .Include(c => c.Districts)
                .ToListAsync();

            return Ok(cities);
        }

        // GET: api/cities/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var city = await _context.Cities
                .Include(c => c.Districts)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (city == null)
                return NotFound();

            return Ok(city);
        }

        // POST: api/cities
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Cities model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Cities.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        // PUT: api/cities/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Cities model)
        {
            if (id != model.Id)
                return BadRequest("ID không khớp");

            var city = await _context.Cities.FindAsync(id);
            if (city == null)
                return NotFound();

            city.CityName = model.CityName;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/cities/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var city = await _context.Cities.FindAsync(id);
            if (city == null)
                return NotFound();

            _context.Cities.Remove(city);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // API để lấy tỉnh/thành phố từ Mapbox và lưu vào DB
        [HttpPost("import-cities")]
        public async Task<IActionResult> ImportCitiesFromJson()
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
                return BadRequest("Dữ liệu JSON rỗng hoặc không hợp lệ.");

            // 👉 Lấy danh sách tất cả userId hiện có trong hệ thống
            var userIds = await _context.Users.Select(u => u.Id).ToListAsync();
            if (!userIds.Any())
                return BadRequest("Không có user nào trong hệ thống để gán.");

            var random = new Random();
            int count = 0;

            foreach (var dto in cityDtos)
            {
                if (_context.Cities.Any(c => c.CityName == dto.CityName))
                    continue;

                // 👉 Chọn ngẫu nhiên một UserId
                int randomUserId = userIds[random.Next(userIds.Count)];

                var address = new Addresses
                {
                    UserId = randomUserId,
                    Longitude = 0,
                    Latitude = 0,
                    Discription = $"Tự động tạo cho {dto.CityName}",
                    Status = "Active"
                };

                _context.Addresses.Add(address);
                await _context.SaveChangesAsync(); // để lấy được address.Id

                var city = new Cities
                {
                    CityName = dto.CityName,
                    Id = address.Id
                };

                _context.Cities.Add(city);
                count++;
            }

            // ❗Tuỳ logic bạn muốn — giữ lại đoạn này nếu bạn vẫn muốn xóa các địa chỉ vừa tạo
            var autoAddresses = await _context.Addresses
                .Where(a => a.Discription.StartsWith("Tự động tạo cho"))
                .ToListAsync();


            return Ok(new { message = $"Đã lưu {count} tỉnh/thành phố vào hệ thống." });
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


        //Trả danh sách huyện theo tỉnh
        [HttpGet("city/{cityId}")]
        public async Task<IActionResult> GetByCityId(int cityId)
        {
            var districts = await _context.Districts
                .Where(d => d.CityId == cityId)
                .Select(d => new { d.Id, d.DistrictName })
                .ToListAsync();

            return Ok(districts);
        }


    }
}
