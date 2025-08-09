using DATN_API.Data;
using DATN_API.Models;
using DATN_API.Services.Interfaces;
using DATN_API.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductsService _service;
        
        public ProductsController(IProductsService service)
        {
            _service = service;
        }

        // GET: api/products
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _service.GetAllAsync();
            return Ok(products);
        }

        // GET: api/products/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _service.GetByIdAsync(id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        // POST: api/products
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Products model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var created = await _service.CreateAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Products model)
        {
            if (!await _service.UpdateAsync(id, model))
                return BadRequest("ID không khớp hoặc không tìm thấy sản phẩm");
            return NoContent();
        }

        // DELETE: api/products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _service.DeleteAsync(id))
                return NotFound();
            return NoContent();
        }
        [HttpGet("count/total")]
        public async Task<IActionResult> GetTotalProducts()
        {
            var count = await _service.GetTotalProductsAsync();
            return Ok(count);
        }
        [HttpGet("count/by-month/{year}")]
        public async Task<IActionResult> GetProductCountByMonth(int year)
        {
            var result = await _service.GetProductCountByMonthAsync(year);
            return Ok(result);
        }

        [HttpGet("store/{storeId}")]
        public async Task<IActionResult> GetProductsByStore(int storeId)
        {
            var products = await _service.GetProductsByStoreAsync(storeId);
            if (products == null || !products.Any())
                return NotFound("No products found for this store.");

            return Ok(products);
        }
        [HttpGet("ByStore/{storeId}")]
        public async Task<IActionResult> GetProductsByStoreId(int storeId)
        {
            var products = await _service.GetProductsByStoreIdAsync(storeId);
            return Ok(products);
        }
        [HttpPost("full")]
        public async Task<IActionResult> CreateFullProduct([FromBody] ProductFullCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, errors });
            }

            var result = await _service.CreateFullProductAsync(model);
            if (result.Success)
                return Ok(new { success = true, productId = result.ProductId });

            return StatusCode(500, new { success = false, error = result.ErrorMessage });
        }

        [HttpDelete("DeleteProduct/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var success = await _service.DeleteProductAndRelatedAsync(id);
            if (!success)
                return BadRequest("Không thể xóa sản phẩm.");

            return Ok("Xóa thành công.");
        }

    }

}
