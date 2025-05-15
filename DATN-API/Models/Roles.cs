using System.Text.Json.Serialization;
using Twilio.TwiML.Messaging;

namespace DATN_API.Models
{
    public class Roles
    {
        public int Id { get; set; }
        public string RoleName { get; set; }
        public bool Status { get; set; }

        [JsonIgnore]
        public virtual ICollection<Users>? Users { get; set; }
    }
}
