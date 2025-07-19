namespace DATN_GO.ViewModels.Authentication
{
    public class ChangeEmailRequest
    {
        public int UserId { get; set; }
        public string NewEmail { get; set; }
        public string OtpCode { get; set; }
    }
}

