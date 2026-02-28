using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Times.Dto.Clients;
using Times.Services.Contracts;
using Times.Services.Errors;

namespace Times.Controllers
{
	[ApiController]
	[Route("api/v1/organizations/{organizationId:guid}/clients")]
	[Authorize]
	public class ClientController : ControllerBase
	{
		private readonly IClientService _clientService;

		public ClientController(IClientService clientService)
		{
			_clientService = clientService;
		}

		private Guid GetUserId()
		{
			var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
			if (string.IsNullOrWhiteSpace(id))
				throw new UnauthorizedException("Missing user id claim.");
			return Guid.Parse(id);
		}

		[HttpGet]
		public async Task<IActionResult> List([FromRoute] Guid organizationId)
		{
			var actorUserId = GetUserId();
			var clients = await _clientService.ListAsync(actorUserId, organizationId);
			return Ok(clients);
		}

		[HttpPost]
		public async Task<IActionResult> Create([FromRoute] Guid organizationId, [FromBody] CreateClientRequest request)
		{
			var actorUserId = GetUserId();
			var created = await _clientService.CreateAsync(actorUserId, organizationId, request);
			return CreatedAtAction(nameof(List), new { organizationId }, created);
		}
	}
}