using System;

namespace Times.Dto.Projects
{
	public class ProjectAssignmentResponse
	{
		public Guid Id { get; set; }
		public Guid ProjectId { get; set; }
		public Guid UserId { get; set; }
		public Guid? AssignedByUserId { get; set; }
		public DateTime AssignedAtUtc { get; set; }
	}
}
