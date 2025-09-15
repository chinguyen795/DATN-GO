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

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var vouchers = await _context.Vouchers
                .Include(v => v.Categories)
                .Include(v => v.ProductVouchers)
                .Include(v => v.Orders)
                .ToListAsync();
            return Ok(vouchers);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var voucher = await _context.Vouchers
                .Include(v => v.Categories)
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
                .Include(v => v.Categories)
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
                .Include(v => v.Categories)
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

                ApplyAllCategories = dto.ApplyAllCategories,
                ApplyAllProducts = dto.ApplyAllProducts,

                StoreId = dto.StoreId,
                CreatedByUserId = dto.CreatedByUserId,
                CreatedByRoleId = dto.CreatedByRoleId,

                Type = dto.StoreId == null ? VoucherType.Platform : VoucherType.Shop,
                Status = VoucherStatus.Valid
            };

            // Map N danh mục (many-to-many)
            if (!entity.ApplyAllCategories && dto.CategoryIds?.Any() == true)
            {
                var cats = await _context.Categories
                    .Where(c => dto.CategoryIds.Contains(c.Id))
                    .ToListAsync();

                var missing = dto.CategoryIds.Except(cats.Select(c => c.Id)).ToList();
                if (missing.Count > 0)
                    return BadRequest($"CategoryId không tồn tại: {string.Join(", ", missing)}");

                entity.Categories = cats;
            }

            // Map N sản phẩm (many-to-many qua ProductVouchers)
            if (!entity.ApplyAllProducts && dto.SelectedProductIds?.Any() == true)
            {
                var pids = dto.SelectedProductIds.Distinct().ToList();
                var existPids = await _context.Products
                    .Where(p => pids.Contains(p.Id))
                    .Select(p => p.Id)
                    .ToListAsync();
                var missingPids = pids.Except(existPids).ToList();
                if (missingPids.Count > 0)
                    return BadRequest($"ProductId không tồn tại: {string.Join(", ", missingPids)}");

                entity.ProductVouchers = pids.Select(pid =>
                    new ProductVouchers { ProductId = pid, Voucher = entity }).ToList();
            }

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
                .Include(v => v.Categories)
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

            voucher.ApplyAllCategories = dto.ApplyAllCategories;
            voucher.ApplyAllProducts = dto.ApplyAllProducts;

            voucher.CreatedByUserId = dto.CreatedByUserId;
            voucher.CreatedByRoleId = dto.CreatedByRoleId;

            // Categories
            voucher.Categories ??= new List<Categories>();
            voucher.Categories.Clear();
            if (!voucher.ApplyAllCategories && dto.CategoryIds?.Any() == true)
            {
                var cats = await _context.Categories
                    .Where(c => dto.CategoryIds.Contains(c.Id))
                    .ToListAsync();

                var missing = dto.CategoryIds.Except(cats.Select(c => c.Id)).ToList();
                if (missing.Count > 0)
                    return BadRequest($"CategoryId không tồn tại: {string.Join(", ", missing)}");

                foreach (var c in cats) voucher.Categories.Add(c);
            }

            // Products
            if (!voucher.ApplyAllProducts)
            {
                _context.ProductVouchers.RemoveRange(voucher.ProductVouchers ?? Enumerable.Empty<ProductVouchers>());
                voucher.ProductVouchers = new List<ProductVouchers>();

                if (dto.SelectedProductIds?.Any() == true)
                {
                    var pids = dto.SelectedProductIds.Distinct().ToList();
                    var existPids = await _context.Products
                        .Where(p => pids.Contains(p.Id))
                        .Select(p => p.Id)
                        .ToListAsync();
                    var missingPids = pids.Except(existPids).ToList();
                    if (missingPids.Count > 0)
                        return BadRequest($"ProductId không tồn tại: {string.Join(", ", missingPids)}");

                    voucher.ProductVouchers = pids.Select(pid =>
                        new ProductVouchers { ProductId = pid, VoucherId = voucher.Id }).ToList();
                }
            }
            else
            {
                if (voucher.ProductVouchers?.Any() == true)
                    _context.ProductVouchers.RemoveRange(voucher.ProductVouchers);
            }

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
                .Include(x => x.Categories)
                .Include(x => x.ProductVouchers)
                .FirstOrDefaultAsync(x => x.Id == req.VoucherId);

            if (v == null) return NotFound("Không tìm thấy voucher.");

            // NEW: chặn nếu user đã dùng voucher này
            var used = await _context.UserVouchers
                .AnyAsync(x => x.UserId == req.UserId && x.VoucherId == req.VoucherId && x.IsUsed);
            if (used)
                return BadRequest(new ApplyVoucherResponseDto
                {
                    DiscountOnSubtotal = 0,
                    DiscountOnShipping = 0,
                    Reason = "Bạn đã sử dụng voucher này rồi."
                });

            // Suy categoryIdsInCart nếu client không gửi
            IEnumerable<int>? categoryIdsInCart = req.CategoryIdsInCart;
            if (categoryIdsInCart == null || !categoryIdsInCart.Any())
            {
                // Products.CategoryId ở trên là int (không nullable)
                categoryIdsInCart = await _context.Products
                    .Where(p => req.ProductIdsInCart.Contains(p.Id))
                    .Select(p => p.CategoryId)
                    .Distinct()
                    .ToListAsync();
            }

            var (discountSub, discountShip, reason) = _vouchersService.ApplyVoucher(
                v, req.OrderSubtotal, req.ProductIdsInCart, categoryIdsInCart);

            var res = new ApplyVoucherResponseDto
            {
                DiscountOnSubtotal = discountSub,
                DiscountOnShipping = discountShip,
                Reason = reason
            };

            if (reason != "OK") return BadRequest(res);
            return Ok(res);
        }
    }
}