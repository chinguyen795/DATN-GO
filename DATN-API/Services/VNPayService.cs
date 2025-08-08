using System.Net;
using System.Security.Cryptography;
using System.Text;

public interface IVNPayService
{
    string CreatePaymentUrl(VnpCreatePaymentRequest req);
    bool ValidateReturn(IDictionary<string, string> query);
}

public class VnpCreatePaymentRequest
{
    public string OrderId { get; set; }      // vnp_TxnRef
    public long Amount { get; set; }         // VND (chưa x100)
    public string OrderInfo { get; set; }
    public string IpAddress { get; set; }
    public string Locale { get; set; } = "vn";
}

public class VNPayService : IVNPayService
{
    private readonly IConfiguration _cfg;
    public VNPayService(IConfiguration cfg) => _cfg = cfg;

    private string Cfg(string key) => _cfg[$"VNPay:{key}"]?.Trim() ?? "";

    public string CreatePaymentUrl(VnpCreatePaymentRequest req)
    {
        var baseUrl = Cfg("BaseUrl").TrimEnd('?');
        var returnUrl = Cfg("ReturnUrl");
        var tmnCode = Cfg("TmnCode");
        var secret = Cfg("HashSecret");

        var p = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = "2.1.0",
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = tmnCode,
            ["vnp_Amount"] = (req.Amount * 100).ToString(), // x100
            ["vnp_CurrCode"] = "VND",
            ["vnp_TxnRef"] = req.OrderId,
            ["vnp_OrderInfo"] = req.OrderInfo,
            ["vnp_OrderType"] = "other",
            ["vnp_Locale"] = string.IsNullOrWhiteSpace(req.Locale) ? "vn" : req.Locale,
            ["vnp_ReturnUrl"] = returnUrl,
            ["vnp_IpAddr"] = string.IsNullOrWhiteSpace(req.IpAddress) ? "127.0.0.1" : req.IpAddress,
            ["vnp_CreateDate"] = DateTime.Now.ToString("yyyyMMddHHmmss")
        };

        string raw = JoinParams(p);                 // đã URL-encode value
        string hash = HmacSHA512(secret, raw);      // ký trên chuỗi raw này
        string url = $"{baseUrl}?{raw}&vnp_SecureHash={hash}";

        // DEBUG: log ra để bạn so
        Console.WriteLine("VNPay raw: " + raw);
        Console.WriteLine("VNPay hash: " + hash);
        Console.WriteLine("VNPay url:  " + url);

        return url;
    }

    public bool ValidateReturn(IDictionary<string, string> query)
    {
        var secret = Cfg("HashSecret");
        if (!query.TryGetValue("vnp_SecureHash", out var secureHash) || string.IsNullOrEmpty(secureHash))
            return false;

        var p = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var kv in query)
        {
            if (!kv.Key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase)) continue;
            if (kv.Key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase)) continue;
            if (string.IsNullOrEmpty(kv.Value)) continue;
            p[kv.Key] = kv.Value;
        }

        string raw = JoinParams(p);
        string calc = HmacSHA512(secret, raw);

        Console.WriteLine("VNPay return raw:  " + raw);
        Console.WriteLine("VNPay return calc: " + calc);
        Console.WriteLine("VNPay return sent: " + secureHash);

        return secureHash.Equals(calc, StringComparison.OrdinalIgnoreCase);
    }

    private static string JoinParams(SortedDictionary<string, string> dict)
    {
        // WebUtility.UrlEncode -> space thành %20 (không dùng '+')
        return string.Join("&", dict.Select(kv => $"{kv.Key}={WebUtility.UrlEncode(kv.Value)}"));
    }

    private static string HmacSHA512(string key, string input)
    {
        using var h = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var bytes = h.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(bytes).Replace("-", "");
    }
}
