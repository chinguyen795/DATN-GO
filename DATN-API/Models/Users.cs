using Microsoft.Extensions.Hosting;
using System.Net;
using System.Text.Json.Serialization;
using Twilio.TwiML.Messaging;
using Twilio.TwiML.Voice;

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
        public DateTime? DateOfBirth { get; set; }


        [JsonIgnore]
        public virtual Roles? Role { get; set; }
        [JsonIgnore]
        public ICollection<Addresses>? Address { get; set; }
        [JsonIgnore]
        public ICollection<Messages>? SentMessages { get; set; }
        [JsonIgnore]
        public ICollection<Messages>? ReceivedMessages { get; set; }
        [JsonIgnore]
        public ICollection<Posts>? Posts { get; set; }
        [JsonIgnore]
        public ICollection<Decorates>? Decorates { get; set; }
        [JsonIgnore]
        public virtual Diners? Diner { get; set; }
        [JsonIgnore]
        public ICollection<Reports>? ReportsSent { get; set; }
        [JsonIgnore]
        public ICollection<ReportActions>? ReportActionsPerformed { get; set; }
        [JsonIgnore]
        public ICollection<Carts>? Carts { get; set; }
        [JsonIgnore]
        public ICollection<Orders>? Orders { get; set; }
        [JsonIgnore]
        public ICollection<Reviews>? Reviews { get; set; }


    }
}
