namespace Times.Entities
{
    public class Client
    {
		public Guid Id { get; set; }

		public required string Name { get; set; } = null!;

		public string? Email { get; set; }

		public string? Phone { get; set; }

		public Guid UserId { get; set; }
		public User User { get; set; } = null!;

		public Guid OrganizationId { get; set; }
	}
}
