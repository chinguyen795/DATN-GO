using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_API.Models
{
    public class UserTradingPayment
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Users")]
        public int UserId { get; set; }
        [JsonIgnore]
        public virtual Users? User { get; set; }

        public decimal? Cost { get; set; }
        public DateTime Date { get; set; }
        public string? Bank { get; set; }
        public string? BankAccount { get; set; }
        public string? BankAccountOwner { get; set; }
        public TradingPaymentStatus Status { get; set; }
    }
}
