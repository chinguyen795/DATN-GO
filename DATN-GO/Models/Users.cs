using System.Text.Json.Serialization;

namespace DATN_API.Models
{
    public class Users
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Avatar { get; set; }
        public bool Status { get; set; }
        public bool Gender { get; set; }
        public string CitizenIdentityCard { get; set; }
        public DateTime CreatedAt { get; set; }


        [JsonIgnore]
        public virtual Roles? Role { get; set; }
    }
}
