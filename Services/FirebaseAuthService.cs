using FirebaseAdmin.Auth;
using FarmTrackBE.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FarmTrackBE.Services
{
    public class FirebaseAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public FirebaseAuthService(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
        }

        public async Task<string> VerifyTokenAsync(string token)
        {
            try
            {
                Console.WriteLine($"Verifica token: {token.Substring(0, Math.Min(20, token.Length))}...");
                var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
                Console.WriteLine($"Token verificato per UID: {decodedToken.Uid}");
                return decodedToken.Uid;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nella verifica del token: {ex.Message}");
                throw;
            }
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var userArgs = new UserRecordArgs
            {
                Email = request.Email,
                Password = request.Password,
                DisplayName = request.DisplayName,
                EmailVerified = false
            };

            var userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(userArgs);
            var customToken = await FirebaseAuth.DefaultInstance.CreateCustomTokenAsync(userRecord.Uid);
            var idToken = await ExchangeCustomTokenForIdToken(customToken);

            return new AuthResponse
            {
                Token = idToken,
                ExpiresIn = null,
                User = new UserInfo
                {
                    Uid = userRecord.Uid,
                    Email = userRecord.Email,
                    DisplayName = userRecord.DisplayName
                }
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var apiKey = _configuration["Firebase:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("Firebase API Key non configurata");

            var content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    email = request.Email,
                    password = request.Password,
                    returnSecureToken = true
                }),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiKey}",
                content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Credenziali non valide. Firebase response: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var authResult = JsonSerializer.Deserialize<FirebaseAuthResponse>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(authResult.LocalId);

            return new AuthResponse
            {
                Token = authResult.IdToken,
                ExpiresIn = authResult.ExpiresIn,
                User = new UserInfo
                {
                    Uid = userRecord.Uid,
                    Email = userRecord.Email,
                    DisplayName = userRecord.DisplayName
                }
            };
        }

        private async Task<string> ExchangeCustomTokenForIdToken(string customToken)
        {
            var apiKey = _configuration["Firebase:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("Firebase API Key non configurata");

            var content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    token = customToken,
                    returnSecureToken = true
                }),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithCustomToken?key={apiKey}",
                content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Errore nello scambio del token: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var authResult = JsonSerializer.Deserialize<FirebaseAuthResponse>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return authResult.IdToken;
        }
    }
}
