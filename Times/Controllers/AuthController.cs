using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Times.Database;
using Times.Dto.Auth;
using Times.Dto.Organizations;
using Times.Entities;
using Times.Infrastructure.Auth;
using Times.Services.Contracts;
using Times.Services.Errors;

namespace Times.Controllers
{
	[ApiController]
	[Route("api/auth")]
	public class AuthController : ControllerBase
	{
		private const int MinPasswordLength = 6;
		private readonly DataContext _db;
		private readonly PasswordHasher<User> _passwordHasher;
		private readonly JwtTokenService _jwt;
		private readonly IRevokedTokenStore _revokedTokenStore;
		private readonly IOrganizationService _orgs;

		public AuthController(
			DataContext db,
			JwtTokenService jwt,
			IRevokedTokenStore revokedTokenStore,
			IOrganizationService orgs)
		{
			_db = db;
			_jwt = jwt;
			_revokedTokenStore = revokedTokenStore;
			_orgs = orgs;
			_passwordHasher = new PasswordHasher<User>();
		}

		// POST: api/auth/register
		[HttpPost("register")]
		public async Task<IActionResult> Register([FromBody] RegisterRequest request)
		{
			var errors = ValidateRegisterRequest(request);
			if (errors.Count > 0)
				throw new ValidationException("Registration validation failed.", errors);

			var email = request.Email.Trim();
			var emailExists = await _db.Users.AnyAsync(u => u.Email == email);
			if (emailExists)
				throw new ConflictException("Email already registered.");

			var user = new User
			{
				Id = Guid.NewGuid(),
				Email = email,
				FirstName = request.FirstName?.Trim() ?? string.Empty,
				LastName = request.LastName?.Trim() ?? string.Empty
			};
			user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

			await using var transaction = await _db.Database.BeginTransactionAsync();
			_db.Users.Add(user);
			await _db.SaveChangesAsync();
			await _orgs.CreateAsync(user.Id, new CreateOrganizationRequest { Name = request.OrganizationName });
			await transaction.CommitAsync();

			var token = _jwt.GenerateToken(user);
			return Ok(new AuthResponse(user.Id, user.Email, token));
		}

		// POST: api/auth/login
		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginRequest request)
		{
			var errors = ValidateLoginRequest(request);
			if (errors.Count > 0)
				throw new ValidationException("Login validation failed.", errors);

			var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
			if (user == null)
				throw new UnauthorizedException("Invalid email or password.");

			var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
			if (result == PasswordVerificationResult.Failed)
				throw new UnauthorizedException("Invalid email or password.");

			var token = _jwt.GenerateToken(user);
			return Ok(new AuthResponse(user.Id, user.Email, token));
		}

		// POST: api/auth/logout
		[HttpPost("logout")]
		[Authorize]
		public async Task<IActionResult> Logout(CancellationToken cancellationToken)
		{
			var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
			var expClaim = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
			if (string.IsNullOrEmpty(jti))
				return Ok(new { message = "No token to revoke." });

			var expiresAtUtc = DateTime.UtcNow.AddMinutes(5); // fallback: revoke for a short window
			if (!string.IsNullOrEmpty(expClaim) && long.TryParse(expClaim, out var unixExp))
				expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(unixExp).UtcDateTime;

			await _revokedTokenStore.RevokeAsync(jti, expiresAtUtc, cancellationToken);
			return Ok(new { message = "Logged out successfully. Token has been revoked." });
		}

		private static Dictionary<string, string[]> ValidateLoginRequest(LoginRequest request)
		{
			var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
			if (string.IsNullOrWhiteSpace(request.Email))
				AddError(errors, "Email", "Email is required.");
			else if (!IsValidEmail(request.Email))
				AddError(errors, "Email", "Email format is invalid.");
			if (string.IsNullOrWhiteSpace(request.Password))
				AddError(errors, "Password", "Password is required.");
			return errors;
		}

		private static Dictionary<string, string[]> ValidateRegisterRequest(RegisterRequest request)
		{
			var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
			if (string.IsNullOrWhiteSpace(request.Email))
				AddError(errors, "Email", "Email is required.");
			else if (!IsValidEmail(request.Email))
				AddError(errors, "Email", "Email format is invalid.");
			if (string.IsNullOrWhiteSpace(request.Password))
				AddError(errors, "Password", "Password is required.");
			else if (request.Password.Length < MinPasswordLength)
				AddError(errors, "Password", $"Password must be at least {MinPasswordLength} characters.");
			if (string.IsNullOrWhiteSpace(request.FirstName))
				AddError(errors, "FirstName", "First name is required.");
			if (string.IsNullOrWhiteSpace(request.LastName))
				AddError(errors, "LastName", "Last name is required.");
			if (string.IsNullOrWhiteSpace(request.OrganizationName))
				AddError(errors, "OrganizationName", "Organization name is required.");
			return errors;
		}

		private static void AddError(Dictionary<string, string[]> errors, string key, string message)
		{
			errors[key] = errors.TryGetValue(key, out var existing)
				? existing.Append(message).ToArray()
				: new[] { message };
		}

		private static bool IsValidEmail(string email)
		{
			return !string.IsNullOrWhiteSpace(email) && MailAddress.TryCreate(email, out _);
		}
	}
}
