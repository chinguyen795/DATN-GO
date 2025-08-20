using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using DATN_GO.Models;

namespace DATN_GO.Models
{
    public class Reviews
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        [ForeignKey("Product")]
        public int ProductId { get; set; }
        [ForeignKey("Order")]
        public int OrderId { get; set; }
        public int Rating { get; set; }

        public string CommentText { get; set; }

        public DateTime CreateAt { get; set; }
        public DateTime UpdateAt { get; set; }
        [JsonIgnore]
        public virtual Users? User { get; set; }
        [JsonIgnore]
        public virtual Products? Product { get; set; }
        [JsonIgnore]
        public virtual Orders? Order { get; set; }
        public virtual ICollection<ReviewMedias>? ReviewMedias { get; set; }

    }

}