using FarmTrackBE.Models;
using FarmTrackBE.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FarmTrackBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TreatmentsController : ControllerBase
    {
        private readonly TreatmentService _service;

        public TreatmentsController(TreatmentService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            var treatments = await _service.GetAllAsync(uid);
            return Ok(treatments);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            var treatment = await _service.GetByIdAsync(id, uid);
            if (treatment == null) return NotFound();

            return Ok(treatment);
        }

        [HttpGet("animal/{animalId}")]
        public async Task<IActionResult> GetByAnimalId(string animalId)
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            var treatments = await _service.GetByAnimalIdAsync(animalId, uid);
            return Ok(treatments);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Treatment treatment)
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            try
            {
                treatment.UserUID = uid;

                // Inizializza la lista dei follow-up se non è già inizializzata
                if (treatment.FollowUps == null)
                {
                    treatment.FollowUps = new List<TreatmentFollowUp>();
                }

                await _service.AddAsync(treatment);
                return Ok(treatment);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] Treatment treatment)
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            try
            {
                // Verifica che il trattamento esista e appartenga all'utente
                var existingTreatment = await _service.GetByIdAsync(id, uid);
                if (existingTreatment == null)
                    return NotFound(new { message = "Trattamento non trovato o non autorizzato" });

                treatment.Id = id;
                treatment.UserUID = uid;

                // Assicurati che la lista dei follow-up non sia null
                if (treatment.FollowUps == null)
                {
                    treatment.FollowUps = new List<TreatmentFollowUp>();
                }

                await _service.UpdateAsync(id, treatment);
                return Ok(treatment);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}/complete")]
        public async Task<IActionResult> Complete(string id, [FromBody] CompleteRequest request)
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            try
            {
                await _service.CompleteAsync(id, request.Outcome, uid);
                var updatedTreatment = await _service.GetByIdAsync(id, uid);
                return Ok(updatedTreatment);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/follow-up")]
        public async Task<IActionResult> AddFollowUp(string id, [FromBody] AddFollowUpRequest request)
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            try
            {
                await _service.AddFollowUpAsync(id, request.ScheduledDate, request.Description, uid);
                var updatedTreatment = await _service.GetByIdAsync(id, uid);
                return Ok(updatedTreatment);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }



        [HttpGet("{id}/follow-ups")]
        public async Task<IActionResult> GetFollowUps(string id)
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            try
            {
                var treatment = await _service.GetByIdAsync(id, uid);
                if (treatment == null)
                    return NotFound(new { message = "Trattamento non trovato o non autorizzato" });

                return Ok(treatment.FollowUps);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/follow-ups")]
        public async Task<IActionResult> UpdateFollowUps(string id, [FromBody] List<TreatmentFollowUp> followUps)
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            try
            {
                var treatment = await _service.GetByIdAsync(id, uid);
                if (treatment == null)
                    return NotFound(new { message = "Trattamento non trovato o non autorizzato" });

                treatment.FollowUps = followUps;
                await _service.UpdateAsync(id, treatment);

                return Ok(treatment);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            try
            {
                // Verifica che il trattamento esista e appartenga all'utente
                var existingTreatment = await _service.GetByIdAsync(id, uid);
                if (existingTreatment == null)
                    return NotFound(new { message = "Trattamento non trovato o non autorizzato" });

                await _service.DeleteAsync(id);
                return Ok(new { message = "Trattamento eliminato con successo" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public class CompleteRequest
    {
        public string Outcome { get; set; }
    }

    public class AddFollowUpRequest
    {
        public DateTime ScheduledDate { get; set; }
        public string Description { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletionDate { get; set; }
        public string Notes { get; set; }
    }
}


