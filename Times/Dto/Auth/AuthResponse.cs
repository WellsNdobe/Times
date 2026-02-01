namespace Times.Dto.Auth
{
	public record AuthResponse(
		Guid UserId,
		string Email,
		string? Token
	);
}
