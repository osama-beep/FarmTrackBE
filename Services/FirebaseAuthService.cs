using FirebaseAdmin;
using FirebaseAdmin.Auth;
using FarmTrackBE.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<FirebaseAuthService> _logger;

        public FirebaseAuthService(IConfiguration configuration, ILogger<FirebaseAuthService> logger = null)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
            _logger = logger;
        }

        public async Task<string> VerifyTokenAsync(string token)
        {
            try
            {
                EnsureFirebaseInitialized();

                _logger?.LogInformation($"Verifica token: {token.Substring(0, Math.Min(20, token.Length))}...");
                var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
                _logger?.LogInformation($"Token verificato per UID: {decodedToken.Uid}");
                return decodedToken.Uid;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Errore nella verifica del token");
                throw;
            }
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                EnsureFirebaseInitialized();

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
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Errore durante la registrazione dell'utente");
                throw;
            }
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                EnsureFirebaseInitialized();

                var apiKey = _configuration["Firebase:ApiKey"] ??
                     _configuration["Firebase__ApiKey"] ??
                     Environment.GetEnvironmentVariable("FIREBASE_API_KEY");

                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger?.LogError("Firebase API Key non trovata. Controlla le variabili d'ambiente.");
                    throw new InvalidOperationException("Firebase API Key non configurata");
                }
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
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Errore durante il login dell'utente");
                throw;
            }
        }

        private async Task<string> ExchangeCustomTokenForIdToken(string customToken)
        {
            var apiKey = _configuration["Firebase:ApiKey"] ??
                 _configuration["Firebase__ApiKey"] ??
                 Environment.GetEnvironmentVariable("FIREBASE_API_KEY");

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger?.LogError("Firebase API Key non trovata. Controlla le variabili d'ambiente.");
                throw new InvalidOperationException("Firebase API Key non configurata");
            }

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

        private void EnsureFirebaseInitialized()
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                _logger?.LogError("Firebase non è stato inizializzato correttamente");
                throw new InvalidOperationException("Firebase non è stato inizializzato. Verificare la configurazione.");
            }

            if (FirebaseAuth.DefaultInstance == null)
            {
                _logger?.LogError("FirebaseAuth non è disponibile");
                throw new InvalidOperationException("FirebaseAuth non è disponibile. Verificare la configurazione Firebase.");
            }
        }
    }
}
