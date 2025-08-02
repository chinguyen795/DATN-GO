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

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingPosts()
        {
            var posts = await _postsService.GetPendingPostsAsync();
            var result = posts.Select(p => new
            {
                p.Id,
                p.Content,
                p.Image,
                Status = p.Status.ToString(),
                p.CreateAt,
                UserName = p.User?.FullName,
                UserEmail = p.User?.Email
            });

            return Ok(result);
        }

        [HttpPut("approve/{id}")]
        public async Task<IActionResult> Approve(int id)
        {
            var success = await _postsService.ApprovePostAsync(id);
            if (!success) return NotFound("Không tìm thấy bài viết");

            return Ok("Bài viết đã được duyệt");
        }

        [HttpPut("reject/{id}")]
        public async Task<IActionResult> Reject(int id)
        {
            var success = await _postsService.RejectPostAsync(id);
            if (!success) return NotFound("Không tìm thấy bài viết");

            return Ok("Bài viết đã bị từ chối");
        }
    }
}
