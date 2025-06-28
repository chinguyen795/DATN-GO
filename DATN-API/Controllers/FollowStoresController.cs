using DATN_API.Interfaces;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DATN_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FollowStoresController : ControllerBase
    {
        private readonly IFollowStoresService _service;

        public FollowStoresController(IFollowStoresService service)
        {
            _service = service;
        }

        // GET: api/followstores
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var follows = await _service.GetAllAsync();
            return Ok(follows);
        }

        // GET: api/followstores/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var follow = await _service.GetByIdAsync(id);
            if (follow == null) return NotFound();
            return Ok(follow);
        }

        // POST: api/followstores
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] FollowStores model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var created = await _service.CreateAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/followstores/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] FollowStores model)
        {
            if (!await _service.UpdateAsync(id, model))
                return BadRequest("ID không khớp hoặc không tìm thấy follow");
            return NoContent();
        }

        // DELETE: api/followstores/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _service.DeleteAsync(id))
                return NotFound();
            return NoContent();
        }
    }
}
