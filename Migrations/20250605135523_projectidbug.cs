using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SonicPoints.Migrations
{
    /// <inheritdoc />
    public partial class projectidbug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "Leaderboards",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Leaderboards_ProjectId",
                table: "Leaderboards",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Leaderboards_Projects_ProjectId",
                table: "Leaderboards",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leaderboards_Projects_ProjectId",
                table: "Leaderboards");

            migrationBuilder.DropIndex(
                name: "IX_Leaderboards_ProjectId",
                table: "Leaderboards");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Leaderboards");
        }
    }
}
