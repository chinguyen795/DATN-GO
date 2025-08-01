using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_GO.Models
{
    public class UserVouchers
    {
      
            [Key]
            public int Id { get; set; }

            [ForeignKey("User")]
            public int UserId { get; set; }
            [JsonIgnore]
            public virtual Users? User { get; set; }

            [ForeignKey("Voucher")]
            public int VoucherId { get; set; }
            [JsonIgnore]
            public virtual Vouchers? Voucher { get; set; }

            public DateTime SavedAt { get; set; }
            public bool IsUsed { get; set; } = false;
        }
    }
