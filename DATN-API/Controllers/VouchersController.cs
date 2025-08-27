using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using DATN_API.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VouchersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IVouchersService _vouchersService;

        public VouchersController(ApplicationDbContext context, IVouchersService vouchersService)
        {
            _context = context;
            _vouchersService = vouchersService;
        }

        // GET: api/vouchers
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var vouchers = await _context.Vouchers
                .Include(v => v.ProductVouchers)
                .Include(v => v.Orders)
                .ToListAsync();
            return Ok(vouchers);
        }

        // GET: api/vouchers/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var voucher = await _context.Vouchers
                .Include(v => v.ProductVouchers)
                .Include(v => v.Orders)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (voucher == null) return NotFound();
            return Ok(voucher);
        }

        [HttpGet("shop/{storeId}")]
        public async Task<IActionResult> GetVouchersByStore(int storeId)
        {
            var vouchers = await _context.Vouchers
                .Where(v => v.StoreId == storeId)
                .Include(v => v.ProductVouchers)
                .Include(v => v.Orders)
                .ToListAsync();
            return Ok(vouchers);
        }

        [HttpGet("admin")]
        public async Task<IActionResult> GetAdminVouchers()
        {
            var vouchers = await _context.Vouchers
                .Where(v => v.StoreId == null && v.CreatedByRoleId == 3)
                .Include(v => v.ProductVouchers)
                .Include(v => v.Orders)
                .ToListAsync();
            return Ok(vouchers);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVoucherDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var entity = new Vouchers
            {

                IsPercentage = dto.IsPercentage,
                Reduce = dto.Reduce,
                MaxDiscount = dto.MaxDiscount,
                MinOrder = dto.MinOrder,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Quantity = dto.Quantity,
                CategoryId = dto.CategoryId,    
                StoreId = dto.StoreId,           
                CreatedByUserId = dto.CreatedByUserId,
                CreatedByRoleId = dto.CreatedByRoleId,

                Type = dto.StoreId == null ? VoucherType.Platform : VoucherType.Shop,
                Status = VoucherStatus.Valid
            };

            var validationError = _vouchersService.ValidateForCreateOrUpdate(entity, isCreate: true);
            if (validationError is not null) return BadRequest(validationError);

            _context.Vouchers.Add(entity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateVoucherDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (id != dto.Id) return BadRequest("ID không khớp.");

            var voucher = await _context.Vouchers
                .Include(v => v.ProductVouchers)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (voucher == null) return NotFound();


            voucher.IsPercentage = dto.IsPercentage;
            voucher.Reduce = dto.Reduce;
            voucher.MaxDiscount = dto.MaxDiscount;
            voucher.MinOrder = dto.MinOrder;
            voucher.StartDate = dto.StartDate;
            voucher.EndDate = dto.EndDate;
            voucher.Quantity = dto.Quantity;
            voucher.CategoryId = dto.CategoryId; 
            voucher.CreatedByUserId = dto.CreatedByUserId;
            voucher.CreatedByRoleId = dto.CreatedByRoleId;

            var validationError = _vouchersService.ValidateForCreateOrUpdate(voucher, isCreate: false);
            if (validationError is not null) return BadRequest(validationError);

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null) return NotFound();

            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("bystoreoradmin")]
        public async Task<IActionResult> GetVouchers([FromQuery] int? storeId)
        {
            var vouchers = await _vouchersService.GetVouchersByStoreOrAdminAsync(storeId);
            return Ok(vouchers);
        }

        [HttpPost("apply")]
        public async Task<IActionResult> Apply([FromBody] ApplyVoucherRequestDto req)
        {
            var v = await _context.Vouchers
                .Include(x => x.ProductVouchers)
                .FirstOrDefaultAsync(x => x.Id == req.VoucherId);

            if (v == null) return NotFound("Không tìm thấy voucher.");

            var (discountSub, discountShip, reason) = _vouchersService.ApplyVoucher(
                v,
                req.OrderSubtotal,
                req.ProductIdsInCart,
                req.CategoryIdInCart
            );

            var res = new ApplyVoucherResponseDto
            {
                DiscountOnSubtotal = discountSub,
                DiscountOnShipping = 0m, // luôn 0 vì đã bỏ freeship
                Reason = reason
            };

            if (reason != "OK") return BadRequest(res);
            return Ok(res);
        }


    }
}