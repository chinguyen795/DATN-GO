using DATN_API.Data;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductVouchersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductVouchersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/productvouchers
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var pvs = await _context.ProductVouchers
                .Include(pv => pv.Product)
                .Include(pv => pv.Voucher)
                .ToListAsync();

            return Ok(pvs);
        }

        // GET: api/productvouchers/product/3
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetByProductId(int productId)
        {
            var pvs = await _context.ProductVouchers
                .Where(pv => pv.ProductId == productId)
                .Include(pv => pv.Voucher)
                .ToListAsync();

            return Ok(pvs);
        }

        // GET: api/productvouchers/voucher/4
        [HttpGet("voucher/{voucherId}")]
        public async Task<IActionResult> GetByVoucherId(int voucherId)
        {
            var pvs = await _context.ProductVouchers
                .Where(pv => pv.VoucherId == voucherId)
                .Include(pv => pv.Product)
                .ToListAsync();

            return Ok(pvs);
        }

        // POST: api/productvouchers
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductVouchers model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var exists = await _context.ProductVouchers
                .AnyAsync(pv => pv.ProductId == model.ProductId && pv.VoucherId == model.VoucherId);

            if (exists)
                return Conflict("Relation already exists.");

            _context.ProductVouchers.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAll), null);
        }

        // DELETE: api/productvouchers/product/3/voucher/4
        [HttpDelete("product/{productId}/voucher/{voucherId}")]
        public async Task<IActionResult> Delete(int productId, int voucherId)
        {
            var pv = await _context.ProductVouchers
                .FirstOrDefaultAsync(p => p.ProductId == productId && p.VoucherId == voucherId);

            if (pv == null) return NotFound();

            _context.ProductVouchers.Remove(pv);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
