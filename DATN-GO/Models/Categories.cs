using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_GO.Models
{
    public class Categories
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(50)]
        public string CategoryName { get; set; }

        [MaxLength(50)]
        public string Type { get; set; }
        [JsonIgnore]
        public ICollection<Products>? Products { get; set; }
    }
}
