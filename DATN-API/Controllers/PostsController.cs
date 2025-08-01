using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostsController : ControllerBase
    {
        private readonly IPostsService _postsService;

        public PostsController(IPostsService postsService)
        {
            _postsService = postsService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Posts>>> GetAll()
        {
            var posts = await _postsService.GetAllAsync();
            return Ok(posts);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Posts>> GetById(int id)
        {
            var post = await _postsService.GetByIdAsync(id);
            if (post == null) return NotFound();
            return Ok(post);
        }

        [HttpPost]
        public async Task<ActionResult<Posts>> Create([FromBody] Posts model)
        {
            if (string.IsNullOrEmpty(model.Content) || model.Content.Length < 5)
            {
                return BadRequest("Nội dung phải từ 5 ký tự trở lên.");
            }

            var newPost = await _postsService.CreateAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = newPost.Id }, newPost);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Posts model)
        {
            var success = await _postsService.UpdateAsync(id, model);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _postsService.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
        [HttpGet("User/{userId}")]
        public async Task<ActionResult<IEnumerable<Posts>>> GetPostsByUserId(int userId)
        {
            var posts = await _postsService.GetByUserIdAsync(userId);
            return Ok(posts);
        }

    }
}
