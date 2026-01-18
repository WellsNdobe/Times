using System;

namespace Times.Entities
{
	public class Project
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		// Ownership boundary
		public Guid OrganizationId { get; set; }
		public Organization Organization { get; set; } = null!;

		// Optional grouping
		public Guid? ClientId { get; set; }
		public Client? Client { get; set; }

		// Business fields
		public string Name { get; set; } = string.Empty;
		public string? Code { get; set; }
		public string? Description { get; set; }

		public bool IsActive { get; set; } = true;

		public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
		public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
	}
}
