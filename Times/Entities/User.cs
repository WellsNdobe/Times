using System.Collections.Generic;
namespace Times.Entities
{
    public class User
    {

			public Guid Id { get; set; }

			public string Email { get; set; } = null!;

			public string PasswordHash { get; set; } = null!;

			public string FirstName { get; set; } = null!;

			public string LastName { get; set; } = null!;

			public string Role { get; set; } = "Employee";

			public bool IsActive { get; set; } = true;

			public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		    public ICollection<OrganizationMember> OrganizationMemberships { get; set; } = new List<OrganizationMember>();
	}
	}


