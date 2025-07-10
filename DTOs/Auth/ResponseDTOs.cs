namespace SecretVaultManager.DTOs.Auth
{
    public class SignUpResponse
    {
        public Guid Id { get; set; }
    }
    public class SignInResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime AccessTokenExpiration { get; set; }
        public DateTime RefreshTokenExpiration { get; set; }
    }
}
