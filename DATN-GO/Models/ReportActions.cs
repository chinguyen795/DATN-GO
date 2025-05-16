using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Twilio.TwiML.Voice;
using System.Text.Json.Serialization;

namespace DATN_GO.Models
{
    public class ReportActions
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Report")]
        public int ReportId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; } // admin xử lý

        public string Action { get; set; }

        public string Notes { get; set; }

        public DateTime CreatedAt { get; set; }
        [JsonIgnore]
        public virtual Reports? Report { get; set; }
        [JsonIgnore]
        public virtual Users? User { get; set; }
    }
}
