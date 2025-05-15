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
        public string DinerAddress { get; set; }

        public float Longitude { get; set; }
        public float Latitude { get; set; }

        [MaxLength(50)]
        public string Avatar { get; set; }

        [MaxLength(50)]
        public string Status { get; set; }

        [MaxLength(50)]
        public string CoverPhoto { get; set; }

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
