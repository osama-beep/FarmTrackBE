using Google.Cloud.Firestore;
using System;

namespace FarmTrackBE.Models
{
    public class RegisterRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Phone { get; set; }
        public string FarmName { get; set; }
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

    [FirestoreData]
    public class User
    {
        [FirestoreDocumentId]
        public string Id { get; set; }

        [FirestoreProperty("firstName")]
        public string FirstName { get; set; }

        [FirestoreProperty("lastName")]
        public string LastName { get; set; }

        [FirestoreProperty("email")]
        public string Email { get; set; }

        [FirestoreProperty("phone")]
        public string Phone { get; set; }

        [FirestoreProperty("farmName")]
        public string FarmName { get; set; }

        [FirestoreProperty("displayName")]
        public string DisplayName { get; set; }

        [FirestoreProperty("profileImage")]
        public string ProfileImage { get; set; }

        [FirestoreProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [FirestoreProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }

    public class UserUpdateRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        public string FarmName { get; set; }
        public string DisplayName { get; set; }
        public string? ProfileImage { get; set; }
    }
}
