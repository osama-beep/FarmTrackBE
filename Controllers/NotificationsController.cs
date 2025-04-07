using FarmTrackBE.Models;
using FarmTrackBE.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace FarmTrackBE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly NotificationService _service;

        public NotificationsController()
        {
            _service = new NotificationService();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            var notifications = await _service.GetAllAsync(uid);
            return Ok(notifications);
        }

        [HttpGet("unread")]
        public async Task<IActionResult> GetUnread()
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            var notifications = await _service.GetUnreadAsync(uid);
            return Ok(notifications);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            var notification = await _service.GetByIdAsync(id, uid);
            if (notification == null) return NotFound();

            return Ok(notification);
        }

        [HttpPost("mark-read/{id}")]
        public async Task<IActionResult> MarkAsRead(string id)
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            await _service.MarkAsReadAsync(id, uid);
            return Ok();
        }

        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            await _service.MarkAllAsReadAsync(uid);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            await _service.DeleteAsync(id, uid);
            return Ok();
        }

        [HttpPost("check-drug-notifications")]
        public async Task<IActionResult> CheckDrugNotifications()
        {
            var uid = HttpContext.Items["UserUID"]?.ToString();
            if (uid == null) return Unauthorized();

            await _service.CheckAndCreateDrugExpirationNotifications(uid);
            return Ok();
        }
    }
}
