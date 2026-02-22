using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Times.Migrations
{
    /// <inheritdoc />
    public partial class _20260222093000_FixPendingModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notifications_OrganizationId' AND object_id = OBJECT_ID('[Notifications]'))
    DROP INDEX [IX_Notifications_OrganizationId] ON [Notifications];
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notifications_RecipientUserId' AND object_id = OBJECT_ID('[Notifications]'))
    DROP INDEX [IX_Notifications_RecipientUserId] ON [Notifications];
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notifications_OrganizationId' AND object_id = OBJECT_ID('[Notifications]'))
    CREATE INDEX [IX_Notifications_OrganizationId] ON [Notifications] ([OrganizationId]);
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notifications_RecipientUserId' AND object_id = OBJECT_ID('[Notifications]'))
    CREATE INDEX [IX_Notifications_RecipientUserId] ON [Notifications] ([RecipientUserId]);
");
        }
    }
}
