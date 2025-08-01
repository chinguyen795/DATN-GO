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
            if (id != model.Id)
                return BadRequest("ID không khớp");

            var address = await _context.Addresses.FindAsync(id);
            if (address == null) return NotFound();

            bool isUpdated = false;

            if (!string.IsNullOrWhiteSpace(model.Name) && model.Name != address.Name)
            {
                address.Name = model.Name;
                isUpdated = true;
            }

            if (!string.IsNullOrWhiteSpace(model.Phone) && model.Phone != address.Phone)
            {
                address.Phone = model.Phone;
                isUpdated = true;
            }

            if (!string.IsNullOrWhiteSpace(model.Description) && model.Description != address.Description)
            {
                address.Description = model.Description;
                isUpdated = true;
            }

            if (model.Latitude != 0 && model.Latitude != address.Latitude)
            {
                address.Latitude = model.Latitude;
                isUpdated = true;
            }

            if (model.Longitude != 0 && model.Longitude != address.Longitude)
            {
                address.Longitude = model.Longitude;
                isUpdated = true;
            }

            if (model.Status != address.Status)
            {
                address.Status = model.Status;
                isUpdated = true;
            }

            if (model.UserId != 0 && model.UserId != address.UserId)
            {
                address.UserId = model.UserId;
                isUpdated = true;
            }

            if (isUpdated)
            {
                address.UpdateAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }



        // DELETE: api/addresses/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var address = await _context.Addresses.FindAsync(id);
            if (address == null)
                return NotFound();

            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();

            return NoContent();
        }



        [HttpGet("full-address/{addressId}")]
        public async Task<IActionResult> GetFullAddress(int addressId)
        {
            var address = await _context.Addresses
                .Include(a => a.City)
                .FirstOrDefaultAsync(a => a.Id == addressId);

            if (address == null)
                return NotFound("Không tìm thấy địa chỉ.");

            var district = await _context.Districts
                .FirstOrDefaultAsync(d => d.CityId == address.City.Id);

            var ward = district != null
                ? await _context.Wards.FirstOrDefaultAsync(w => w.DistrictId == district.Id)
                : null;

            var fullAddress = string.Join(", ", new[]
            {
        $"{address.Name} - {address.Phone}",   
        address.Description,                      
        ward?.WardName,
        district?.DistrictName,
        address.City?.CityName
    }.Where(s => !string.IsNullOrWhiteSpace(s)));

            return Ok(new
            {
                address.Id,
                FullAddress = fullAddress
            });
        }

    }
}