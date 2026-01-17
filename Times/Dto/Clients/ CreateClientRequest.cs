namespace Times.Dto.Clients;

public class CreateClientRequest
{
	public string Name { get; set; } = null!;
	public string? Email { get; set; }
	public string? Phone { get; set; }
}
