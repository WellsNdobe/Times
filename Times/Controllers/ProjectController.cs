using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Times.Dto.Projects;
using Times.Services.Contracts;

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
			if (string.IsNullOrWhiteSpace(id)) throw new UnauthorizedAccessException("Missing user id claim.");
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
			return item is null ? NotFound() : Ok(item);
		}

		[HttpPatch("{projectId:guid}")]
		public async Task<IActionResult> Update([FromRoute] Guid organizationId, [FromRoute] Guid projectId, [FromBody] UpdateProjectRequest request)
		{
			var actorUserId = GetUserId();
			var updated = await _projects.UpdateAsync(actorUserId, organizationId, projectId, request);
			return updated is null ? Forbid() : Ok(updated);
		}
	}
}
