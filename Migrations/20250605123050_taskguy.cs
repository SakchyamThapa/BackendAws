using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SonicPoints.Migrations
{
    /// <inheritdoc />
    public partial class taskguy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PointEligibleUserId",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PointEligibleUserId",
                table: "Tasks");
        }
    }
}
