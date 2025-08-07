using DATN_API.Interfaces;
using DATN_API.ViewModels.GHTK;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace DATN_API.Services
{
    public class GHTKService : IGHTKService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public GHTKService(IConfiguration config)
        {
            _config = config;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Token", _config["GHTK:Token"]);
        }

        public async Task<int?> CalculateShippingFeeAsync(GHTKFeeRequestViewModel model)
        {
            var query = $"pick_province={model.PickProvince}" +
                        $"&pick_district={model.PickDistrict}" +
                        $"&province={model.Province}" +
                        $"&district={model.District}" +
                        $"&address={Uri.EscapeDataString(model.Address)}" +
                        $"&weight={model.Weight}" +
                        $"&value={model.Value}" +
                        $"&deliver_option={model.DeliverOption}" +
                        $"&tags[]=1";

            var baseUrl = _config["GHTK:BaseUrl"];
            var url = $"{baseUrl}/services/shipment/fee?{query}";

            Console.WriteLine("[GHTK] URL gọi: " + url);

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            Console.WriteLine("[GHTK] StatusCode: " + response.StatusCode);
            Console.WriteLine("[GHTK] Raw Response: " + content);

            var json = JObject.Parse(content);

            // Log message khi lỗi
            if (json["success"]?.Value<bool>() != true)
            {
                Console.WriteLine("[GHTK] Lỗi: " + json["message"]?.ToString());
                return null;
            }

            var fee = json["fee"]?["fee"]?.Value<int>();
            return fee;
        }

    }
}
