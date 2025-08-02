using DATN_API.Data;
using DATN_API.Models;
using DATN_API.Services;
using DATN_API.Services.Interfaces;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUsersService _service;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _config;

        public UsersController(IUsersService service, IJwtService jwtService, IConfiguration config)
        {
            _service = service;
            _jwtService = jwtService;
            _config = config;
        }

        // GET: api/users
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _service.GetAllAsync();
            return Ok(users);
        }

        // GET: api/users/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _service.GetByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        // POST: api/users
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Users model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var created = await _service.CreateAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/users/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Users model)
        {
            if (!await _service.UpdateAsync(id, model))
                return BadRequest("ID không khớp hoặc không tìm thấy user");
            return NoContent();
        }

        // DELETE: api/users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _service.DeleteAsync(id))
                return NotFound();
            return NoContent();
        }

        [Authorize]
        [HttpGet("Profile")]
        public async Task<IActionResult> GetProfile()
        {
            if (!Int32.TryParse(HttpContext?.User?.Identity?.Name, out var uId))
                return Unauthorized("Không tìm thấy thông tin người dùng");
            var user = await _service.GetByIdAsync(uId);
            if (user == null)
                return NotFound("Không tìm thấy người dùng");
            return Ok(user);
        }


        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
        {
            if (string.IsNullOrEmpty(dto.IdToken))
                return BadRequest(new { message = "Thiếu mã xác thực từ Google." });

            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken/*, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _config["Authentication:Google:ClientId"] }
                }*/);

                var existingUser = await _service.GetByEmailAsync(payload.Email);
                if (existingUser == null)
                {
                    var newUser = new Users
                    {
                        Email = payload.Email,
                        FullName = payload.Name,
                        Avatar = payload.Picture,
                        Status = UserStatus.Active,
                        Gender = GenderType.Other,
                        RoleId = 1,
                        Password = Guid.NewGuid().ToString(),
                        Phone = "0000000000",
                        CreateAt = DateTime.UtcNow,
                        UpdateAt = DateTime.UtcNow
                    };

                    existingUser = await _service.CreateAsync(newUser);
                }

                var token = _jwtService.GenerateToken(existingUser);

                return Ok(new
                {
                    token,
                    user = new
                    {
                        id = existingUser.Id,
                        fullName = existingUser.FullName,
                        email = existingUser.Email,
                        roles = existingUser.RoleId
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Xác thực Google thất bại",
                    error = ex.Message,
                    stack = ex.StackTrace // 👈 tạm thêm để dễ debug
                });
            }
        }
    }

}
