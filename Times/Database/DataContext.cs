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

		public DbSet<Project> Projects => Set<Project>();
		public DbSet<ProjectAssignment> ProjectAssignments => Set<ProjectAssignment>();

		public DbSet<Timesheet> Timesheets => Set<Timesheet>();
		public DbSet<TimesheetEntry> TimesheetEntries => Set<TimesheetEntry>();
		public DbSet<Notification> Notifications => Set<Notification>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// ---- ProjectAssignment ----
			modelBuilder.Entity<ProjectAssignment>(b =>
			{
				b.HasKey(x => x.Id);
				b.HasIndex(x => new { x.ProjectId, x.UserId }).IsUnique();
				b.HasOne(x => x.Organization)
					.WithMany()
					.HasForeignKey(x => x.OrganizationId)
					.OnDelete(DeleteBehavior.NoAction);
				b.HasOne(x => x.Project)
					.WithMany()
					.HasForeignKey(x => x.ProjectId)
					.OnDelete(DeleteBehavior.Cascade);
				b.HasOne(x => x.User)
					.WithMany()
					.HasForeignKey(x => x.UserId)
					.OnDelete(DeleteBehavior.Cascade);
				b.HasOne(x => x.AssignedByUser)
					.WithMany()
					.HasForeignKey(x => x.AssignedByUserId)
					.OnDelete(DeleteBehavior.NoAction);
			});

			// ---- Timesheet ----
			modelBuilder.Entity<Timesheet>(b =>
			{
				b.HasKey(x => x.Id);

				b.Property(x => x.Status).IsRequired();

				b.Property(x => x.SubmissionComment).HasMaxLength(2000);
				b.Property(x => x.RejectionReason).HasMaxLength(2000);

				// One timesheet per user per org per week
				b.HasIndex(x => new { x.OrganizationId, x.UserId, x.WeekStartDate }).IsUnique();

				// Timesheet owns entries; deleting a timesheet deletes its entries
				b.HasMany(x => x.Entries)
					.WithOne(e => e.Timesheet)
					.HasForeignKey(e => e.TimesheetId)
					.OnDelete(DeleteBehavior.Cascade);

				b.HasOne(x => x.Organization)
					.WithMany()
					.HasForeignKey(x => x.OrganizationId)
					.OnDelete(DeleteBehavior.Cascade);

				b.HasOne(x => x.User)
					.WithMany()
					.HasForeignKey(x => x.UserId)
					.OnDelete(DeleteBehavior.Cascade);

				// IMPORTANT: avoid multiple cascade paths (Timesheet -> User and Timesheet -> ApprovedByUser)
				b.HasOne(x => x.ApprovedByUser)
					.WithMany()
					.HasForeignKey(x => x.ApprovedByUserId)
					.OnDelete(DeleteBehavior.NoAction);
			});

			// ---- TimesheetEntry ----
			modelBuilder.Entity<TimesheetEntry>(b =>
			{
				b.HasKey(x => x.Id);

				b.Property(x => x.DurationMinutes).IsRequired();
				b.Property(x => x.Notes).HasMaxLength(2000);

				b.HasIndex(x => new { x.OrganizationId, x.TimesheetId });
				b.HasIndex(x => new { x.OrganizationId, x.ProjectId });
				b.HasIndex(x => new { x.OrganizationId, x.WorkDate });

				// FK to Timesheet already configured via Timesheet.HasMany(...)

				// IMPORTANT: This fixes your SQL Server Error 1785
				// Do NOT cascade delete entries via Project, or you create multiple cascade paths.
				b.HasOne(x => x.Project)
					.WithMany()
					.HasForeignKey(x => x.ProjectId)
					.OnDelete(DeleteBehavior.NoAction);

				// Optional but recommended: avoid org -> entries cascade path too
				b.HasOne(x => x.Organization)
					.WithMany()
					.HasForeignKey(x => x.OrganizationId)
					.OnDelete(DeleteBehavior.NoAction);
			});

			// ---- Notification ----
			modelBuilder.Entity<Notification>(b =>
			{
				b.HasKey(x => x.Id);

				b.Property(x => x.Type).IsRequired();
				b.Property(x => x.Title).IsRequired().HasMaxLength(200);
				b.Property(x => x.Message).IsRequired().HasMaxLength(2000);

				b.HasIndex(x => new { x.OrganizationId, x.RecipientUserId, x.IsRead, x.CreatedAtUtc });

				b.HasOne(x => x.Organization)
					.WithMany()
					.HasForeignKey(x => x.OrganizationId)
					.OnDelete(DeleteBehavior.NoAction);

				b.HasOne(x => x.RecipientUser)
					.WithMany()
					.HasForeignKey(x => x.RecipientUserId)
					.OnDelete(DeleteBehavior.NoAction);

				b.HasOne(x => x.ActorUser)
					.WithMany()
					.HasForeignKey(x => x.ActorUserId)
					.OnDelete(DeleteBehavior.NoAction);

				b.HasOne(x => x.Timesheet)
					.WithMany()
					.HasForeignKey(x => x.TimesheetId)
					.OnDelete(DeleteBehavior.NoAction);
			});
		}
	}
}
