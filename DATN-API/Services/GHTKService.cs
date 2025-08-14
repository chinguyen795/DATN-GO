using DATN_API.Interfaces;
using DATN_API.ViewModels.GHTK;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

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
        public async Task<string?> CreateOrderAsync(GHTKCreateOrderRequest payload)
        {
            var baseUrl = _config["GHTK:BaseUrl"] ?? "";
            var url = $"{baseUrl.TrimEnd('/')}/services/shipment/order";

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var respStr = await response.Content.ReadAsStringAsync();

            JObject obj;
            try { obj = JObject.Parse(respStr); } catch { return null; }

            // success true
            if (obj["success"]?.Value<bool>() == true)
            {
                var label = obj["data"]?["label"]?.ToString()
                         ?? obj["order"]?["label"]?.ToString();
                return string.IsNullOrWhiteSpace(label) ? null : label;
            }

            // idempotent: ORDER_ID_EXIST -> lấy lại label
            var errCode = obj["error"]?["code"]?.ToString();
            if (string.Equals(errCode, "ORDER_ID_EXIST", StringComparison.OrdinalIgnoreCase))
            {
                var ghtkLabel = obj["error"]?["ghtk_label"]?.ToString();
                return string.IsNullOrWhiteSpace(ghtkLabel) ? null : ghtkLabel;
            }

            return null;
        }


        public async Task<(bool Success, string? Label, string Raw)> CreateOrderDebugAsync(GHTKCreateOrderRequest payload)
        {
            var baseUrl = _config["GHTK:BaseUrl"] ?? "";
            var url = $"{baseUrl.TrimEnd('/')}/services/shipment/order";

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var respStr = await response.Content.ReadAsStringAsync();

            Console.WriteLine("[GHTK] DEBUG Status: " + response.StatusCode);
            Console.WriteLine("[GHTK] DEBUG Body  : " + respStr);

            try
            {
                var obj = JObject.Parse(respStr);
                var ok = obj["success"]?.Value<bool>() == true;

                string? label =
                    obj["data"]?["label"]?.ToString() ??
                    obj["order"]?["label"]?.ToString();

                return (ok, string.IsNullOrWhiteSpace(label) ? null : label, respStr);
            }
            catch
            {
                // không parse được, vẫn trả raw
                return (false, null, respStr);
            }
        }

    }
}