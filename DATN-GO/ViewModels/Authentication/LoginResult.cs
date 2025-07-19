namespace DATN_GO.ViewModels.Authentication
{
    public class LoginResult
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string FullName { get; set; }
        public int Roles { get; set; }
        public string Token { get; set; }
    }
}
