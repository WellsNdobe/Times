using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Times.Dto.OrganizationMembers;
using Times.Dto.Organizations;
using Times.Services.Contracts;

namespace Times.Controllers
{
	[ApiController]
	[Route("api/v1/organizations")]
	[Authorize]
	public class OrganizationController : ControllerBase
	{
		private readonly IOrganizationService _orgs;

		public OrganizationController(IOrganizationService orgs)
		{
			_orgs = orgs;
		}

		private Guid GetUserId()
		{
			var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
			if (string.IsNullOrWhiteSpace(id)) throw new UnauthorizedAccessException("Missing user id claim.");
			return Guid.Parse(id);
		}

		[HttpPost]
		public async Task<IActionResult> Create([FromBody] CreateOrganizationRequest request)
		{
			var actorUserId = GetUserId();
			var created = await _orgs.CreateAsync(actorUserId, request);
			return CreatedAtAction(nameof(GetById), new { organizationId = created.Id }, created);
		}

		[HttpGet("mine")]
		public async Task<IActionResult> Mine()
		{
			var actorUserId = GetUserId();
			var orgs = await _orgs.GetMyOrganizationsAsync(actorUserId);
			return Ok(orgs);
		}

		[HttpGet("{organizationId:guid}")]
		public async Task<IActionResult> GetById([FromRoute] Guid organizationId)
		{
			var actorUserId = GetUserId();
			var org = await _orgs.GetByIdAsync(actorUserId, organizationId);
			return Ok(org);
		}


		[HttpPatch("{organizationId:guid}")]
		public async Task<IActionResult> Update([FromRoute] Guid organizationId, [FromBody] UpdateOrganizationRequest request)
		{
			var actorUserId = GetUserId();
			var updated = await _orgs.UpdateAsync(actorUserId, organizationId, request);
			return Ok(updated);
		}
		[HttpGet("{organizationId:guid}/members")]
		public async Task<IActionResult> Members([FromRoute] Guid organizationId)
		{
			var actorUserId = GetUserId();
			var members = await _orgs.GetMembersAsync(actorUserId, organizationId);
			return Ok(members);
		}

		[HttpPost("{organizationId:guid}/members")]
		public async Task<IActionResult> AddMember([FromRoute] Guid organizationId, [FromBody] AddMemberRequest request)
		{
			var actorUserId = GetUserId();
			var added = await _orgs.AddMemberAsync(actorUserId, organizationId, request);
			return Ok(added);
		}

		/// <summary>
		/// Admin only: create a new user and add them to the organization, or add an existing user (by email) to the org.
		/// </summary>
		[HttpPost("{organizationId:guid}/members/create-user")]
		public async Task<IActionResult> CreateUserInOrganization([FromRoute] Guid organizationId, [FromBody] CreateOrganizationUserRequest request)
		{
			var actorUserId = GetUserId();
			var result = await _orgs.CreateUserInOrganizationAsync(actorUserId, organizationId, request);
			return Ok(result);
		}

		[HttpPatch("{organizationId:guid}/members/{memberId:guid}")]
		public async Task<IActionResult> UpdateMember([FromRoute] Guid organizationId, [FromRoute] Guid memberId, [FromBody] UpdateMemberRequest request)
		{
			var actorUserId = GetUserId();
			var updated = await _orgs.UpdateMemberAsync(actorUserId, organizationId, memberId, request);
			return Ok(updated);
		}
	}
}
