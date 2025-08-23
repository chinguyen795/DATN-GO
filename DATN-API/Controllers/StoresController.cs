using DATN_API.Interfaces;
using DATN_API.Models;
using DATN_API.Services;
using DATN_API.ViewModel;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DATN_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StoresController : ControllerBase
    {
        private readonly IStoresService _service;

        public StoresController(IStoresService service)
        {
            _service = service;
        }

        // GET: api/stores
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var stores = await _service.GetAllAsync();
            return Ok(stores);
        }

        // GET: api/stores/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var store = await _service.GetByIdAsync(id);
            if (store == null) return NotFound();
            return Ok(store);
        }

        // POST: api/stores
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Stores model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var created = await _service.CreateAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/stores/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Stores model)
        {
            if (!await _service.UpdateAsync(id, model))
                return BadRequest("ID không khớp hoặc không tìm thấy store");
            return NoContent();
        }

        // DELETE: api/stores/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _service.DeleteAsync(id))
                return NotFound();
            return NoContent();
        }

        // GET: api/stores/user/5
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUserId(int userId)
        {
            var store = await _service.GetByUserIdAsync(userId);
            if (store == null) return NotFound();
            return Ok(store);
        }

        [HttpGet("count/all")]
        public async Task<IActionResult> GetTotalStores()
        {
            var total = await _service.GetTotalStoresAsync();
            return Ok(total);
        }

        [HttpGet("count/active")]
        public async Task<IActionResult> GetTotalActiveStores()
        {
            var total = await _service.GetTotalActiveStoresAsync();
            return Ok(total);
        }
        [HttpGet("count/by-month-year")]
        public async Task<IActionResult> GetStoreCountByMonthYear([FromQuery] int month, [FromQuery] int year)
        {
            var count = await _service.GetStoreCountByMonthYearAsync(month, year);
            return Ok(count);
        }
        [HttpGet("count/by-month/{year}")]
        public async Task<IActionResult> GetStoreCountByMonth(int year)
        {
            var data = await _service.GetStoreCountByMonthAsync(year);
            return Ok(data);
        }

        // GET: api/stores/pending
        [HttpGet("PendingApproval")]
        public async Task<IActionResult> GetPendingStores()
        {
            var stores = await _service.GetByStatusAsync("PendingApproval");

            var result = stores.Select(store => new StoreAdminViewModel
            {
                Id = store.Id,
                Name = store.Name,
                Avatar = store.Avatar,
                CoverPhoto = store.CoverPhoto,
                Address = store.Address,
                Latitude = store.Latitude,
                Longitude = store.Longitude,
                Rating = store.Rating,
                Status = store.Status,
                Bank = store.Bank,
                BankAccount = store.BankAccount,
                CreateAt = store.CreateAt,
                UpdateAt = store.UpdateAt,
                OwnerName = store.User?.FullName,
                OwnerEmail = store.User?.Email
            });

            return Ok(result);
        }
        // PUT: api/stores/approve/5
        [HttpPut("approve/{id}")]
        public async Task<IActionResult> ApproveStore(int id)
        {
            var result = await _service.UpdateStatusAsync(id, "Active"); // Use "Active" instead of "Approved"
            if (!result)
                return NotFound("Không tìm thấy cửa hàng.");
            return Ok("Cửa hàng đã được duyệt.");
        }

        [HttpPut("reject/{id}")]
        public async Task<IActionResult> RejectStore(int id)
        {
            var result = await _service.UpdateStatusAsync(id, "Rejected");
            if (!result)
                return NotFound("Không tìm thấy cửa hàng.");
            return Ok("Cửa hàng đã bị từ chối.");
        }

        [HttpGet("admin/{id}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var store = await _service.GetAdminDetailAsync(id);
            if (store == null) return NotFound();
            return Ok(store);
        }

        [HttpGet("admin")]
        public async Task<IActionResult> GetAllAdminStores()
        {
            var stores = await _service.GetAllAdminStoresAsync();
            return Ok(stores);
        }


    }
}