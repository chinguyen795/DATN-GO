using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using DATN_API.ViewModels.GHTK;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShippingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IGHTKService _ghtkService;

        private readonly IConfiguration _configuration;

        public ShippingController(ApplicationDbContext context, IGHTKService ghtkService, IConfiguration configuration)
        {
            _context = context;
            _ghtkService = ghtkService;
            _configuration = configuration;
        }


        [HttpPost("ghtk/calculate-fee")]
        public async Task<IActionResult> CalculateGHTKFee([FromBody] GHTKShippingFeeFromDbRequest request)
        {
            var address = await _context.Addresses
                .Include(a => a.City)
                .FirstOrDefaultAsync(a => a.Id == request.AddressId);

            if (address == null || address.City == null)
                return NotFound("Không tìm thấy địa chỉ người nhận hoặc thành phố.");

            // Lấy quận từ CityId
            var district = await _context.Districts
                .FirstOrDefaultAsync(d => d.CityId == address.City.Id);

            if (district == null)
                return NotFound("Không tìm thấy quận cho địa chỉ này.");

            // Lấy phường từ DistrictId
            var ward = await _context.Wards
                .FirstOrDefaultAsync(w => w.DistrictId == district.Id);

            if (ward == null)
                return NotFound("Không tìm thấy phường cho địa chỉ này.");

            var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == request.StoreId);

            if (store == null || string.IsNullOrEmpty(store.Province) || string.IsNullOrEmpty(store.District))
                return NotFound("Không tìm thấy thông tin địa chỉ cửa hàng.");

            // Build GHTK URL
            var apiUrl = $"{_configuration["GHTK:BaseUrl"]}/services/shipment/fee" +
                $"?address={Uri.EscapeDataString(address.Description ?? "")}" +
                $"&province={Uri.EscapeDataString(address.City.CityName)}" +
                $"&district={Uri.EscapeDataString(district.DistrictName)}" +
                $"&ward={Uri.EscapeDataString(ward.WardName)}" +
                $"&pick_province={Uri.EscapeDataString(store.Province)}" +
                $"&pick_district={Uri.EscapeDataString(store.District)}" +
                $"&weight={request.Weight}" +
                $"&value={request.Value}" +
                $"&deliver_option=none&tags[]=1";

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Token", _configuration["GHTK:Token"]);

            var response = await httpClient.GetAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, errorContent);
            }

            var content = await response.Content.ReadAsStringAsync();
            return Ok(content);
        }


    }
}
