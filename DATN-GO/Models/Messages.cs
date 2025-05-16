using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_GO.Models
{
    public class Messages
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SenderId { get; set; }

        [Required]
        public int ReceiverId { get; set; }

        public string Text { get; set; }

        public DateTime Timestamp { get; set; }
        [JsonIgnore]
        [ForeignKey("SenderId")]
        public virtual Users? Sender { get; set; }
        [JsonIgnore]
        [ForeignKey("ReceiverId")]
        public virtual Users? Receiver { get; set; }
    }
}
