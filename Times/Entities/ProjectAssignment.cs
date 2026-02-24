using System;

namespace Times.Entities
{
	/// <summary>
	/// Links a user to a project within an organization. Managers/Admins assign users to projects.
	/// </summary>
	public class ProjectAssignment
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		public Guid OrganizationId { get; set; }
		public Organization Organization { get; set; } = null!;

		public Guid ProjectId { get; set; }
		public Project Project { get; set; } = null!;

		public Guid UserId { get; set; }
		public User User { get; set; } = null!;

		public Guid? AssignedByUserId { get; set; }
		public User? AssignedByUser { get; set; }
		public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
	}
}
