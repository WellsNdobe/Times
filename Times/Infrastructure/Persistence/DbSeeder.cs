using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Times.Entities;
using Times.Database;


namespace Times.Infrastructure.Persistence
{
	public static class DbSeeder
	{
		public static async Task SeedAdminAsync(DataContext db)
		{
			if (await db.Users.AnyAsync())
				return;

			var admin = new User
			{
				Id = Guid.NewGuid(),
				Email = "admin@timesheet.local",
				FirstName = "System",
				LastName = "Admin",
				Role = "Admin"
			};

			var hasher = new PasswordHasher<User>();
			admin.PasswordHash = hasher.HashPassword(admin, "Admin123!");

			db.Users.Add(admin);
			await db.SaveChangesAsync();
		}
	}
}
