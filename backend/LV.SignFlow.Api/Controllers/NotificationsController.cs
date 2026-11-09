using LV.SignFlow.Application.Notifications;
using LV.SignFlow.Application.Notifications.Dtos;
using LV.SignFlow.Application.Notifications.Requests;
using LV.SignFlow.Domain.Enums.NotificationEnums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LV.SignFlow.Api.Controllers
{
    [Route("api/notifications")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly INotificationPreferenceService _preferenceService;
        public NotificationsController(
            INotificationService notificationService, INotificationPreferenceService preferenceService)
        {
            _notificationService = notificationService;
            _preferenceService = preferenceService;
        }

        [HttpGet("user/{userId:guid}")]
        public async Task<ActionResult<IReadOnlyList<NotificationDto>>>
            GetUserNotifications(
                Guid userId,
                [FromQuery] int take = 50,
                CancellationToken cancellationToken = default)
        {
            var notifications =
                await _notificationService.GetUserNotificationsAsync(
                    userId,
                    take,
                    cancellationToken);

            return Ok(notifications);
        }

        [HttpGet("user/{userId:guid}/unread-count")]
        public async Task<ActionResult<int>>
            GetUnreadCount(
                Guid userId,
                CancellationToken cancellationToken = default)
        {
            var count =
                await _notificationService.GetUnreadCountAsync(
                    userId,
                    cancellationToken);

            return Ok(count);
        }

        [HttpPatch("{notificationId:guid}/read")]
        public async Task<IActionResult> MarkAsRead(
            Guid notificationId,
            [FromQuery] Guid userId,
            CancellationToken cancellationToken = default)
        {
            await _notificationService.MarkAsReadAsync(
                notificationId,
                userId,
                cancellationToken);

            return NoContent();
        }

        [HttpPatch("user/{userId:guid}/read-all")]
        public async Task<IActionResult> MarkAllAsRead(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            await _notificationService.MarkAllAsReadAsync(
                userId,
                cancellationToken);

            return NoContent();
        }

        // DEVELOPMENT ONLY.
        // Remove before production release.
        [HttpPost("test")]
        public async Task<ActionResult<Guid>> CreateTestNotification(
            [FromQuery] Guid userId,
            [FromQuery] Guid? envelopeId = null,
            CancellationToken cancellationToken = default)
        {
            var notificationId =
                await _notificationService.CreateAsync(
                    userId,
                    NotificationType.EnvelopeSent,
                    "Signature requested",
                    "A document has been sent to you for signature.",
                    envelopeId,
                    cancellationToken);

            return Ok(notificationId);
        }

        [HttpGet("user/{userId:guid}/preferences")]
        public async Task<ActionResult<NotificationPreferenceDto>>
    GetPreferences(
        Guid userId,
        CancellationToken cancellationToken = default)
        {
            var result =
                await _preferenceService.GetAsync(
                    userId,
                    cancellationToken);

            return Ok(result);
        }

        [HttpPut("user/{userId:guid}/preferences")]
        public async Task<ActionResult<NotificationPreferenceDto>>
    UpdatePreferences(
        Guid userId,
        [FromBody]
        UpdateNotificationPreferenceRequest request,
        CancellationToken cancellationToken = default)
        {
            var result =
                await _preferenceService.UpdateAsync(
                    userId,
                    request,
                    cancellationToken);

            return Ok(result);
        }
    }
}
