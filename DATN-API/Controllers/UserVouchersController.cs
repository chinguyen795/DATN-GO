using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DATN_API.Data;
using DATN_API.Models;

namespace DATN_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserVouchersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserVouchersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/UserVouchers/user/5
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserVouchers(int userId)
        {
            try
            {
                var userVouchers = await _context.UserVouchers
                    .Where(uv => uv.UserId == userId && !uv.IsUsed)
                    .Include(uv => uv.Voucher)
                    .ThenInclude(v => v.Store)
                    .ToListAsync();

                if (!userVouchers.Any())
                {
                    return Ok(new List<object>());
                }

                var result = userVouchers.Select(uv => new
                {
                    id = uv.Id,
                    userId = uv.UserId,
                    voucherId = uv.VoucherId,
                    savedAt = uv.SavedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                    isUsed = uv.IsUsed,
                    voucher = new
                    {
                        id = uv.Voucher.Id,
                        reduce = uv.Voucher.Reduce,
                        type = uv.Voucher.Type.ToString(),
                        minOrder = uv.Voucher.MinOrder,
                        startDate = uv.Voucher.StartDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                        endDate = uv.Voucher.EndDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                        status = uv.Voucher.Status.ToString(),
                        storeId = uv.Voucher.StoreId,
                        storeName = uv.Voucher.Store?.Name ?? "Sàn TMĐT"
                    }
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving user vouchers", error = ex.Message });
            }
        }

        // POST: api/UserVouchers/save
        [HttpPost("save")]
        public async Task<IActionResult> SaveVoucher([FromBody] SaveVoucherRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Kiểm tra xem voucher đã được lưu chưa
            var existingUserVoucher = await _context.UserVouchers
                .FirstOrDefaultAsync(uv => uv.UserId == request.UserId && uv.VoucherId == request.VoucherId);

            if (existingUserVoucher != null)
            {
                return BadRequest(new { message = "Voucher đã được lưu trước đó" });
            }

            // Kiểm tra voucher có tồn tại không
            var voucher = await _context.Vouchers.FindAsync(request.VoucherId);
            if (voucher == null)
            {
                return NotFound(new { message = "Voucher không tồn tại" });
            }

            // Kiểm tra voucher còn hạn không
            if (voucher.EndDate < DateTime.Now)
            {
                return BadRequest(new { message = "Voucher đã hết hạn" });
            }

            var userVoucher = new UserVouchers
            {
                UserId = request.UserId,
                VoucherId = request.VoucherId,
                SavedAt = DateTime.Now,
                IsUsed = false
            };

            _context.UserVouchers.Add(userVoucher);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Lưu voucher thành công", id = userVoucher.Id });
        }

        // DELETE: api/UserVouchers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserVoucher(int id)
        {
            var userVoucher = await _context.UserVouchers.FindAsync(id);
            if (userVoucher == null)
            {
                return NotFound();
            }

            _context.UserVouchers.Remove(userVoucher);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa voucher thành công" });
        }

        // PUT: api/UserVouchers/use/5
        [HttpPut("use/{id}")]
        public async Task<IActionResult> UseVoucher(int id)
        {
            var userVoucher = await _context.UserVouchers.FindAsync(id);
            if (userVoucher == null)
            {
                return NotFound();
            }

            userVoucher.IsUsed = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã sử dụng voucher" });
        }

        // GET: api/UserVouchers/check/{userId}/{voucherId}
        [HttpGet("check/{userId}/{voucherId}")]
        public async Task<IActionResult> CheckVoucherSaved(int userId, int voucherId)
        {
            var exists = await _context.UserVouchers
                .AnyAsync(uv => uv.UserId == userId && uv.VoucherId == voucherId);

            return Ok(new { isSaved = exists });
        }
    }

    public class SaveVoucherRequest
    {
        public int UserId { get; set; }
        public int VoucherId { get; set; }
    }
}