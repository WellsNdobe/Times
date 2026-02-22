using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Times.Dto.Timesheets;
using Times.Services.Contracts;

namespace Times.Controllers
{
	[ApiController]
	[Route("api/v1/organizations/{organizationId:guid}/timesheets")]
	[Authorize]
	public class TimesheetController : ControllerBase
	{
		private readonly ITimesheetService _timesheets;

		public TimesheetController(ITimesheetService timesheets)
		{
			_timesheets = timesheets;
		}

		private Guid GetUserId()
		{
			var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
			if (string.IsNullOrWhiteSpace(id)) throw new UnauthorizedAccessException("Missing user id claim.");
			return Guid.Parse(id);
		}

		// Create a weekly timesheet (idempotent: returns existing if already created for that week)
		[HttpPost]
		public async Task<IActionResult> Create([FromRoute] Guid organizationId, [FromBody] CreateTimesheetRequest request)
		{
			var actorUserId = GetUserId();
			var created = await _timesheets.CreateAsync(actorUserId, organizationId, request);
			return CreatedAtAction(nameof(GetById), new { organizationId, timesheetId = created.Id }, created);
		}

		// Manager/Admin: list timesheets for the organization
		[HttpGet]
		public async Task<IActionResult> List(
			[FromRoute] Guid organizationId,
			[FromQuery] DateOnly? fromWeekStart = null,
			[FromQuery] DateOnly? toWeekStart = null)
		{
			var actorUserId = GetUserId();
			var items = await _timesheets.ListOrgAsync(actorUserId, organizationId, fromWeekStart, toWeekStart);
			return Ok(items);
		}

		// Manager/Admin: list timesheets pending approval (Submitted status)
		[HttpGet("pending-approval")]
		public async Task<IActionResult> PendingApproval(
			[FromRoute] Guid organizationId,
			[FromQuery] DateOnly? fromWeekStart = null,
			[FromQuery] DateOnly? toWeekStart = null)
		{
			var actorUserId = GetUserId();
			var items = await _timesheets.ListPendingApprovalAsync(actorUserId, organizationId, fromWeekStart, toWeekStart);
			return Ok(items);
		}

		// GET .../timesheets/mine?fromWeekStart=2026-01-05&toWeekStart=2026-02-02
		[HttpGet("mine")]
		public async Task<IActionResult> Mine(
			[FromRoute] Guid organizationId,
			[FromQuery] DateOnly? fromWeekStart = null,
			[FromQuery] DateOnly? toWeekStart = null)
		{
			var actorUserId = GetUserId();
			var items = await _timesheets.ListMineAsync(actorUserId, organizationId, fromWeekStart, toWeekStart);
			return Ok(items);
		}

		// Get one timesheet (owner OR manager/admin)
		[HttpGet("{timesheetId:guid}")]
		public async Task<IActionResult> GetById([FromRoute] Guid organizationId, [FromRoute] Guid timesheetId)
		{
			var actorUserId = GetUserId();
			var ts = await _timesheets.GetByIdAsync(actorUserId, organizationId, timesheetId);
			return ts is null ? NotFound() : Ok(ts);
		}

		// Employee submits timesheet for approval
		[HttpPost("{timesheetId:guid}/submit")]
		public async Task<IActionResult> Submit(
			[FromRoute] Guid organizationId,
			[FromRoute] Guid timesheetId,
			[FromBody] SubmitTimesheetRequest request)
		{
			var actorUserId = GetUserId();
			var ts = await _timesheets.SubmitAsync(actorUserId, organizationId, timesheetId, request);
			return ts is null ? NotFound() : Ok(ts);
		}

		// Manager/Admin approves a submitted timesheet
		[HttpPost("{timesheetId:guid}/approve")]
		public async Task<IActionResult> Approve(
			[FromRoute] Guid organizationId,
			[FromRoute] Guid timesheetId,
			[FromBody] ApproveTimesheetRequest request)
		{
			var actorUserId = GetUserId();
			var ts = await _timesheets.ApproveAsync(actorUserId, organizationId, timesheetId, request);
			return ts is null ? Forbid() : Ok(ts);
		}

		// Manager/Admin rejects a submitted timesheet
		[HttpPost("{timesheetId:guid}/reject")]
		public async Task<IActionResult> Reject(
			[FromRoute] Guid organizationId,
			[FromRoute] Guid timesheetId,
			[FromBody] RejectTimesheetRequest request)
		{
			var actorUserId = GetUserId();
			var ts = await _timesheets.RejectAsync(actorUserId, organizationId, timesheetId, request);
			return ts is null ? Forbid() : Ok(ts);
		}
	}
}
