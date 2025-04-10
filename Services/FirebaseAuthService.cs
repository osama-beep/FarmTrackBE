using FirebaseAdmin.Auth;
using FarmTrackBE.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Cloud.Firestore;

namespace FarmTrackBE.Services
{
    public class FirebaseAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly FirestoreDb _db;
        private const string UsersCollection = "users";

        public FirebaseAuthService(IConfiguration configuration, FirestoreDb firestoreDb)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
            _db = firestoreDb;
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
            try
            {
                // Crea l'utente in Firebase Auth
                var userArgs = new UserRecordArgs
                {
                    Email = request.Email,
                    Password = request.Password,
                    DisplayName = request.DisplayName ?? $"{request.FirstName} {request.LastName}",
                    EmailVerified = false
                };

                var userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(userArgs);

                // Crea il profilo utente in Firestore
                var user = new User
                {
                    Id = userRecord.Uid,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    Phone = request.Phone,
                    FarmName = request.FarmName,
                    DisplayName = request.DisplayName ?? $"{request.FirstName} {request.LastName}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _db.Collection(UsersCollection).Document(userRecord.Uid).SetAsync(user);

                // Genera token per l'utente
                var customToken = await FirebaseAuth.DefaultInstance.CreateCustomTokenAsync(userRecord.Uid);
                var authResult = await ExchangeCustomTokenForIdTokenAndRefreshToken(customToken);

                return new AuthResponse
                {
                    Token = authResult.IdToken,
                    RefreshToken = authResult.RefreshToken,
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
                Console.WriteLine($"Errore nella registrazione: {ex.Message}");
                throw;
            }
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
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

                // Verifica se esiste un profilo utente in Firestore, altrimenti crealo
                await EnsureUserProfileExistsAsync(userRecord.Uid, userRecord.Email, userRecord.DisplayName);

                return new AuthResponse
                {
                    Token = authResult.IdToken,
                    RefreshToken = authResult.RefreshToken,
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
                Console.WriteLine($"Errore nel login: {ex.Message}");
                throw;
            }
        }

        // Nuovo metodo per il refresh del token
        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var apiKey = _configuration["Firebase:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                    throw new InvalidOperationException("Firebase API Key non configurata");

                var content = new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        grant_type = "refresh_token",
                        refresh_token = refreshToken
                    }),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(
                    $"https://securetoken.googleapis.com/v1/token?key={apiKey}",
                    content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Errore nel refresh del token: {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var refreshResult = JsonSerializer.Deserialize<RefreshTokenResponse>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // Ottieni informazioni utente dal token
                var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(refreshResult.IdToken);
                var userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(decodedToken.Uid);

                return new AuthResponse
                {
                    Token = refreshResult.IdToken,
                    RefreshToken = refreshResult.RefreshToken,
                    ExpiresIn = refreshResult.ExpiresIn,
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
                Console.WriteLine($"Errore nel refresh del token: {ex.Message}");
                throw;
            }
        }

        private async Task<FirebaseAuthResponse> ExchangeCustomTokenForIdTokenAndRefreshToken(string customToken)
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

            return authResult;
        }

        // Per retrocompatibilità, manteniamo anche il vecchio metodo
        private async Task<string> ExchangeCustomTokenForIdToken(string customToken)
        {
            var authResult = await ExchangeCustomTokenForIdTokenAndRefreshToken(customToken);
            return authResult.IdToken;
        }

        // Metodi per la gestione del profilo utente
        private async Task EnsureUserProfileExistsAsync(string uid, string email, string displayName)
        {
            try
            {
                var userDoc = await _db.Collection(UsersCollection).Document(uid).GetSnapshotAsync();

                if (!userDoc.Exists)
                {
                    // Crea un profilo utente base se non esiste
                    var user = new User
                    {
                        Id = uid,
                        Email = email,
                        DisplayName = displayName ?? email,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _db.Collection(UsersCollection).Document(uid).SetAsync(user);
                    Console.WriteLine($"Creato profilo utente per {email}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nella verifica/creazione del profilo utente: {ex.Message}");
                // Non lanciare l'eccezione per non interrompere il flusso di login
            }
        }

        public async Task<User> GetUserProfileAsync(string uid)
        {
            try
            {
                var userDoc = await _db.Collection(UsersCollection).Document(uid).GetSnapshotAsync();

                if (userDoc.Exists)
                {
                    var user = userDoc.ConvertTo<User>();
                    user.Id = uid;
                    return user;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nel recupero del profilo utente: {ex.Message}");
                throw;
            }
        }

        public async Task<User> UpdateUserProfileAsync(string uid, UserUpdateRequest request)
        {
            try
            {
                // Ottieni il profilo utente corrente
                var userDoc = await _db.Collection(UsersCollection).Document(uid).GetSnapshotAsync();
                if (!userDoc.Exists)
                {
                    throw new Exception("Profilo utente non trovato");
                }
                var user = userDoc.ConvertTo<User>();
                user.Id = uid;

                // Aggiorna i campi forniti
                if (!string.IsNullOrEmpty(request.FirstName))
                    user.FirstName = request.FirstName;
                if (!string.IsNullOrEmpty(request.LastName))
                    user.LastName = request.LastName;
                if (!string.IsNullOrEmpty(request.Phone))
                    user.Phone = request.Phone;
                if (!string.IsNullOrEmpty(request.FarmName))
                    user.FarmName = request.FarmName;
                if (!string.IsNullOrEmpty(request.DisplayName))
                {
                    user.DisplayName = request.DisplayName;
                    // Aggiorna anche il displayName in Firebase Auth
                    await FirebaseAuth.DefaultInstance.UpdateUserAsync(new UserRecordArgs
                    {
                        Uid = uid,
                        DisplayName = request.DisplayName
                    });
                }

                // Modifica qui: aggiorna ProfileImage solo se è esplicitamente incluso nella richiesta
                // Il campo request.ProfileImage può essere null o vuoto, ma non dovrebbe sovrascrivere
                // il valore esistente a meno che non sia esplicitamente incluso nella richiesta
                if (request.ProfileImage != null)  // Cambiato da !string.IsNullOrEmpty a null check
                    user.ProfileImage = request.ProfileImage;  // Può essere una stringa vuota per rimuovere l'immagine

                user.UpdatedAt = DateTime.UtcNow;

                // Salva le modifiche
                await _db.Collection(UsersCollection).Document(uid).SetAsync(user);
                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nell'aggiornamento del profilo utente: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteUserAsync(string uid)
        {
            try
            {
                // Elimina il profilo utente da Firestore
                await _db.Collection(UsersCollection).Document(uid).DeleteAsync();

                // Elimina l'utente da Firebase Auth
                await FirebaseAuth.DefaultInstance.DeleteUserAsync(uid);

                Console.WriteLine($"Utente {uid} eliminato con successo");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nell'eliminazione dell'utente: {ex.Message}");
                throw;
            }
        }
    }

    // Classe per la risposta del refresh token
    public class RefreshTokenResponse
    {
        public string IdToken { get; set; }
        public string RefreshToken { get; set; }
        public string ExpiresIn { get; set; }
        public string TokenType { get; set; }
        public string UserId { get; set; }
    }

    // Aggiorna la classe FirebaseAuthResponse per includere il refresh token
    public class FirebaseAuthResponse
    {
        public string IdToken { get; set; }
        public string Email { get; set; }
        public string RefreshToken { get; set; }
        public string ExpiresIn { get; set; }
        public string LocalId { get; set; }
        public bool Registered { get; set; }
    }

    // Aggiorna la classe AuthResponse per includere il refresh token
    public class AuthResponse
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public string ExpiresIn { get; set; }
        public UserInfo User { get; set; }
    }
}
