using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_API.Models
{
    public class AdminSettings
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        [JsonIgnore]
        public virtual Users? User { get; set; }

        [JsonIgnore]
        public ICollection<Policies>? Policies { get; set; }
        [JsonIgnore]
        public ICollection<Decorates>? Decorates { get; set; }

        public string? Theme { get; set; }
        public string? Logo { get; set; }
        public DateTime? UpdateAt { get; set; }
    }
}
