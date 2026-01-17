using System;

namespace Times.Entities
{
	public class OrganizationMember
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		public Guid OrganizationId { get; set; }
		public Organization Organization { get; set; } = null!;

		public Guid UserId { get; set; }
		public User User { get; set; } = null!;

		public OrganizationRole Role { get; set; } = OrganizationRole.Employee;

		public bool IsActive { get; set; } = true;

		public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
		public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
	}
}
