using System;

namespace Times.Dto.Organizations
{
	public class OrganizationResponse
	{
		public Guid Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public bool IsActive { get; set; }

		public DateTime CreatedAtUtc { get; set; }
		public DateTime UpdatedAtUtc { get; set; }
	}
}
