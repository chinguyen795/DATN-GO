using DATN_API.Interfaces;
using DATN_API.Models;
using DATN_API.ViewModels.Request;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DATN_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VariantCompositionController : ControllerBase
    {
        private readonly IVariantCompositionService _service;

        public VariantCompositionController(IVariantCompositionService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpGet("by-product-variant/{productVariantId}")]
        public async Task<IActionResult> GetByProductVariant(int productVariantId)
        {
            var result = await _service.GetByProductVariantIdAsync(productVariantId);
            return Ok(result);
        }

        [HttpPost("add-multiple")]
        public async Task<IActionResult> AddMultiple([FromBody] VariantCompositionRequest request)
        {
            await _service.AddMultipleAsync(
                request.ProductId,
                request.ProductVariantId,
                request.VariantPairs.Select(p => (p.VariantId, p.VariantValueId)).ToList()
            );

            return Ok(new { message = "Thêm thành công!" });
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] VariantComposition updated)
        {
            if (id != updated.Id) return BadRequest("ID không khớp.");
            await _service.UpdateAsync(updated);
            return Ok("Cập nhật thành công.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return Ok("Xóa thành công.");
        }
    }

}
