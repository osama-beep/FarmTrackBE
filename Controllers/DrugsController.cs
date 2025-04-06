using FarmTrackBE.Models;
using FarmTrackBE.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace FarmTrackBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DrugsController : ControllerBase
    {
        private readonly DrugService _service;

        public DrugsController(DrugService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            var drugs = await _service.GetAllAsync(uid);
            return Ok(drugs);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            var drug = await _service.GetByIdAsync(id, uid);
            if (drug == null) return NotFound();

            return Ok(drug);
        }

        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStock()
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            var drugs = await _service.GetLowStockAsync(uid);
            return Ok(drugs);
        }

        [HttpGet("expiring")]
        public async Task<IActionResult> GetExpiring([FromQuery] int days = 30)
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            var drugs = await _service.GetExpiringAsync(uid, days);
            return Ok(drugs);
        }

        [HttpGet("expired")]
        public async Task<IActionResult> GetExpired()
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            var drugs = await _service.GetExpiredAsync(uid);
            return Ok(drugs);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Drug drug)
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            try
            {
                drug.UserUID = uid;
                await _service.AddAsync(drug);
                return Ok(drug);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] Drug drug)
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            try
            {
                // Verifica che il farmaco esista e appartenga all'utente
                var existingDrug = await _service.GetByIdAsync(id, uid);
                if (existingDrug == null)
                    return NotFound(new { message = "Farmaco non trovato o non autorizzato" });

                drug.Id = id;
                drug.UserUID = uid;
                await _service.UpdateAsync(id, drug);
                return Ok(drug);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}/quantity")]
        public async Task<IActionResult> UpdateQuantity(string id, [FromBody] UpdateQuantityRequest request)
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            try
            {
                await _service.UpdateQuantityAsync(id, request.Quantity, uid);
                var updatedDrug = await _service.GetByIdAsync(id, uid);
                return Ok(updatedDrug);
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
                var existingDrug = await _service.GetByIdAsync(id, uid);
                if (existingDrug == null)
                    return NotFound(new { message = "Farmaco non trovato o non autorizzato" });

                await _service.DeleteAsync(id);
                return Ok(new { message = "Farmaco eliminato con successo" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public class UpdateQuantityRequest
    {
        public int Quantity { get; set; }
    }
}
