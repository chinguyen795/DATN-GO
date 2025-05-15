using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_API.Models
{
    public class Posts
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        [MaxLength(50)]
        public string Content { get; set; }

        [MaxLength(50)]
        public string Image { get; set; }

        public DateTime CreateAt { get; set; }
        [JsonIgnore]
        public virtual Users? User { get; set; }
    }
}
