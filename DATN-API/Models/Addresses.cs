using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_API.Models
{
    public class Addresses
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        public float Longitude { get; set; }
        public float Latitude { get; set; }

        [MaxLength]
        public string Discription { get; set; }

        [MaxLength(50)]
        public string Status { get; set; }
        [JsonIgnore]
        public virtual Users? User { get; set; }
        [JsonIgnore]
        public virtual Cities? City { get; set; }
    }
}
