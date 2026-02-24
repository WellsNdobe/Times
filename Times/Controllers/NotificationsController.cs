using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Times.Dto.Notifications;
using Times.Services.Contracts;

namespace Times.Controllers
{
	[ApiController]
	[Route("api/v1/organizations/{organizationId:guid}/notifications")]
	[Authorize]
	public class NotificationsController : ControllerBase
	{
		private readonly INotificationService _notifications;

		public NotificationsController(INotificationService notifications)
		{
			_notifications = notifications;
		}

		private Guid GetUserId()
		{
			var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
			if (string.IsNullOrWhiteSpace(id)) throw new UnauthorizedAccessException("Missing user id claim.");
			return Guid.Parse(id);
		}

		[HttpGet]
		public async Task<IActionResult> List(
			[FromRoute] Guid organizationId,
			[FromQuery] bool unreadOnly = false,
			[FromQuery] int take = 25)
		{
			var actorUserId = GetUserId();
			var items = await _notifications.ListAsync(actorUserId, organizationId, unreadOnly, take);
			return Ok(items);
		}

		[HttpPost("mark-read")]
		public async Task<IActionResult> MarkRead(
			[FromRoute] Guid organizationId,
			[FromBody] MarkReadNotificationsRequest request)
		{
			var actorUserId = GetUserId();
			var updated = await _notifications.MarkReadAsync(
				actorUserId,
				organizationId,
				request?.Ids ?? new List<Guid>()
			);
			return Ok(new { updated });
		}

		[HttpPost("mark-all-read")]
		public async Task<IActionResult> MarkAllRead([FromRoute] Guid organizationId)
		{
			var actorUserId = GetUserId();
			var updated = await _notifications.MarkAllReadAsync(actorUserId, organizationId);
			return Ok(new { updated });
		}

		[HttpPost("reminder")]
		public async Task<IActionResult> CreateReminder([FromRoute] Guid organizationId)
		{
			var actorUserId = GetUserId();
			var created = await _notifications.CreateReminderAsync(actorUserId, organizationId);
			return Ok(created);
		}
	}
}
