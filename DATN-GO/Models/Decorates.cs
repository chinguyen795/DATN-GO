using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_GO.Models
{
    public class Decorates
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        [MaxLength(50)]
        public string Title { get; set; }

        [MaxLength(50)]
        public string Image { get; set; }
        [JsonIgnore]
        public virtual Users? User { get; set; }

        [ForeignKey("AdminSetting")]
        public int? AdminSettingId { get; set; }
        [JsonIgnore]
        public virtual AdminSettings? AdminSetting { get; set; }
    }
}
