using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Times.Entities;

namespace Times.Infrastructure.Auth
{
	public sealed class JwtTokenService
	{
		private readonly IConfiguration _config;

		public JwtTokenService(IConfiguration config)
		{
			_config = config ?? throw new ArgumentNullException(nameof(config));
		}

		public string GenerateToken(User user)
		{
			if (user == null) throw new ArgumentNullException(nameof(user));

			var keyString = _config["Jwt:Key"];
			if (string.IsNullOrWhiteSpace(keyString))
				throw new InvalidOperationException("JWT signing key is not configured. Set 'Jwt:Key' in configuration.");

			var expiryStr = _config["Jwt:ExpiryMinutes"];
			if (!int.TryParse(expiryStr, out var expiryMinutes) || expiryMinutes <= 0)
			{
				// fallback to a sensible default if configuration is missing or invalid
				expiryMinutes = 60;
			}

			var now = DateTime.UtcNow;

			var claims = new List<Claim>
			{
				new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
				new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
				new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
				new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
				new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
			};

			// Support multiple roles if stored as comma-separated value
			if (!string.IsNullOrWhiteSpace(user.Role))
			{
				var roles = user.Role.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim());
				foreach (var r in roles)
				{
					claims.Add(new Claim(ClaimTypes.Role, r));
				}
			}

			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			var expires = now.AddMinutes(expiryMinutes);

			var token = new JwtSecurityToken(
				issuer: _config["Jwt:Issuer"],
				audience: _config["Jwt:Audience"],
				claims: claims,
				notBefore: now,
				expires: expires,
				signingCredentials: creds
			);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}
	}
}
