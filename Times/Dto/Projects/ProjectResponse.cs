using System;

namespace Times.Dto.Projects
{
	public class ProjectResponse
	{
		public Guid Id { get; set; }

		public Guid OrganizationId { get; set; }
		public Guid? ClientId { get; set; }

		public string Name { get; set; } = string.Empty;
		public string? Code { get; set; }
		public string? Description { get; set; }

		public bool IsActive { get; set; }

		public DateTime CreatedAtUtc { get; set; }
		public DateTime UpdatedAtUtc { get; set; }
	}
}
