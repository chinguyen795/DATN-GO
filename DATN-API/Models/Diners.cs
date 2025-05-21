using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Twilio.TwiML.Voice;
using System.Text.Json.Serialization;

namespace DATN_API.Models
{
    public class Diners
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        [MaxLength(50)]
        public string DinerName { get; set; }

        [MaxLength(50)]
        public string DinerAddress { get; set; } // số N, Đường XX, phường ac, quận mm, tp ABC

        public float Longitude { get; set; }
        public float Latitude { get; set; }

        public string Avatar { get; set; }

        [MaxLength(50)]
        public string Status { get; set; }

        public string CoverPhoto { get; set; }

        [Required]
        [Range(0, 23)]
        public int OpenHouse { get; set; }
        [Required]
        [Range(0, 59)]
        public int OpenMinute { get; set; }
        [Required]
        [Range(0, 23)]
        public int CloseHouse { get; set; }
        [Required]
        [Range(0, 59)]
        public int CloseMinute { get; set; }

        public DateTime CreateAt { get; set; }

        [JsonIgnore]
        public virtual Users User { get; set; }
        [JsonIgnore]
        public ICollection<Reports>? ReportsReceived { get; set; }
        [JsonIgnore]
        public ICollection<Products>? Products { get; set; }
        [JsonIgnore]
        public ICollection<ShippingMethods>? ShippingMethods { get; set; }

    }
}
