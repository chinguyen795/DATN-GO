using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_API.Models
{
    public class Orders
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        [ForeignKey("Voucher")]
        public int? VoucherId { get; set; } // nullable

        [ForeignKey("ShippingMethod")]
        public int ShippingMethodId { get; set; }
        public string LabelId { get; set; }
        public DateTime OrderDate { get; set; }

        public decimal TotalPrice { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(50)")]
        public OrderStatus Status { get; set; }

        [MaxLength(50)]
        public string PaymentMethod { get; set; }

        public int? TransactionId { get; set; }

        public DateTime? PaymentDate { get; set; }

        [MaxLength(50)]
        public string PaymentStatus { get; set; }

        public decimal DeliveryFee { get; set; }
        [JsonIgnore]
        public virtual Users? User { get; set; }
        [JsonIgnore]
        public virtual Vouchers? Voucher { get; set; }
        [JsonIgnore]
        public ShippingMethods? ShippingMethod { get; set; }
        [JsonIgnore]
        public ICollection<Reviews>? Reviews { get; set; }
        [JsonIgnore]
        public DeliveryTrackings? DeliveryTracking { get; set; }
        [JsonIgnore]
        public ICollection<OrderDetails>? OrderDetails { get; set; }


    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OrderStatus
    {
        ChoXuLy,
        ChoLayHang,
        DangGiao,
        DaHoanThanh,
        DaHuy
    }
}
