namespace DATN_API.ViewModels.Vnpay
{
    public class VNPayPaymentRequestModel
    {
        public string OrderDescription { get; set; }
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public string BankCode { get; set; }
        public string Language { get; set; }
    }

}
