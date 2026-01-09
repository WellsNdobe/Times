namespace Times.Contracts.Auth
{
	public record AuthResponse(
		Guid UserId,
		string Email,
		string Token
	);
}
