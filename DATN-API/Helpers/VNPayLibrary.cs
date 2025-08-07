using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace DATN_API.Helpers
{
    public class VNPayLibrary
    {
        private readonly SortedList<string, string> _requestData = new();
        private readonly SortedList<string, string> _responseData = new();

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
                _requestData[key] = value;
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
                _responseData[key] = WebUtility.UrlDecode(value); // ✅ GIẢI MÃ dữ liệu từ VNPAY
        }

        public string GetResponseData(string key)
        {
            _responseData.TryGetValue(key, out var value);
            return value;
        }

        public string CreateRequestUrl(string baseUrl, string hashSecret)
        {
            var rawData = string.Join("&", _requestData
                .OrderBy(x => x.Key)
                .Select(x => $"{x.Key}={x.Value}"));

            string secureHash = ComputeSha512(hashSecret + rawData);

            var queryString = string.Join("&", _requestData
                .OrderBy(x => x.Key)
                .Select(x => $"{WebUtility.UrlEncode(x.Key)}={WebUtility.UrlEncode(x.Value)}"));

            return $"{baseUrl}?{queryString}&vnp_SecureHash={secureHash}";
        }

        public bool ValidateSignature(string hashSecret)
        {
            if (!_responseData.TryGetValue("vnp_SecureHash", out string? vnpSecureHash) || string.IsNullOrEmpty(vnpSecureHash))
                return false;

            _responseData.Remove("vnp_SecureHash");
            _responseData.Remove("vnp_SecureHashType");

            // ✅ Log tất cả key/value để kiểm tra
            Console.WriteLine("===== CHECKING SIGNATURE =====");
            foreach (var kv in _responseData.OrderBy(k => k.Key))
            {
                Console.WriteLine($"KEY: [{kv.Key}] VALUE: [{kv.Value}]");
            }

            string rawData = string.Join("&", _responseData
                .OrderBy(k => k.Key)
                .Select(kv => $"{kv.Key}={kv.Value}"));

            string calculatedHash = ComputeSha512(hashSecret + rawData);

            Console.WriteLine("RAW DATA: " + rawData);
            Console.WriteLine("HASH SECRET: " + hashSecret);
            Console.WriteLine("HASH TÍNH: " + calculatedHash);
            Console.WriteLine("HASH VNPAY: " + vnpSecureHash);
            Console.WriteLine("===== END SIGNATURE CHECK =====");

            return string.Equals(calculatedHash, vnpSecureHash, StringComparison.OrdinalIgnoreCase);
        }

        private string ComputeSha512(string input)
        {
            using var sha512 = SHA512.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = sha512.ComputeHash(bytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}
