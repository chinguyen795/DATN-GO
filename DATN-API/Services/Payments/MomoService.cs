// MomoService.cs
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;



public sealed class MomoService : IMomoService
{
    private readonly IConfiguration _cfg;
    private readonly HttpClient _http;
    public MomoService(IConfiguration cfg, IHttpClientFactory http)
    {
        _cfg = cfg;
        _http = http.CreateClient();
    }

    public async Task<(bool Ok, string? PayUrl, string? Message)> CreatePaymentAsync(
        string orderId, long amount, string orderInfo, string clientIp, string? extraData = "")
    {
        // đọc cấu hình
        var s = _cfg.GetSection("MomoAPI");
        var partnerCode = s["PartnerCode"];
        var accessKey = s["AccessKey"];
        var secretKey = s["SecretKey"];
        var endpoint = s["MomoApiUrl"];
        var returnUrl = s["ReturnUrl"];
        var notifyUrl = s["NotifyUrl"];
        var requestType = s["RequestType"] ?? "captureMoMoWallet";

        // kiểm tra cấu hình thiếu
        string? missing = new[]
        {
            (partnerCode, "MomoAPI:PartnerCode"),
            (accessKey,   "MomoAPI:AccessKey"),
            (secretKey,   "MomoAPI:SecretKey"),
            (endpoint,    "MomoAPI:MomoApiUrl"),
            (returnUrl,   "MomoAPI:ReturnUrl"),
            (notifyUrl,   "MomoAPI:NotifyUrl"),
        }.FirstOrDefault(t => string.IsNullOrWhiteSpace(t.Item1)).Item2;
        if (missing != null) return (false, null, $"Thiếu cấu hình {missing}");

        // MoMo yêu cầu orderId/requestId duy nhất mỗi lần tạo giao dịch
        var momoOrderId = $"{orderId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        var requestId = Guid.NewGuid().ToString("N");
        extraData ??= "";

        // chuỗi raw để ký (MoMo V1)
        var rawPairs = new[]
        {
            $"partnerCode={partnerCode}",
            $"accessKey={accessKey}",
            $"requestId={requestId}",
            $"amount={amount}",
            $"orderId={momoOrderId}",
            $"orderInfo={orderInfo}",
            $"returnUrl={returnUrl}",
            $"notifyUrl={notifyUrl}",
            $"extraData={extraData}"
        };
        var raw = string.Join("&", rawPairs);
        var signature = HmacSha256(secretKey!, raw);

        // payload gửi MoMo
        var payload = new
        {
            partnerCode = partnerCode,
            accessKey = accessKey,
            requestId = requestId,
            orderId = momoOrderId,        // phải duy nhất
            amount = amount.ToString(),
            orderInfo = orderInfo,
            returnUrl = returnUrl,
            notifyUrl = notifyUrl,
            requestType = requestType,
            extraData = extraData,
            signature = signature
        };

        var resp = await _http.PostAsJsonAsync(endpoint!, payload);
        if (!resp.IsSuccessStatusCode)
            return (false, null, $"HTTP {(int)resp.StatusCode}");

        var dto = await resp.Content.ReadFromJsonAsync<MomoCreateResponse>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (dto is null) return (false, null, "Empty response from MoMo");

        // MoMo trả về payUrl hoặc deeplink
        var payUrl = !string.IsNullOrWhiteSpace(dto.PayUrl) ? dto.PayUrl
                   : !string.IsNullOrWhiteSpace(dto.Deeplink) ? dto.Deeplink
                   : null;

        var ok = (dto.ErrorCode ?? -1) == 0 && !string.IsNullOrWhiteSpace(payUrl);
        return (ok, payUrl, dto.Message ?? (ok ? "OK" : "Create payment failed"));
    }

    public bool ValidateSignature(IDictionary<string, string> data, string signature)
    {
        var secretKey = _cfg["MomoAPI:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey) || string.IsNullOrWhiteSpace(signature))
            return false;

        string Get(string k) => data.TryGetValue(k, out var v) ? v : "";

        // raw callback V1 (tài liệu MoMo)
        var parts = new[]
        {
            $"partnerCode={Get("partnerCode")}",
            $"accessKey={Get("accessKey")}",
            $"requestId={Get("requestId")}",
            $"amount={Get("amount")}",
            $"orderId={Get("orderId")}",
            $"orderInfo={Get("orderInfo")}",
            $"orderType={Get("orderType")}",
            $"transId={Get("transId")}",
            $"message={Get("message")}",
            $"localMessage={Get("localMessage")}",
            $"responseTime={Get("responseTime")}",
            $"errorCode={Get("errorCode")}",
            $"payType={Get("payType")}",
            $"extraData={Get("extraData")}"
        };
        var raw = string.Join("&", parts);
        var mine = HmacSha256(secretKey!, raw);
        return string.Equals(mine, signature, StringComparison.OrdinalIgnoreCase);
    }

    private static string HmacSha256(string key, string input)
    {
        using var h = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(input)))
            .Replace("-", "").ToLowerInvariant();
    }

    // DTO đọc response tạo thanh toán MoMo
    private sealed class MomoCreateResponse
    {
        public int? ErrorCode { get; set; }
        public string? Message { get; set; }
        public string? PayUrl { get; set; }   // payUrl
        public string? Deeplink { get; set; }   // deeplink (mobile)
    }
}
