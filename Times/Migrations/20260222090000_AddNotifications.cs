using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Times.Migrations
{
	public partial class AddNotifications : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "Notifications",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
					OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
					RecipientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
					ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
					TimesheetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
					Type = table.Column<int>(type: "int", nullable: false),
					Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
					Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
					CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
					ReadAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Notifications", x => x.Id);
					table.ForeignKey(
						name: "FK_Notifications_Organizations_OrganizationId",
						column: x => x.OrganizationId,
						principalTable: "Organizations",
						principalColumn: "Id",
						onDelete: ReferentialAction.NoAction);
					table.ForeignKey(
						name: "FK_Notifications_Timesheets_TimesheetId",
						column: x => x.TimesheetId,
						principalTable: "Timesheets",
						principalColumn: "Id",
						onDelete: ReferentialAction.NoAction);
					table.ForeignKey(
						name: "FK_Notifications_Users_ActorUserId",
						column: x => x.ActorUserId,
						principalTable: "Users",
						principalColumn: "Id",
						onDelete: ReferentialAction.NoAction);
					table.ForeignKey(
						name: "FK_Notifications_Users_RecipientUserId",
						column: x => x.RecipientUserId,
						principalTable: "Users",
						principalColumn: "Id",
						onDelete: ReferentialAction.NoAction);
				});

			migrationBuilder.CreateIndex(
				name: "IX_Notifications_ActorUserId",
				table: "Notifications",
				column: "ActorUserId");

			migrationBuilder.CreateIndex(
				name: "IX_Notifications_OrganizationId",
				table: "Notifications",
				column: "OrganizationId");

			migrationBuilder.CreateIndex(
				name: "IX_Notifications_RecipientUserId",
				table: "Notifications",
				column: "RecipientUserId");

			migrationBuilder.CreateIndex(
				name: "IX_Notifications_TimesheetId",
				table: "Notifications",
				column: "TimesheetId");

			migrationBuilder.CreateIndex(
				name: "IX_Notifications_OrganizationId_RecipientUserId_CreatedAtUtc",
				table: "Notifications",
				columns: new[] { "OrganizationId", "RecipientUserId", "CreatedAtUtc" });

			migrationBuilder.CreateIndex(
				name: "IX_Notifications_RecipientUserId_ReadAtUtc",
				table: "Notifications",
				columns: new[] { "RecipientUserId", "ReadAtUtc" });
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "Notifications");
		}
	}
}
