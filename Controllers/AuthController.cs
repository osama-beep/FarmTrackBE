using FarmTrackBE.Models;
using FarmTrackBE.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace FarmTrackBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly FirebaseAuthService _authService;

        public AuthController(FirebaseAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { Message = "Email e password sono obbligatori" });
                }

                var result = await _authService.RegisterAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { Message = "Email e password sono obbligatori" });
                }

                var result = await _authService.LoginAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // Nuovo endpoint per il refresh del token
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.RefreshToken))
                {
                    return BadRequest(new { Message = "Refresh token è obbligatorio" });
                }

                var result = await _authService.RefreshTokenAsync(request.RefreshToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // Endpoint per il profilo utente
        [HttpGet("profile")]
        public async Task<IActionResult> GetUserProfile()
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            try
            {
                var userProfile = await _authService.GetUserProfileAsync(uid);
                if (userProfile == null)
                {
                    return NotFound(new { Message = "Profilo utente non trovato" });
                }
                return Ok(userProfile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UserUpdateRequest request)
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            try
            {
                var updatedProfile = await _authService.UpdateUserProfileAsync(uid, request);
                return Ok(updatedProfile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        [HttpPost("profile/image")]
        [Consumes("multipart/form-data")] // <-- IMPORTANTE per Swagger!
        public async Task<IActionResult> UploadProfileImage(IFormFile file)
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { Message = "Nessun file caricato" });

                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
                if (!allowedTypes.Contains(file.ContentType.ToLower()))
                    return BadRequest(new { Message = "Formato file non supportato" });

                if (file.Length > 5 * 1024 * 1024)
                    return BadRequest(new { Message = "Il file è troppo grande. Max 5MB" });

                var imageKit = HttpContext.RequestServices.GetRequiredService<ImageKitUploadService>();
                var imageUrl = await imageKit.UploadImageAsync(uid, file);

                var updateRequest = new UserUpdateRequest { ProfileImage = imageUrl };
                var updatedProfile = await _authService.UpdateUserProfileAsync(uid, updateRequest);

                return Ok(new
                {
                    Message = "Immagine caricata con successo",
                    ProfileImage = imageUrl,
                    User = updatedProfile
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        [HttpDelete("account")]
        public async Task<IActionResult> DeleteAccount()
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            try
            {
                await _authService.DeleteUserAsync(uid);
                return Ok(new { Message = "Account eliminato con successo" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }
    }

    // Classe per la richiesta di refresh token
    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; }
    }
}
