using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_API.Models
{
    public class Products
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Category")]
        public int CategoryId { get; set; }

        [ForeignKey("Diner")]
        public int DinerId { get; set; }

        [MaxLength(50)]
        public string ProductName { get; set; }

        public decimal Price { get; set; }

        public string Discription { get; set; }

        [MaxLength(50)]
        public string MainImage { get; set; }

        [MaxLength(50)]
        public string Status { get; set; }

        public int Quantity { get; set; }

        public int Views { get; set; }

        public DateTime CreateAt { get; set; }
        [JsonIgnore]
        public virtual Categories? Category { get; set; }
        [JsonIgnore]
        public virtual Diners? Diner { get; set; }
        [JsonIgnore]
        public ICollection<Options>? Options { get; set; }
        [JsonIgnore]
        public ICollection<ProductSkus>? ProductSkus { get; set; }
        [JsonIgnore]
        public ICollection<SkusValues>? SkusValues { get; set; }
        [JsonIgnore]
        public ICollection<OptionValues>? OptionValues { get; set; }
        [JsonIgnore]
        public ICollection<ProductVouchers>? ProductVouchers { get; set; }
        [JsonIgnore]
        public ICollection<Carts>? Carts { get; set; }
        [JsonIgnore]
        public ICollection<Reviews>? Reviews { get; set; }
        [JsonIgnore]
        public ICollection<OrderDetails>? OrderDetails { get; set; }


    }
}
