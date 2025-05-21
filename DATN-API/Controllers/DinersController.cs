using DATN_API.Data;
using DATN_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DinersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DinersController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize]
        [HttpGet("ByUser")]
        public async Task<IActionResult> Get()
        {
            if (!Int32.TryParse(HttpContext?.User?.Identity?.Name, out var uId))
                return Unauthorized("Không tìm thấy thông tin người dùng");
            var user = await _context.Users.FindAsync(uId);
            if (user == null)
                return NotFound("Không tìm thấy người dùng");

            var diners = await _context.Diners.FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (diners == null)
                return NotFound("Không tìm thấy thông tin Diner của người dùng này");
            return Ok(diners);
        }

        // GET: api/diners
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var diners = await _context.Diners
                .Include(d => d.User)
                .Include(d => d.ReportsReceived)
                .Include(d => d.Products)
                .Include(d => d.ShippingMethods)
                .ToListAsync();

            return Ok(diners);
        }

        // GET: api/diners/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var diner = await _context.Diners
                .Include(d => d.User)
                .Include(d => d.ReportsReceived)
                .Include(d => d.Products)
                .Include(d => d.ShippingMethods)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (diner == null) return NotFound();

            return Ok(diner);
        }

        // POST: api/diners
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] DinnerModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!Int32.TryParse(HttpContext?.User?.Identity?.Name, out var uId))
                return Unauthorized("Không tìm thấy thông tin người dùng");

            var user = await _context.Users.FindAsync(uId);
            if (user == null)
                return NotFound("Không tìm thấy người dùng");

            //Cái này là đảm bảo rằng 1 user chỉ có 1 diner thôi
            var existed = await _context.Diners.FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (existed != null)
            {
                model.Id = existed.Id;
                return await Update(existed.Id, model);
            }

            var diner = new Diners
            {
                DinerName = model.DinerName,
                DinerAddress = model.DinerAddress,
                Longitude = model.Longitude,
                Latitude = model.Latitude,
                Avatar = model.Avatar,
                Status = model.Status,
                CoverPhoto = model.CoverPhoto,
                CreateAt = DateTime.Now,
                OpenHouse = model.OpenHouse,
                OpenMinute = model.OpenMinute,
                CloseHouse = model.CloseHouse,
                CloseMinute = model.CloseMinute,
                UserId = user.Id
            };
            _context.Diners.Add(diner);
            // Update role của người dùng thành Seller khi đăng ký diner
            if (user.Role?.RoleName != "Seller")
            {
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Seller");
                user.RoleId = role?.Id ?? user.RoleId; // Nếu lỡ role Seller bị xóa thì đảm bảo rằng user.RoleId không bị null
            }
            if (!string.IsNullOrEmpty(model.PhoneNumber)) user.PhoneNumber = model.PhoneNumber;
            if (!string.IsNullOrEmpty(model.Email)) user.Email = model.Email;

            if (string.IsNullOrEmpty(user.PhoneNumber)) user.PhoneNumber = string.Empty;
            if (string.IsNullOrEmpty(user.Email)) user.Email = string.Empty;
            await _context.SaveChangesAsync();

            return Ok(diner);
        }

        // PUT: api/diners/5
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] DinnerModel model)
        {
            if (id != model.Id)
                return BadRequest("ID không khớp");

            if (!Int32.TryParse(HttpContext?.User?.Identity?.Name, out var uId))
                return Unauthorized("Không tìm thấy thông tin người dùng");

            var user = await _context.Users.FindAsync(uId);
            if (user == null)
                return NotFound("Không tìm thấy người dùng");
            var diner = await _context.Diners.FindAsync(id);
            if (diner == null)
                return NotFound();
            if (diner.UserId != user.Id)
                return Unauthorized("Người dùng không có quyền sửa thông tin Diner này");

            diner.DinerName = model.DinerName;
            diner.DinerAddress = model.DinerAddress;
            diner.Longitude = model.Longitude;
            diner.Latitude = model.Latitude;
            diner.Status = model.Status;
            if (!string.IsNullOrEmpty(model.Avatar) && model.Avatar != "no")
                diner.Avatar = model.Avatar;
            if (!string.IsNullOrEmpty(model.CoverPhoto) && model.CoverPhoto != "no")
                diner.CoverPhoto = model.CoverPhoto;
            diner.OpenHouse = model.OpenHouse;
            diner.OpenMinute = model.OpenMinute;
            diner.CloseHouse = model.CloseHouse;
            diner.CloseMinute = model.CloseMinute;
            //// Không update CreateAt để giữ thời gian tạo ban đầu
            if (!string.IsNullOrEmpty(model.PhoneNumber)) user.PhoneNumber = model.PhoneNumber;
            if (!string.IsNullOrEmpty(model.Email)) user.Email = model.Email;
            await _context.SaveChangesAsync();
            return Ok(diner);
        }

        // DELETE: api/diners/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var diner = await _context.Diners.FindAsync(id);
            if (diner == null) return NotFound();

            _context.Diners.Remove(diner);
            await _context.SaveChangesAsync();

            return NoContent();
        }

[HttpPut("ChangeImage")]
public async Task<IActionResult> ChangeImage([FromBody] ChangeImageModel model)
{
    if (!Int32.TryParse(HttpContext?.User?.Identity?.Name, out var uId))
        return Unauthorized("Không tìm thấy thông tin người dùng");

    var user = await _context.Users.FindAsync(uId);
    if (user == null)
        return NotFound("Không tìm thấy người dùng");

    var diner = await _context.Diners.FirstOrDefaultAsync(x => x.UserId == user.Id);
    if (diner == null)
        return NotFound("Không tìm thấy Diner");

    // ✅ Lưu URL ảnh vào đúng cột
    if (model.IsAvatar)
        diner.Avatar = model.Data;
    else
        diner.CoverPhoto = model.Data;

    await _context.SaveChangesAsync();

    return Ok(new { url = model.Data }); // Gửi lại URL cho FE cập nhật ảnh ngay
}

    }

    public class DinnerModel
    {
        public int? Id { get; set; }
        [MaxLength(50)]
        public string DinerName { get; set; }

        [MaxLength(50)]
        public string DinerAddress { get; set; }

        public float Longitude { get; set; }
        public float Latitude { get; set; }

        public string Avatar { get; set; }

        [MaxLength(50)]
        public string Status { get; set; }

        public string CoverPhoto { get; set; }
        [Required]
        [Range(0, 23)]
        public int OpenHouse { get; set; } = 8;
        [Range(0, 59)]
        public int OpenMinute { get; set; } = 0;
        [Required]
        [Range(0, 23)]
        public int CloseHouse { get; set; } = 22;
        [Range(0, 59)]
        public int CloseMinute { get; set; } = 0;

        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
    }

    public class ChangeImageModel
    {
        public bool IsAvatar { get; set; }
        public string Data { get; set; }
    }
}