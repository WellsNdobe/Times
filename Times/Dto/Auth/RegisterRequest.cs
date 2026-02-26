namespace Times.Dto.Auth
{
	public record RegisterRequest(
		string Email,
		string Password,
		string FirstName,
		string LastName,
		string OrganizationName
	);
}
