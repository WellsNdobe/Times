using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Times.Dto.Tracker;
using Times.Services.Contracts;

namespace Times.Controllers
{
	[ApiController]
	[Route("api/v1/organizations/{organizationId:guid}/tracker")]
	[Authorize]
	public class TrackerController : ControllerBase
	{
		private readonly ITrackerService _tracker;

		public TrackerController(ITrackerService tracker)
		{
			_tracker = tracker;
		}

		private Guid GetUserId()
		{
			var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
			if (string.IsNullOrWhiteSpace(id)) throw new UnauthorizedAccessException("Missing user id claim.");
			return Guid.Parse(id);
		}

		[HttpGet("session")]
		public async Task<IActionResult> Get([FromRoute] Guid organizationId)
		{
			var actorUserId = GetUserId();
			var session = await _tracker.GetActiveSessionAsync(actorUserId, organizationId);
			return session is null ? NotFound() : Ok(session);
		}

		[HttpPost("session/start")]
		public async Task<IActionResult> Start([FromRoute] Guid organizationId, [FromBody] StartActiveTimerSessionRequest request)
		{
			var actorUserId = GetUserId();
			var session = await _tracker.StartAsync(actorUserId, organizationId, request);
			return Ok(session);
		}

		[HttpPatch("session")]
		public async Task<IActionResult> Update([FromRoute] Guid organizationId, [FromBody] UpdateActiveTimerSessionRequest request)
		{
			var actorUserId = GetUserId();
			var session = await _tracker.UpdateAsync(actorUserId, organizationId, request);
			return Ok(session);
		}

		[HttpPost("session/stop")]
		public async Task<IActionResult> Stop([FromRoute] Guid organizationId, [FromBody] StopActiveTimerSessionRequest? request)
		{
			var actorUserId = GetUserId();
			var entry = await _tracker.StopAsync(actorUserId, organizationId, request ?? new StopActiveTimerSessionRequest());
			return Ok(entry);
		}

		[HttpDelete("session")]
		public async Task<IActionResult> Delete([FromRoute] Guid organizationId)
		{
			var actorUserId = GetUserId();
			await _tracker.DeleteAsync(actorUserId, organizationId);
			return NoContent();
		}
	}
}
