using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Times.Migrations
{
    /// <inheritdoc />
    public partial class AddActiveTimerSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActiveTimerSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimesheetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UtcOffsetMinutes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActiveTimerSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActiveTimerSessions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ActiveTimerSessions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ActiveTimerSessions_Timesheets_TimesheetId",
                        column: x => x.TimesheetId,
                        principalTable: "Timesheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActiveTimerSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActiveTimerSessions_OrganizationId_ProjectId",
                table: "ActiveTimerSessions",
                columns: new[] { "OrganizationId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActiveTimerSessions_OrganizationId_TimesheetId",
                table: "ActiveTimerSessions",
                columns: new[] { "OrganizationId", "TimesheetId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActiveTimerSessions_OrganizationId_UserId",
                table: "ActiveTimerSessions",
                columns: new[] { "OrganizationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActiveTimerSessions_ProjectId",
                table: "ActiveTimerSessions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ActiveTimerSessions_TimesheetId",
                table: "ActiveTimerSessions",
                column: "TimesheetId");

            migrationBuilder.CreateIndex(
                name: "IX_ActiveTimerSessions_UserId",
                table: "ActiveTimerSessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActiveTimerSessions");
        }
    }
}
