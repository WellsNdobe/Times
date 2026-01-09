namespace Times.Contracts.Clients;

public class ClientResponse
{
	public long Id { get; set; }
	public string Name { get; set; } = null!;
	public string? Email { get; set; }
	public string? Phone { get; set; }
}
