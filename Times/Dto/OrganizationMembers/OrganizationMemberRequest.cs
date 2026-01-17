using System;
using Times.Entities;

namespace Times.Dto.OrganizationMembers
{
	public class OrganizationMemberResponse
	{
		public Guid Id { get; set; }

		public Guid OrganizationId { get; set; }
		public Guid UserId { get; set; }

		public OrganizationRole Role { get; set; }
		public bool IsActive { get; set; }

		public DateTime CreatedAtUtc { get; set; }
		public DateTime UpdatedAtUtc { get; set; }
	}
}
