using Microsoft.EntityFrameworkCore;
using Times.Entities;
namespace Times.Database

{
    public class DataContext : DbContext
    {
		
		
			public DataContext(DbContextOptions<DataContext> options)
				: base(options)
			{
			}
		


		public DbSet<User> Users => Set<User>();
		public DbSet<Client> Clients => Set<Client>();
		public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
		public DbSet<Organization> Organizations => Set<Organization>();


	}
}
