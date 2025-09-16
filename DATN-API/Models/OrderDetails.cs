using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_API.Models
{
    public class OrderDetails
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Order")]
        public int OrderId { get; set; }

        [ForeignKey("Product")]
        public int ProductId { get; set; }
        public int? ProductVariantId { get; set; }   // <-- cần có

        public int Quantity { get; set; }

        public decimal Price { get; set; }
        [JsonIgnore]
        public virtual Orders? Order { get; set; }
        [JsonIgnore]
        public virtual Products? Product { get; set; }
    }
}