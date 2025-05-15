using DATN_API.Data;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PostsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/posts
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var posts = await _context.Posts
                .Include(p => p.User)
                .ToListAsync();

            return Ok(posts);
        }

        // GET: api/posts/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var post = await _context.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null) return NotFound();

            return Ok(post);
        }

        // GET: api/posts/user/3
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var posts = await _context.Posts
                .Where(p => p.UserId == userId)
                .ToListAsync();

            return Ok(posts);
        }

        // POST: api/posts
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Posts model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            model.CreateAt = DateTime.UtcNow;

            _context.Posts.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        // PUT: api/posts/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Posts model)
        {
            if (id != model.Id)
                return BadRequest("ID không khớp");

            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

            post.UserId = model.UserId;
            post.Content = model.Content;
            post.Image = model.Image;
            post.CreateAt = model.CreateAt;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/posts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
