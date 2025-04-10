using FarmTrackBE.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace FarmTrackBE.Middleware
{
    public class TokenRefreshMiddleware
    {
        private readonly RequestDelegate _next;

        public TokenRefreshMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, FirebaseAuthService authService)
        {
            // Estrai il token dall'header Authorization
            string authHeader = context.Request.Headers["Authorization"];
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                string token = authHeader.Substring("Bearer ".Length);

                // Verifica se il token è valido e se sta per scadere
                try
                {
                    // Estrai il refresh token dai cookie o da un altro header
                    if (context.Request.Cookies.TryGetValue("refreshToken", out string refreshToken) &&
                        IsTokenAboutToExpire(token))
                    {
                        // Esegui il refresh del token
                        var refreshResponse = await authService.RefreshTokenAsync(refreshToken);

                        // Aggiorna il token nell'header per la richiesta corrente
                        context.Request.Headers["Authorization"] = $"Bearer {refreshResponse.Token}";

                        // Aggiorna il cookie del refresh token
                        context.Response.Cookies.Append("refreshToken", refreshResponse.RefreshToken, new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Strict,
                            Expires = DateTimeOffset.UtcNow.AddDays(14)
                        });

                        // Aggiungi un header alla risposta con il nuovo token
                        // Il client può leggere questo header e aggiornare il token memorizzato
                        context.Response.Headers.Add("X-New-Token", refreshResponse.Token);
                    }
                }
                catch (Exception ex)
                {
                    // Log dell'errore ma continua con la richiesta
                    Console.WriteLine($"Errore nel refresh automatico del token: {ex.Message}");
                }
            }

            await _next(context);
        }

        private bool IsTokenAboutToExpire(string token)
        {
            try
            {
                // Decodifica il token JWT per ottenere la data di scadenza
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                // Ottieni la data di scadenza
                var expiry = jwtToken.ValidTo;

                // Verifica se il token scadrà nei prossimi 5 minuti
                return expiry < DateTime.UtcNow.AddMinutes(5);
            }
            catch
            {
                // In caso di errore nella decodifica, considera il token come valido
                return false;
            }
        }
    }

    // Extension method per aggiungere il middleware alla pipeline
    public static class TokenRefreshMiddlewareExtensions
    {
        public static IApplicationBuilder UseTokenRefresh(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TokenRefreshMiddleware>();
        }
    }
}
