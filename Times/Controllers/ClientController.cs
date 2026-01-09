using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Times.Contracts.Clients;
using Times.Entities;
using Times.Database;


namespace Times.Controllers;

[ApiController]
[Route("api/clients")]
[Authorize]
public class ClientController : ControllerBase
{
	private readonly DataContext _db;

	public ClientController(DataContext db)
	{
		_db = db;
	}

	private long UserId =>
		long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

	[HttpGet]
	public IActionResult GetMyClients()
	{
		var clients = _db.Clients
			.Where(c => c.UserId == UserId)
			.Select(c => new ClientResponse
			{
				Id = c.Id,
				Name = c.Name,
				Email = c.Email,
				Phone = c.Phone
			})
			.ToList();

		return Ok(clients);
	}

	[HttpPost]
	public IActionResult Create(CreateClientRequest request)
	{
		var client = new Client
		{
			Name = request.Name,
			Email = request.Email,
			Phone = request.Phone,
			UserId = UserId
		};

		_db.Clients.Add(client);
		_db.SaveChanges();

		var response = new ClientResponse
		{
			Id = client.Id,
			Name = client.Name,
			Email = client.Email,
			Phone = client.Phone
		};

		return CreatedAtAction(nameof(GetMyClients), response);
	}
}
