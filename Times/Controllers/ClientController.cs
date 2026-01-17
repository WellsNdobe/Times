using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Times.Dto.Clients;
using Times.Services.Contracts;

[ApiController]
[Route("api/clients")]
[Authorize]
public class ClientController : ControllerBase
{
	private readonly IClientService _clientService;

	public ClientController(IClientService clientService)
	{
		_clientService = clientService;
	}

	private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);


	[HttpGet]
	public IActionResult GetMyClients()
	{
		return Ok(_clientService.GetMyClients(UserId));
	}

	[HttpPost]
	public IActionResult Create(CreateClientRequest request)
	{
		var result = _clientService.CreateClient(UserId, request);
		return CreatedAtAction(nameof(GetMyClients), result);
	}


}
