using FarmTrackBE.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace FarmTrackBE.Middleware
{
    public class FirebaseAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public FirebaseAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (IsPublicPath(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var authHeader = context.Request.Headers["Authorization"].ToString();

            if (string.IsNullOrEmpty(authHeader))
            {
                await HandleUnauthorizedResponse(context, "Token di autenticazione mancante");
                return;
            }

            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                await HandleUnauthorizedResponse(context, "Il formato del token deve essere 'Bearer {token}'");
                return;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();

            try
            {
                var authService = context.RequestServices.GetRequiredService<FirebaseAuthService>();

                var uid = await authService.VerifyTokenAsync(token);

                context.Items["UserUID"] = uid;
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleUnauthorizedResponse(context, $"Errore di autenticazione: {ex.Message}");
            }
        }

        private bool IsPublicPath(PathString path)
        {
            return path.StartsWithSegments("/swagger") ||
                   path.StartsWithSegments("/api/Auth");
        }

        private async Task HandleUnauthorizedResponse(HttpContext context, string message)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

            var response = JsonSerializer.Serialize(new
            {
                error = "Unauthorized",
                message = message
            });

            await context.Response.WriteAsync(response);
        }
    }
}
