using System;

namespace Times.Dto.Projects
{
	public class UpdateProjectRequest
	{
		public string? Name { get; set; }
		public Guid? ClientId { get; set; }       // can set or clear (see service notes)
		public string? Code { get; set; }
		public string? Description { get; set; }
		public bool? IsActive { get; set; }
	}
}
