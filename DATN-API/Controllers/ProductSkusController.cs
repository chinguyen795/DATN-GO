using DATN_API.Data;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductSkusController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductSkusController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/productskus
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var skus = await _context.ProductSkus
                .Include(ps => ps.Product)
                .Include(ps => ps.SkusValues)
                .ToListAsync();

            return Ok(skus);
        }

        // GET: api/productskus/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var sku = await _context.ProductSkus
                .Include(ps => ps.Product)
                .Include(ps => ps.SkusValues)
                .FirstOrDefaultAsync(ps => ps.Id == id);

            if (sku == null) return NotFound();

            return Ok(sku);
        }

        // GET: api/productskus/product/3
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetByProductId(int productId)
        {
            var skus = await _context.ProductSkus
                .Where(ps => ps.ProductId == productId)
                .Include(ps => ps.SkusValues)
                .ToListAsync();

            return Ok(skus);
        }

        // POST: api/productskus
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductSkus model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.ProductSkus.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        // PUT: api/productskus/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductSkus model)
        {
            if (id != model.Id)
                return BadRequest("ID không khớp");

            var sku = await _context.ProductSkus.FindAsync(id);
            if (sku == null) return NotFound();

            sku.ProductId = model.ProductId;
            sku.Sku = model.Sku;
            sku.Price = model.Price;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/productskus/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var sku = await _context.ProductSkus.FindAsync(id);
            if (sku == null) return NotFound();

            _context.ProductSkus.Remove(sku);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
