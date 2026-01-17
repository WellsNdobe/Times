using System;
using Times.Entities;

namespace Times.Dto.OrganizationMembers
{
    public class AddMemberRequest
    {
        public Guid UserId { get; set; }
        public OrganizationRole Role { get; set; } = OrganizationRole.Employee;
    }
}
