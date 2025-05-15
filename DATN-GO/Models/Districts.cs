using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_API.Models
{
    public class Districts
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("City")]
        public int CityId { get; set; }

        [MaxLength(50)]
        public string DistrictName { get; set; }
        [JsonIgnore]
        public virtual Cities? City { get; set; }
        [JsonIgnore]
        public ICollection<Wards>? Wards { get; set; }
    }
}
