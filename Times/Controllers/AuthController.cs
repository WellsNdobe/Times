using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Times.Entities;
using Times.Database;
using Times.Infrastructure.Auth;
using Times.Dto.Auth;


namespace Times.Controllers
{

	[ApiController]
	[Route("api/auth")]
	public class AuthController : ControllerBase
	{
		private readonly DataContext _db;
		private readonly PasswordHasher<User> _passwordHasher;
		private readonly JwtTokenService _jwt;

		public AuthController(DataContext db, JwtTokenService jwt)
		{
			_db = db;
			_jwt = jwt;
			_passwordHasher = new PasswordHasher<User>();
		}

		// POST: api/auth/register
		[HttpPost("register")]
		public async Task<IActionResult> Register([FromBody] RegisterRequest request)
		{
			var emailExists = await _db.Users
				.AnyAsync(u => u.Email == request.Email);

			if (emailExists)
				return BadRequest("Email already registered.");

			var user = new User
			{
				Id = Guid.NewGuid(),
				Email = request.Email,
				FirstName = request.FirstName,
				LastName = request.LastName
			};

			user.PasswordHash =
				_passwordHasher.HashPassword(user, request.Password);

			_db.Users.Add(user);
			await _db.SaveChangesAsync();

			return Ok(new AuthResponse(user.Id, user.Email, Token: null));
		}

		// POST: api/auth/login
		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginRequest request)
		{
			var user = await _db.Users
				.FirstOrDefaultAsync(u => u.Email == request.Email);

			if (user == null)
				return Unauthorized("Invalid email or password.");

			var result = _passwordHasher.VerifyHashedPassword(
				user,
				user.PasswordHash,
				request.Password
			);

			if (result == PasswordVerificationResult.Failed)
				return Unauthorized("Invalid email or password.");

			var token = _jwt.GenerateToken(user);
			return Ok(new AuthResponse(user.Id, user.Email, token));
		}
	}
}