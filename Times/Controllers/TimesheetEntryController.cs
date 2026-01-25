using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Times.Dto.TimesheetEntries;
using Times.Services.Contracts;

namespace Times.Controllers
{
	[ApiController]
	[Route("api/v1/organizations/{organizationId:guid}/timesheets/{timesheetId:guid}/entries")]
	[Authorize]
	public class TimesheetEntryController : ControllerBase
	{
		private readonly ITimesheetEntryService _entries;

		public TimesheetEntryController(ITimesheetEntryService entries)
		{
			_entries = entries;
		}

		private Guid GetUserId()
		{
			var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
			if (string.IsNullOrWhiteSpace(id)) throw new UnauthorizedAccessException("Missing user id claim.");
			return Guid.Parse(id);
		}

		// List entries for a timesheet (owner OR manager/admin)
		[HttpGet]
		public async Task<IActionResult> List([FromRoute] Guid organizationId, [FromRoute] Guid timesheetId)
		{
			var actorUserId = GetUserId();
			var items = await _entries.ListAsync(actorUserId, organizationId, timesheetId);
			return Ok(items);
		}

		// Add an entry (owner only, timesheet must be editable)
		[HttpPost]
		public async Task<IActionResult> Create(
			[FromRoute] Guid organizationId,
			[FromRoute] Guid timesheetId,
			[FromBody] CreateTimesheetEntryRequest request)
		{
			var actorUserId = GetUserId();
			var created = await _entries.CreateAsync(actorUserId, organizationId, timesheetId, request);
			return CreatedAtAction(nameof(List), new { organizationId, timesheetId }, created);
		}

		// Update an entry (owner only, timesheet must be editable)
		[HttpPatch("{entryId:guid}")]
		public async Task<IActionResult> Update(
			[FromRoute] Guid organizationId,
			[FromRoute] Guid timesheetId,
			[FromRoute] Guid entryId,
			[FromBody] UpdateTimesheetEntryRequest request)
		{
			var actorUserId = GetUserId();
			var updated = await _entries.UpdateAsync(actorUserId, organizationId, timesheetId, entryId, request);
			return updated is null ? NotFound() : Ok(updated);
		}

		// Delete (soft delete) an entry (owner only, timesheet must be editable)
		[HttpDelete("{entryId:guid}")]
		public async Task<IActionResult> Delete(
			[FromRoute] Guid organizationId,
			[FromRoute] Guid timesheetId,
			[FromRoute] Guid entryId)
		{
			var actorUserId = GetUserId();
			var ok = await _entries.DeleteAsync(actorUserId, organizationId, timesheetId, entryId);
			return ok ? NoContent() : NotFound();
		}
	}
}
