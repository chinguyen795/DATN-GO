using Microsoft.AspNetCore.Mvc;

namespace DATN_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BankController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public BankController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetBankList()
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync("https://api.vietqr.io/v2/banks");

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "Không thể lấy danh sách ngân hàng.");
            }

            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }
    }

}
