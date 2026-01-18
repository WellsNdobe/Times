using System;

namespace Times.Dto.Projects
{
	public class CreateProjectRequest
	{
		public string Name { get; set; } = string.Empty;

		public Guid? ClientId { get; set; }

		public string? Code { get; set; }
		public string? Description { get; set; }
	}
}
