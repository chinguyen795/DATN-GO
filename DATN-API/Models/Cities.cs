using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json.Serialization;

namespace DATN_API.Models
{
    public class Cities
    {
        [Key]
        public int Id { get; set; }
        [MaxLength(50)]
        public string CityName { get; set; }

        // 1-1 với Address
        [JsonIgnore]
        public virtual Addresses? Address { get; set; }
        [JsonIgnore]

        public ICollection<Districts>? Districts { get; set; }
    }
}
