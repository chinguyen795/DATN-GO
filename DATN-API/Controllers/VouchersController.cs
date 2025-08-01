using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
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

        // POST: api/vouchers
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Vouchers model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Vouchers.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        // PUT: api/vouchers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Vouchers model)
        {
            if (id != model.Id)
                return BadRequest("ID không khớp");

            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null) return NotFound();

            voucher.Reduce = model.Reduce;
            voucher.MinOrder = model.MinOrder;
            voucher.StartDate = model.StartDate;
            voucher.EndDate = model.EndDate;
            voucher.Status = model.Status;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/vouchers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null) return NotFound();

            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();

            return NoContent();
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
        // GET: api/vouchers/admin
        [HttpGet("admin")]
        public async Task<IActionResult> GetAdminVouchers()
        {
            var vouchers = await _context.Vouchers
                .Where(v => v.StoreId == null)
                .Include(v => v.ProductVouchers)
                .Include(v => v.Orders)
                .ToListAsync();

            return Ok(vouchers);
        }

        // POST: api/vouchers/admin
        [HttpPost("admin")]
        public async Task<IActionResult> CreateAdminVoucher([FromBody] Vouchers model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            model.StoreId = null;

            _context.Vouchers.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        [HttpGet("bystoreoradmin")]
        public async Task<IActionResult> GetVouchers([FromQuery] int? storeId)
        {
            var vouchers = await _vouchersService.GetVouchersByStoreOrAdminAsync(storeId);
            return Ok(vouchers);
        }
    }
}