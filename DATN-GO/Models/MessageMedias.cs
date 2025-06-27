using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DATN_GO.Models
{
    public class MessageMedias
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Messages")]
        public int MessageId { get; set; }
        public virtual Messages? Message { get; set; }

        public string? Media { get; set; }
    }
}
