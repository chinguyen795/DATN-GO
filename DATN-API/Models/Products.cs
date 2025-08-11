using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_API.Models
{
    public enum ProductStatus
    {
        [Display(Name = "Chờ duyệt")]
        Pending,
        [Display(Name = "Đã duyệt")]
        Approved
    }

    public class Products
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Category")]
        public int CategoryId { get; set; }

        [ForeignKey("Store")]
        public int StoreId { get; set; }

        [MaxLength(50)]
        [MinLength(2, ErrorMessage = "Tên phải từ 2 đến 50 kí tự.")]
        public string Name { get; set; }

        [MaxLength(50)]
        public string? Brand { get; set; }

        public int? Weight { get; set; }

        public string? Slug { get; set; }

        public string? Description { get; set; }

        public string? MainImage { get; set; }

        public ProductStatus Status { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng phải >= 0")]
        public int Quantity { get; set; }
        public int Views { get; set; }
        public float? Rating { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime UpdateAt { get; set; }
        [Range(1000, double.MaxValue, ErrorMessage = "Giá phải từ 1.000")]
        public decimal? CostPrice { get; set; }
        public float? Height { get; set; }

        public float? Width { get; set; }

        public float? Length { get; set; }
        public string? PlaceOfOrigin { get; set; }
        [MaxLength(50)]
        public string? Hashtag { get; set; }
        [JsonIgnore]
        public virtual Categories? Category { get; set; }
        [JsonIgnore]
        public virtual Stores? Store { get; set; }
        [JsonIgnore]
        public ICollection<Variants>? Variants { get; set; }
        [JsonIgnore]
        public ICollection<Prices>? Prices { get; set; }
        [JsonIgnore]
        public ICollection<ProductImages>? ProductImages { get; set; }
        [JsonIgnore]
        public ICollection<ProductVouchers>? ProductVouchers { get; set; }
        [JsonIgnore]
        public ICollection<Carts>? Carts { get; set; }
        [JsonIgnore]
        public ICollection<Reviews>? Reviews { get; set; }
        [JsonIgnore]
        public ICollection<OrderDetails>? OrderDetails { get; set; }
        [JsonIgnore]
        public ICollection<ProductVariants>? ProductVariants { get; set; }

    }
}