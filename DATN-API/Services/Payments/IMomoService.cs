public interface IMomoService
{
    Task<(bool Ok, string? PayUrl, string? Message)> CreatePaymentAsync(
        string orderId, long amount, string orderInfo, string clientIp, string? extraData = "");
    bool ValidateSignature(IDictionary<string, string> data, string signature);
}