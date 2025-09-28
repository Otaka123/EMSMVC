namespace EMSMVC.Models
{
    public class LoginDTO
    {
        public string AccessToken { get; set; }
        public DateTime AccessTokenExpiresAt { get; }
        public DateTime RefreshTokenExpiresAt { get; }

        public string RefreshToken { get; set; }
        public LoginDTO() { } // لازم يكون موجود

        public LoginDTO(string accessToken, string refreshToken)
        {
            this.AccessToken = accessToken;
            this.RefreshToken = refreshToken;
            this.AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(30);
            this.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);

        }
    }
}
