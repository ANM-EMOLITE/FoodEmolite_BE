namespace FoodEmolite.Application.DTOs.Auth
{
    public class ChangePasswordRequest
    {
        public string OldPassword { get; set; }

        public string NewPassword { get; set; }

        public string ConfirmNewPassword { get; set; }
    }
}
