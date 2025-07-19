namespace DATN_GO.ViewModels.Authentication
{
    public class ChangePasswordWithIdentifierRequest
    {
        public string Identifier { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmNewPassword { get; set; }
    }
}
