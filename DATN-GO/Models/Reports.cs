using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_API.Models
{
    public class Reports
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        [ForeignKey("Diner")]
        public int DinerId { get; set; }

        [MaxLength(200)]
        public string Reanson { get; set; }

        public DateTime CreatedAt { get; set; }

        [MaxLength(50)]
        public virtual string Status { get; set; }
        [JsonIgnore]
        public virtual Users? User { get; set; }
        [JsonIgnore]
        public virtual Diners? Diner { get; set; }
        [JsonIgnore]
        public ICollection<ReportActions>? Actions { get; set; }
    }
}
