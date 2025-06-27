using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_GO.Models
{
    public class RatingStores
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        [JsonIgnore]
        public virtual Users? User { get; set; }

        [ForeignKey("Store")]
        public int StoreId { get; set; }
        [JsonIgnore]
        public virtual Stores? Store { get; set; }

        public int Rating { get; set; }
        public DateTime CreateAt { get; set; }
    }
}
