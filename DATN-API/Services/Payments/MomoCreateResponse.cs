namespace DATN_API.Services.Payments
{
    public sealed class MomoCreateResponse
    {
        public int? errorCode { get; set; }         // 0 là OK
        public string? message { get; set; }
        public string? payUrl { get; set; }         // web
        public string? deeplink { get; set; }       // mobile (fallback)
        public string? qrCodeUrl { get; set; }      // đôi khi có
    }
}
