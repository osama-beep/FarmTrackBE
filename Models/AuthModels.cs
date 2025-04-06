namespace FarmTrackBE.Models
{
    public class RegisterRequest
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string DisplayName { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class FirebaseAuthResponse
    {
        public string IdToken { get; set; }
        public string RefreshToken { get; set; }
        public string ExpiresIn { get; set; }
        public string LocalId { get; set; }
    }

    public class TokenRequest
    {
        public string Token { get; set; }
    }

    public class UserInfo
    {
        public string Uid { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
    }

    public class AuthResponse
    {
        public string Token { get; set; }
        public string ExpiresIn { get; set; }
        public UserInfo User { get; set; }
    }
}
