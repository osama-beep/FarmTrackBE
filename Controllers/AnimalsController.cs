using FarmTrackBE.Models;
using FarmTrackBE.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace FarmTrackBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnimalsController : ControllerBase
    {
        private readonly AnimalService _service;

        public AnimalsController(AnimalService animalService)
        {
            _service = animalService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            var animals = await _service.GetAllAsync(uid);
            return Ok(animals);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            var animal = await _service.GetByIdAsync(id, uid);
            if (animal == null) return NotFound();

            return Ok(animal);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Animal animal)
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            try
            {
                animal.UserUID = uid;
                await _service.AddAsync(animal);
                return Ok(animal);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] Animal animal)
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            try
            {
                // Verifica che l'animale esista e appartenga all'utente
                var existingAnimal = await _service.GetByIdAsync(id, uid);
                if (existingAnimal == null)
                    return NotFound(new { message = "Animale non trovato o non autorizzato" });

                animal.Id = id;
                animal.UserUID = uid;
                await _service.UpdateAsync(id, animal);
                return Ok(animal);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}/health-status")]
        public async Task<IActionResult> UpdateHealthStatus(string id, [FromBody] UpdateHealthStatusRequest request)
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            try
            {
                await _service.UpdateHealthStatusAsync(id, request.HealthStatus, uid);
                var updatedAnimal = await _service.GetByIdAsync(id, uid);
                return Ok(updatedAnimal);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}/weight")]
        public async Task<IActionResult> UpdateWeight(string id, [FromBody] UpdateWeightRequest request)
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            try
            {
                await _service.UpdateWeightAsync(id, request.Weight, uid);
                var updatedAnimal = await _service.GetByIdAsync(id, uid);
                return Ok(updatedAnimal);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}/age")]
        public async Task<IActionResult> UpdateAge(string id, [FromBody] UpdateAgeRequest request)
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            try
            {
                await _service.UpdateAgeAsync(id, request.Years, request.Months, uid);
                var updatedAnimal = await _service.GetByIdAsync(id, uid);
                return Ok(updatedAnimal);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("upload-image")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAnimalImage(IFormFile file, [FromForm] string animalId)
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

                // Verify that the animal exists and belongs to the user
                var existingAnimal = await _service.GetByIdAsync(animalId, uid);
                if (existingAnimal == null)
                    return NotFound(new { Message = "Animale non trovato o non autorizzato" });

                var imageKit = HttpContext.RequestServices.GetRequiredService<ImageKitUploadService>();
                var imageUrl = await imageKit.UploadImageAsync(uid, file);

                // Update the animal with the new image URL
                existingAnimal.ImageUrl = imageUrl;
                await _service.UpdateAsync(animalId, existingAnimal);

                return Ok(new
                {
                    Message = "Immagine caricata con successo",
                    ImageUrl = imageUrl,
                    Animal = existingAnimal
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }



        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            try
            {
                // Verifica che l'animale esista e appartenga all'utente
                var existingAnimal = await _service.GetByIdAsync(id, uid);
                if (existingAnimal == null)
                    return NotFound(new { message = "Animale non trovato o non autorizzato" });

                await _service.DeleteAsync(id);
                return Ok(new { message = "Animale eliminato con successo" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public class UpdateHealthStatusRequest
    {
        public string HealthStatus { get; set; }
    }

    public class UpdateWeightRequest
    {
        public double Weight { get; set; }
    }

    public class UpdateAgeRequest
    {
        public int Years { get; set; }
        public int Months { get; set; }
    }
}
