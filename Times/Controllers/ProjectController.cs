using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Times.Dto.Projects;
using Times.Services.Contracts;
using Times.Services.Errors;

namespace Times.Controllers
{
	[ApiController]
	[Route("api/v1/organizations/{organizationId:guid}/projects")]
	[Authorize]
	public class ProjectController : ControllerBase
	{
		private readonly IProjectService _projects;

		public ProjectController(IProjectService projects)
		{
			_projects = projects;
		}

		private Guid GetUserId()
		{
			var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
			if (string.IsNullOrWhiteSpace(id))
				throw new UnauthorizedException("Missing user id claim.");

			return Guid.Parse(id);
		}

		[HttpPost]
		public async Task<IActionResult> Create([FromRoute] Guid organizationId, [FromBody] CreateProjectRequest request)
		{
			var actorUserId = GetUserId();
			var created = await _projects.CreateAsync(actorUserId, organizationId, request);
			return CreatedAtAction(nameof(GetById), new { organizationId, projectId = created.Id }, created);
		}

		[HttpGet]
		public async Task<IActionResult> List(
			[FromRoute] Guid organizationId,
			[FromQuery] bool? isActive = null,
			[FromQuery] Guid? clientId = null)
		{
			var actorUserId = GetUserId();
			var items = await _projects.ListAsync(actorUserId, organizationId, isActive, clientId);
			return Ok(items);
		}

		[HttpGet("{projectId:guid}")]
		public async Task<IActionResult> GetById([FromRoute] Guid organizationId, [FromRoute] Guid projectId)
		{
			var actorUserId = GetUserId();
			var item = await _projects.GetByIdAsync(actorUserId, organizationId, projectId);
			return Ok(item);
		}

		[HttpPatch("{projectId:guid}")]
		public async Task<IActionResult> Update([FromRoute] Guid organizationId, [FromRoute] Guid projectId, [FromBody] UpdateProjectRequest request)
		{
			var actorUserId = GetUserId();
			var updated = await _projects.UpdateAsync(actorUserId, organizationId, projectId, request);
			return Ok(updated);
		}

		/// <summary>Manager/Admin: assign a user to this project.</summary>
		[HttpPost("{projectId:guid}/assignments")]
		public async Task<IActionResult> AssignUser([FromRoute] Guid organizationId, [FromRoute] Guid projectId, [FromBody] AssignUserToProjectRequest request)
		{
			var actorUserId = GetUserId();
			var result = await _projects.AssignUserAsync(actorUserId, organizationId, projectId, request);
			return Ok(result);
		}

		/// <summary>Manager/Admin: remove a user from this project.</summary>
		[HttpDelete("{projectId:guid}/assignments/{userId:guid}")]
		public async Task<IActionResult> UnassignUser([FromRoute] Guid organizationId, [FromRoute] Guid projectId, [FromRoute] Guid userId)
		{
			var actorUserId = GetUserId();
			await _projects.UnassignUserAsync(actorUserId, organizationId, projectId, userId);
			return NoContent();
		}

		/// <summary>List users assigned to this project.</summary>
		[HttpGet("{projectId:guid}/assignments")]
		public async Task<IActionResult> GetAssignments([FromRoute] Guid organizationId, [FromRoute] Guid projectId)
		{
			var actorUserId = GetUserId();
			var assignments = await _projects.GetProjectAssignmentsAsync(actorUserId, organizationId, projectId);
			return Ok(assignments);
		}
	}
}
