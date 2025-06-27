using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DATN_API.Models
{
    public class ReviewMedias
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Review")]
        public int ReviewId { get; set; }
        public string? Media { get; set; }

        public virtual Reviews? Review { get; set; }
    }
}
