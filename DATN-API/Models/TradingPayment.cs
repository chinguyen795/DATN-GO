using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_API.Models
{
    public class TradingPayment
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Stores")]
        public int StoreId { get; set; }
        [JsonIgnore]
        public virtual Stores? Store { get; set; }

        public decimal Cost { get; set; }
        public DateTime Date { get; set; }
        public TradingPaymentStatus Status { get; set; }

    }
    public enum TradingPaymentStatus
    {
        ChoXuLy = 0,   
        DaXacNhan = 1, 
        TuChoi = 2     
    }
}
