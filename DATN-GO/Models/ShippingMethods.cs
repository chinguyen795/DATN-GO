using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_GO.Models
{
    public class ShippingMethods
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Diner")]
        public int DinerId { get; set; }

        public decimal Price { get; set; }

        [MaxLength(50)]
        public string MethodName { get; set; }
        [JsonIgnore]
        public virtual Diners? Diner { get; set; }
        [JsonIgnore]
        public ICollection<Orders>? Orders { get; set; }

    }
}
