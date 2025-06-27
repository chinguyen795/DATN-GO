using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_API.Models
{
    public enum MessageStatus
    {
        [Display(Name = "Chưa nhận")]
        NotReceived,
        [Display(Name = "Đã nhận")]
        Received
    }

    public class Messages
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SenderId { get; set; }

        [Required]
        public int ReceiverId { get; set; }

        [Required]
        public string? Text { get; set; }

        public MessageStatus Status { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        [JsonIgnore]
        [ForeignKey("SenderId")]
        public virtual Users? Sender { get; set; }

        [JsonIgnore]
        [ForeignKey("ReceiverId")]
        public virtual Users? Receiver { get; set; }

        [JsonIgnore]
        public ICollection<MessageMedias>? MessageMedias { get; set; }
    }
}
