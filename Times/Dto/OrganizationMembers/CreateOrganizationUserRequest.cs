using Times.Entities;

namespace Times.Dto.OrganizationMembers
{
	/// <summary>
	/// Request for an admin to create a new user and add them to the organization in one step.
	/// If a user with the given email already exists, they are added to the org instead.
	/// </summary>
	public class CreateOrganizationUserRequest
	{
		public string Email { get; set; } = null!;
		public string FirstName { get; set; } = null!;
		public string LastName { get; set; } = null!;
		public string Password { get; set; } = null!;
		public OrganizationRole Role { get; set; } = OrganizationRole.Employee;
	}
}
