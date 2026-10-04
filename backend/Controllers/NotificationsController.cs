using System.Security.Claims;
using InternshipManagementApi.Common.Models;
using InternshipManagementApi.DTOs.Notifications;
using InternshipManagementApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagementApi.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    [Produces("application/json")]
    public sealed class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService) =>
            _notificationService = notificationService;

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NotificationDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
        {
            var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId");
            if (!int.TryParse(idValue, out var userId))
                return Unauthorized(ApiResponse.Fail("The authenticated account ID is missing."));

            var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? string.Empty;
            var notifications = await _notificationService.GetForUserAsync(userId, role, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<NotificationDto>>.Ok(notifications, "Notifications retrieved successfully."));
        }
    }
}
