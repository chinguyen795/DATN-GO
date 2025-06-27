using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DATN_API.Models
{
    public class TradingPayment
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Stores")]
        public int DinerId { get; set; }
        public virtual Stores? store { get; set; }

        public decimal Cost { get; set; }
        public DateTime Date { get; set; }

        public string? RemittanceAccount { get; set; }
        public string? RemittanceBank { get; set; }
        public string? BeneficiaryAccount { get; set; }
        public string? BeneficiaryBank { get; set; }
    }
}
