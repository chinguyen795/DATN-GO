using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_GO.Models
{
    public class ShippingMethods
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Store")]
        public int StoreId { get; set; }

        public decimal Price { get; set; }

        [MaxLength(50)]
        public string MethodName { get; set; }
        [JsonIgnore]
        public virtual Stores? store { get; set; }
        [JsonIgnore]
        public ICollection<Orders>? Orders { get; set; }

    }
}
