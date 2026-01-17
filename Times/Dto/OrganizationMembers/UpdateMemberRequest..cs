using Times.Entities;

namespace Times.Dto.OrganizationMembers
{
	public class UpdateMemberRequest
	{
		public OrganizationRole? Role { get; set; }
		public bool? IsActive { get; set; }
	}
}
