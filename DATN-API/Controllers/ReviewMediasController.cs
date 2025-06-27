using DATN_API.Models;
using DATN_API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DATN_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewMediasController : ControllerBase
    {
        private readonly IReviewMediasService _service;

        public ReviewMediasController(IReviewMediasService service)
        {
            _service = service;
        }

        // GET: api/reviewmedias
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var medias = await _service.GetAllAsync();
            return Ok(medias);
        }

        // GET: api/reviewmedias/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var media = await _service.GetByIdAsync(id);
            if (media == null) return NotFound();
            return Ok(media);
        }

        // POST: api/reviewmedias
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ReviewMedias model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var created = await _service.CreateAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/reviewmedias/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ReviewMedias model)
        {
            if (!await _service.UpdateAsync(id, model))
                return BadRequest("ID không khớp hoặc không tìm thấy review media");
            return NoContent();
        }

        // DELETE: api/reviewmedias/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _service.DeleteAsync(id))
                return NotFound();
            return NoContent();
        }
    }
}
