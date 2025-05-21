using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Twilio.TwiML.Voice;
using System.Text.Json.Serialization;

namespace DATN_GO.Models
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
        public string DinerAddress { get; set; }

        public float Longitude { get; set; }
        public float Latitude { get; set; }

        [MaxLength(50)]
        public string Avatar { get; set; }

        [MaxLength(50)]
        public string Status { get; set; }

        [MaxLength(50)]
        public string CoverPhoto { get; set; }

        public int OpenHouse { get; set; }
        public int OpenMinute { get; set; }
        public int CloseHouse { get; set; }
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

    public class DinnerModel
    {
        public int? Id { get; set; }
        [MaxLength(50)]
        public string DinerName { get; set; }

        [MaxLength(50)]
        public string DinerAddress { get; set; }

        public float Longitude { get; set; }
        public float Latitude { get; set; }

        [MaxLength(50)]
        public string Avatar { get; set; } = "no";

        [MaxLength(50)]
        public string Status { get; set; } = "active";

        [MaxLength(50)]
        public string CoverPhoto { get; set; } = "no";

        [Required]
        [Range(0, 23)]
        public int OpenHouse { get; set; } = 8;
        [Range(0, 59)]
        public int OpenMinute { get; set; } = 0;
        [Required]
        [Range(0, 23)]
        public int CloseHouse { get; set; } = 22;
        [Range(0, 59)]
        public int CloseMinute { get; set; } = 0;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
    }
}
