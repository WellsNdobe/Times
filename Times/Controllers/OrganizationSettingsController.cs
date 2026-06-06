using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Times.Dto.OrganizationSettings;
using Times.Services.Contracts;

namespace Times.Controllers
{
	[ApiController]
	[Route("api/v1/organizations/{organizationId:guid}/settings")]
	[Authorize]
	public class OrganizationSettingsController : ControllerBase
	{
		private readonly IOrganizationSettingsService _settings;

		public OrganizationSettingsController(IOrganizationSettingsService settings)
		{
			_settings = settings;
		}

		private Guid GetUserId()
		{
			var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
			if (string.IsNullOrWhiteSpace(id)) throw new UnauthorizedAccessException("Missing user id claim.");
			return Guid.Parse(id);
		}

		[HttpGet]
		public async Task<IActionResult> Get([FromRoute] Guid organizationId)
		{
			var actorUserId = GetUserId();
			var settings = await _settings.GetAsync(actorUserId, organizationId);
			return Ok(settings);
		}

		[HttpPatch]
		public async Task<IActionResult> Update([FromRoute] Guid organizationId, [FromBody] UpdateOrganizationSettingsRequest request)
		{
			var actorUserId = GetUserId();
			var settings = await _settings.UpdateAsync(actorUserId, organizationId, request);
			return Ok(settings);
		}
	}
}
