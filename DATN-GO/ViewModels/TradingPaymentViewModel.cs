using DATN_GO.Models;

namespace DATN_GO.ViewModels
{
    public class TradingPaymentViewModel
    {
        public int Id { get; set; }
        public int StoreId { get; set; }
        public decimal Cost { get; set; }
        public DateTime Date { get; set; }
        public TradingPaymentStatus Status { get; set; }

        public string? Bank { get; set; }
        public string? BankAccount { get; set; }
        public string? BankAccountOwner { get; set; }
    }
}
