using DATN_API.Data;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/products
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Diner)
                .Include(p => p.Options)
                .Include(p => p.ProductSkus)
                .Include(p => p.OptionValues)
                .Include(p => p.ProductVouchers)
                .Include(p => p.Carts)
                .Include(p => p.Reviews)
                .Include(p => p.OrderDetails)
                .ToListAsync();

            return Ok(products);
        }

        // GET: api/products/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Diner)
                .Include(p => p.Options)
                .Include(p => p.ProductSkus)
                .Include(p => p.OptionValues)
                .Include(p => p.ProductVouchers)
                .Include(p => p.Carts)
                .Include(p => p.Reviews)
                .Include(p => p.OrderDetails)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            return Ok(product);
        }

        // GET: api/products/category/3
        [HttpGet("category/{categoryId}")]
        public async Task<IActionResult> GetByCategory(int categoryId)
        {
            var products = await _context.Products
                .Where(p => p.CategoryId == categoryId)
                .Include(p => p.Category)
                .Include(p => p.Diner)
                .ToListAsync();

            return Ok(products);
        }

        // POST: api/products
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Products model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            model.CreateAt = DateTime.UtcNow;

            _context.Products.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        // PUT: api/products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Products model)
        {
            if (id != model.Id)
                return BadRequest("ID không khớp");

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.CategoryId = model.CategoryId;
            product.DinerId = model.DinerId;
            product.ProductName = model.ProductName;
            product.Price = model.Price;
            product.Discription = model.Discription;
            product.MainImage = model.MainImage;
            product.Status = model.Status;
            product.Quantity = model.Quantity;
            product.Views = model.Views;
            // Không update CreateAt để giữ ngày tạo gốc

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
