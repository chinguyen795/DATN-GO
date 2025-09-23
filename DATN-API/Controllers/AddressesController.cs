using DATN_API.Data;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AddressesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AddressesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/addresses
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var addresses = await _context.Addresses
                .Include(a => a.User)
                .ToListAsync();
            return Ok(addresses);
        }

        // GET: api/addresses/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var address = await _context.Addresses
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (address == null) return NotFound();

            return Ok(address);
        }

        // POST: api/addresses

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Addresses model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            model.City = null;
            _context.Addresses.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }


        // PUT: api/addresses/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Addresses model)
        {
            if (id != model.Id) return BadRequest("ID không khớp");

            // Log payload để debug
            Console.WriteLine($"[Addresses PUT] id={id} payload={System.Text.Json.JsonSerializer.Serialize(model)}");

            var address = await _context.Addresses.FindAsync(id);
            if (address == null) return NotFound();

            bool changed = false;

            // Helpers
            void SetIfChanged<T>(Func<T> cur, Action<T> set, T val, string name)
            {
                if (!EqualityComparer<T>.Default.Equals(cur(), val))
                {
                    set(val);
                    changed = true;
                    Console.WriteLine($"  - Set {name} -> {val}");
                }
            }

            // Các field cơ bản
            if (!string.IsNullOrWhiteSpace(model.Name)) SetIfChanged(() => address.Name, v => address.Name = v, model.Name, "Name");
            if (!string.IsNullOrWhiteSpace(model.Phone)) SetIfChanged(() => address.Phone, v => address.Phone = v, model.Phone, "Phone");
            if (!string.IsNullOrWhiteSpace(model.Description)) SetIfChanged(() => address.Description, v => address.Description = v, model.Description, "Description");
            if (model.Latitude != 0) SetIfChanged(() => address.Latitude, v => address.Latitude = v, model.Latitude, "Latitude");
            if (model.Longitude != 0) SetIfChanged(() => address.Longitude, v => address.Longitude = v, model.Longitude, "Longitude");
            SetIfChanged(() => address.Status, v => address.Status = v, model.Status, "Status");
            if (model.UserId != 0) SetIfChanged(() => address.UserId, v => address.UserId = v, model.UserId, "UserId");

            // --- District/Ward: chuẩn hóa giá trị (0 => null) ---
            int? newDistrictId = (model.DistrictId.HasValue && model.DistrictId.Value > 0)
                                 ? model.DistrictId.Value
                                 : (int?)null;
            int? newWardId = (model.WardId.HasValue && model.WardId.Value > 0)
                                 ? model.WardId.Value
                                 : (int?)null;

            // --- Validate quan hệ (tùy ràng buộc của bạn) ---
            if (newDistrictId.HasValue)
            {
                var districtValid = await _context.Districts
                    .AnyAsync(d => d.Id == newDistrictId.Value && d.CityId == address.Id /* City shared PK = Address.Id */);
                if (!districtValid)
                    return BadRequest("DistrictId không thuộc City của địa chỉ.");
            }

            if (newWardId.HasValue)
            {
                // Nếu gửi WardId mà DistrictId null thì lấy DistrictId cũ để check
                var districtIdToCheck = newDistrictId ?? address.DistrictId;
                if (!districtIdToCheck.HasValue)
                    return BadRequest("Thiếu DistrictId để kiểm tra WardId.");

                var wardValid = await _context.Wards
                    .AnyAsync(w => w.Id == newWardId.Value && w.DistrictId == districtIdToCheck.Value);
                if (!wardValid)
                    return BadRequest("WardId không thuộc District đã chọn.");
            }

            // --- Gán District/Ward ---
            SetIfChanged(() => address.DistrictId, v => address.DistrictId = v, newDistrictId, "DistrictId");
            SetIfChanged(() => address.WardId, v => address.WardId = v, newWardId, "WardId");

            if (changed)
            {
                address.UpdateAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            // Trả về object để dễ debug phía client (tạm thời; sau ổn có thể trả NoContent)
            return Ok(new
            {
                address.Id,
                address.DistrictId,
                address.WardId,
                address.Name,
                address.Phone,
                address.Status,
                address.Latitude,
                address.Longitude
            });
        }






        // DELETE: api/addresses/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            // dùng transaction để all-or-nothing
            await using var tx = await _context.Database.BeginTransactionAsync();

            var address = await _context.Addresses.FindAsync(id);
            if (address == null)
                return NotFound();

            // (Optional) nếu có đơn hàng trỏ tới Address thì nên chặn xoá
            // nếu FK Orders.AddressId là NOT NULL, xoá sẽ fail
            //var hasOrders = await _context.Orders.AnyAsync(o => o.Id == id);
            //if (hasOrders)
            //    return Conflict("Địa chỉ đang được dùng trong đơn hàng, không thể xoá.");

            // 1) Cắt FK từ Address tới Ward/District trước
            address.WardId = null;
            address.DistrictId = null;
            address.UpdateAt = DateTime.Now;
            _context.Addresses.Update(address);
            await _context.SaveChangesAsync();

            // 2) Xoá cây location của City (City.Id == Address.Id - shared PK)
            //    Lấy districts theo CityId = id
            var districts = await _context.Districts
                .Where(d => d.CityId == id)
                .ToListAsync();

            var districtIds = districts.Select(d => d.Id).ToList();

            //    Lấy wards theo các DistrictId
            var wards = await _context.Wards
                .Where(w => districtIds.Contains(w.DistrictId))
                .ToListAsync();

            if (wards.Count > 0)
            {
                _context.Wards.RemoveRange(wards);
                await _context.SaveChangesAsync();
            }

            if (districts.Count > 0)
            {
                _context.Districts.RemoveRange(districts);
                await _context.SaveChangesAsync();
            }

            //    Xoá City (child của Address theo FK_Cities_Addresses_Id)
            var city = await _context.Cities.FindAsync(id);
            if (city != null)
            {
                _context.Cities.Remove(city);
                await _context.SaveChangesAsync();
            }

            // 3) Cuối cùng xoá Address (lúc này không còn child nào tham chiếu nữa)
            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();

            await tx.CommitAsync();
            return NoContent();
        }




        [HttpGet("full-address/{addressId}")]
        public async Task<IActionResult> GetFullAddress(int addressId)
        {
            // Lấy address + City (City 1-1 theo Address.Id)
            var address = await _context.Addresses
                .AsNoTracking()
                .Include(a => a.City)
                .FirstOrDefaultAsync(a => a.Id == addressId);

            if (address == null)
                return NotFound("Không tìm thấy địa chỉ.");

            // ƯU TIÊN: lấy theo DistrictId/WardId đã lưu trong Address (nếu có)
            Districts? district = null;
            Wards? ward = null;

            // District
            if (address.DistrictId.HasValue && address.DistrictId.Value > 0)
            {
                district = await _context.Districts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == address.DistrictId.Value);
            }
            else if (address.City != null)
            {
                // Fallback: nếu chưa có DistrictId thì lấy district bất kỳ theo City (tránh null)
                district = await _context.Districts
                    .AsNoTracking()
                    .Where(d => d.CityId == address.City.Id)
                    .OrderBy(d => d.Id)
                    .FirstOrDefaultAsync();
            }

            // Ward
            if (address.WardId.HasValue && address.WardId.Value > 0)
            {
                ward = await _context.Wards
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == address.WardId.Value);
            }
            else if (district != null)
            {
                // Fallback: nếu chưa có WardId thì lấy ward bất kỳ theo District
                ward = await _context.Wards
                    .AsNoTracking()
                    .Where(w => w.DistrictId == district.Id)
                    .OrderBy(w => w.Id)
                    .FirstOrDefaultAsync();
            }

            // Build chuỗi hiển thị
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(address.Name) || !string.IsNullOrWhiteSpace(address.Phone))
                parts.Add($"{address.Name} - {address.Phone}");

            if (!string.IsNullOrWhiteSpace(address.Description))
                parts.Add(address.Description);

            if (!string.IsNullOrWhiteSpace(ward?.WardName))
                parts.Add(ward!.WardName);

            if (!string.IsNullOrWhiteSpace(district?.DistrictName))
                parts.Add(district!.DistrictName);

            if (!string.IsNullOrWhiteSpace(address.City?.CityName))
                parts.Add(address.City!.CityName);

            var fullAddress = string.Join(", ", parts);

            return Ok(new
            {
                address.Id,
                FullAddress = fullAddress
            });
        }


    }
}