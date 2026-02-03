using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Times.Migrations
{
	/// <inheritdoc />
	public partial class AddApprovedComment : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<string>(
				name: "ApprovedComment",
				table: "Timesheets",
				type: "nvarchar(2000)",
				maxLength: 2000,
				nullable: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "ApprovedComment",
				table: "Timesheets");
		}
	}
}
