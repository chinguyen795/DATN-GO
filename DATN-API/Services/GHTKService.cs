using DATN_API.Interfaces;
using DATN_API.ViewModels.GHTK;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
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
        public async Task<GHTKOrderStatusViewModel> GetStatusByLabelIdAsync(string labelId)
        {
            var url = $"{_config["GHTK:BaseUrl"]}/services/shipment/v2/{labelId}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);

            if (json["success"]?.Value<bool>() != true)
                return null;

            var data = json["order"];
            var status = data["status"]?.Value<int>() ?? 0;

            return new GHTKOrderStatusViewModel
            {
                OrderCode = data["label_id"]?.ToString(),
                Status = status,
                StatusText = data["status_text"]?.ToString() ?? MapStatusText(status)
            };
        }



        public string MapStatusText(int statusId) => statusId switch
        {
            -1 => "Hủy đơn hàng",
            1 => "Chưa tiếp nhận",
            2 => "Đã tiếp nhận",
            3 => "Đã lấy hàng/Đã nhập kho",
            4 => "Đang giao hàng",
            5 => "Đã giao hàng/Chưa đối soát",
            6 => "Đã đối soát",
            7 => "Không lấy được hàng",
            8 => "Hoãn lấy hàng",
            9 => "Không giao được hàng",
            10 => "Delay giao hàng",
            11 => "Đã đối soát công nợ trả hàng",
            12 => "Đang lấy hàng",
            13 => "Đơn hàng bồi hoàn",
            20 => "Đang trả hàng (COD cầm hàng đi trả)",
            21 => "Đã trả hàng (COD đã trả xong hàng)",
            123 => "Shipper báo đã lấy hàng",
            127 => "Shipper báo không lấy được hàng",
            128 => "Shipper báo delay lấy hàng",
            45 => "Shipper báo đã giao hàng",
            49 => "Shipper báo không giao được",
            410 => "Shipper báo delay giao hàng",
            _ => $"Mã trạng thái {statusId}"
        };


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
