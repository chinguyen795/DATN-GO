using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_API.Models
{
    public class Policies
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("AdminSetting")]
        public int AdminSettingId { get; set; }
        [JsonIgnore]
        public virtual AdminSettings? AdminSetting { get; set; }
        // Thêm các trường khác nếu cần
    }
}
