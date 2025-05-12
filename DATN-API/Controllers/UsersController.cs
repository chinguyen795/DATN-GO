using DATN_API.Data;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
        [Route("api/[controller]")]
        [ApiController]
        public class UsersController : ControllerBase
        {
            private readonly ApplicationDbContext _context;

            public UsersController(ApplicationDbContext context)
            {
                _context = context;
            }

            // GET: api/Users
            [HttpGet]
            public async Task<ActionResult<IEnumerable<Users>>> GetUsers()
            {
                return await _context.Users.ToListAsync();
            }

            // GET: api/Users/id
            [HttpGet("{id}")]
            public async Task<ActionResult<Users>> Getuser(int id)
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound();
                }
                return user;
            }

            // POST: api/Users
            [HttpPost]
            public async Task<ActionResult<Users>> Postuser(Users user)
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(Getuser), new { id = user.Id }, user);
            }

            // PUT: api/Users/id
            [HttpPut("{id}")]
            public async Task<IActionResult> Putuser(int id, Users user)
            {
                if (id != user.Id)
                {
                    return BadRequest();
                }

                _context.Entry(user).State = EntityState.Modified;
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!userExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return NoContent();
            }

            // DELETE: api/Users/5
            [HttpDelete("{id}")]
            public async Task<IActionResult> Deleteuser(int id)
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return NoContent();
            }

            private bool userExists(int id)
            {
                return _context.Users.Any(e => e.Id == id);
            }
        }

}
