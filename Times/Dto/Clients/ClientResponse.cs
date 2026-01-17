namespace Times.Dto.Clients;

public class ClientResponse
{
	public Guid Id { get; set; }
	public string Name { get; set; } = null!;
	public string? Email { get; set; }
	public string? Phone { get; set; }
}
