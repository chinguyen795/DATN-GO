using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_GO.Models
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
    }
}
