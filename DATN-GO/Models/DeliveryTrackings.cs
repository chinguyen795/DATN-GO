using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_GO.Models
{
    public class DeliveryTrackings
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Order")]
        public int OrderId { get; set; }

        [MaxLength(200)]
        public string AhamoveOrderId { get; set; }

        [MaxLength(50)]
        public string ServiceId { get; set; }

        public string TrackingUrl { get; set; }

        [MaxLength(100)]
        public string DriverName { get; set; }

        [MaxLength(20)]
        public string DriverPhone { get; set; }

        [MaxLength(50)]
        public string EstimatedTime { get; set; }

        [MaxLength(50)]
        public string Status { get; set; }

        public DateTime CreateAt { get; set; }
        [JsonIgnore]
        public virtual Orders? Order { get; set; }
    }
}
