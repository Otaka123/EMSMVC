namespace EMSMVC.Models
{
    public class LoginRequest
    {

        public string UserNameOrEmailOrPhone { get; set; }
        public string Password { get; set; }
        public bool IsPersistent { get; set; }

    }
}
