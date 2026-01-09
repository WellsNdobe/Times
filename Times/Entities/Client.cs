namespace Times.Entities
{
    public class Client
    {
		public long Id { get; set; }

		public required string Name { get; set; } = null!;

		public string? Email { get; set; }

		public string? Phone { get; set; }

		public long UserId { get; set; }
		public User User { get; set; } = null!;
	}
}
