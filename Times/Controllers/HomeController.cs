using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Times.Database;


namespace Times.Controllers
{
	[ApiController]
	public class RootController : ControllerBase
	{
		private readonly DataContext _db;

		public RootController(DataContext db)
		{
			_db = db;
		}

		[HttpGet("/")]
		public async Task<IActionResult> Get()
		{
			var dbOnline = await _db.Database.CanConnectAsync();

			return Ok(new
			{
				service = "TimeSheet API",
				status = "Running",
				database = dbOnline ? "Connected" : "Unavailable",
				timestamp = DateTime.UtcNow
			});
		}
	}


	[Authorize]
	[ApiController]
	[Route("api/secure")]
	public class SecureController : ControllerBase
	{
		[HttpGet]
		public IActionResult Get()
		{
			return Ok("You are authenticated");
		}
	}

}
