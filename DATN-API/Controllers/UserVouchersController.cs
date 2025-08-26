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
        public UserVouchersController(ApplicationDbContext context) => _context = context;

        // GET: api/UserVouchers/user/5?scope=all|platform|shop
        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetUserVouchers(int userId, [FromQuery] string scope = "all")
        {
            try
            {
                var q = _context.UserVouchers
                    .Where(uv => uv.UserId == userId && !uv.IsUsed)
                    .Include(uv => uv.Voucher)
                    .ThenInclude(v => v.Store) // để lấy storeName
                    .AsQueryable();

                scope = (scope ?? "all").Trim().ToLowerInvariant();
                if (scope == "platform" || scope == "sàn" || scope == "san")
                    q = q.Where(uv => uv.Voucher.StoreId == null && uv.Voucher.CreatedByRoleId == 3);
                else if (scope == "shop" || scope == "store")
                    q = q.Where(uv => uv.Voucher.StoreId != null && uv.Voucher.CreatedByRoleId == 2);
                // else: all

                var list = await q.ToListAsync();
                var result = list.Select(uv => new
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
                        type = uv.Voucher.Type.ToString(), // Platform / Shop
                        minOrder = uv.Voucher.MinOrder,
                        startDate = uv.Voucher.StartDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                        endDate = uv.Voucher.EndDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                        status = uv.Voucher.Status.ToString(),
                        storeId = uv.Voucher.StoreId,
                        storeName = uv.Voucher.Store?.Name ?? "Sàn TMĐT"
                    }
                }).ToList();

                return Ok(result); // luôn Ok([]) nếu rỗng
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, message = "Error retrieving user vouchers", error = ex.Message });
            }
        }

        // POST: api/UserVouchers/save
        [HttpPost("save")]
        public async Task<IActionResult> SaveVoucher([FromBody] SaveVoucherRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(new { ok = false, message = "Invalid model" });
            if (request.UserId <= 0 || request.VoucherId <= 0) return BadRequest(new { ok = false, message = "Thiếu userId/voucherId" });

            var existed = await _context.UserVouchers
                .AnyAsync(uv => uv.UserId == request.UserId && uv.VoucherId == request.VoucherId);
            if (existed) return BadRequest(new { ok = false, message = "Voucher đã được lưu trước đó" });

            var v = await _context.Vouchers.FindAsync(request.VoucherId);
            if (v == null) return NotFound(new { ok = false, message = "Voucher không tồn tại" });

            // Cho phép lưu cả sàn (roleId=3) lẫn shop (roleId=2)
            var isPlatform = v.StoreId == null && v.CreatedByRoleId == 3;
            var isShop = v.StoreId != null && v.CreatedByRoleId == 2;
            if (!isPlatform && !isShop)
                return BadRequest(new { ok = false, message = "Voucher không hợp lệ (không thuộc sàn hay shop hợp lệ)." });

            var now = DateTime.UtcNow;
            if (now > v.EndDate) return BadRequest(new { ok = false, message = "Voucher đã hết hạn" });
            if (v.UsedCount >= v.Quantity) return BadRequest(new { ok = false, message = "Voucher đã hết lượt" });

            var userVoucher = new UserVouchers
            {
                UserId = request.UserId,
                VoucherId = request.VoucherId,
                SavedAt = DateTime.UtcNow,
                IsUsed = false
            };

            _context.UserVouchers.Add(userVoucher);
            await _context.SaveChangesAsync();

            return Ok(new { ok = true, message = "Lưu voucher thành công", id = userVoucher.Id });
        }

        // DELETE: api/UserVouchers/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteUserVoucher(int id)
        {
            var userVoucher = await _context.UserVouchers.FindAsync(id);
            if (userVoucher == null) return NotFound(new { ok = false, message = "Không tìm thấy" });

            _context.UserVouchers.Remove(userVoucher);
            await _context.SaveChangesAsync();
            return Ok(new { ok = true, message = "Xóa voucher thành công" });
        }

        // PUT: api/UserVouchers/use/5
        [HttpPut("use/{id:int}")]
        public async Task<IActionResult> UseVoucher(int id)
        {
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var uv = await _context.UserVouchers.Include(x => x.Voucher).FirstOrDefaultAsync(x => x.Id == id);
                if (uv == null) return NotFound(new { ok = false, message = "Không tìm thấy user voucher" });
                if (uv.IsUsed) return BadRequest(new { ok = false, message = "Voucher đã được sử dụng" });

                var v = uv.Voucher!;
                var now = DateTime.UtcNow;
                if (now < v.StartDate || now > v.EndDate)
                    return BadRequest(new { ok = false, message = "Voucher hết hạn hoặc chưa bắt đầu" });
                if (v.UsedCount >= v.Quantity)
                    return BadRequest(new { ok = false, message = "Voucher đã hết lượt" });

                // Cho phép dùng cả sàn (roleId=3) & shop (roleId=2)
                var isPlatform = v.StoreId == null && v.CreatedByRoleId == 3;
                var isShop = v.StoreId != null && v.CreatedByRoleId == 2;
                if (!isPlatform && !isShop)
                    return BadRequest(new { ok = false, message = "Voucher không hợp lệ (không thuộc sàn hay shop hợp lệ)." });

                v.UsedCount += 1;
                uv.IsUsed = true;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new { ok = true, message = "Đã sử dụng voucher", voucherId = v.Id });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { ok = false, message = "Lỗi khi sử dụng voucher", error = ex.Message });
            }
        }

        // GET: api/UserVouchers/check/{userId}/{voucherId}
        [HttpGet("check/{userId:int}/{voucherId:int}")]
        public async Task<IActionResult> CheckVoucherSaved(int userId, int voucherId)
        {
            var exists = await _context.UserVouchers.AnyAsync(uv => uv.UserId == userId && uv.VoucherId == voucherId);
            return Ok(new { ok = true, isSaved = exists });
        }
    }

    public class SaveVoucherRequest
    {
        public int UserId { get; set; }
        public int VoucherId { get; set; }
    }
}
