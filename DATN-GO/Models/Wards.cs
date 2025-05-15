using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_API.Models
{
    public class Wards
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("District")]
        public int DistrictId { get; set; }

        [MaxLength(50)]
        public string WardName { get; set; }
        [JsonIgnore]
        public virtual Districts? District { get; set; }
    }
}
