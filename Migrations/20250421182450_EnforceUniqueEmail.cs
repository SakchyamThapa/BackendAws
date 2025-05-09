using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SonicPoints.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUniqueEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leaderboards_AspNetUsers_UserId",
                table: "Leaderboards");

            migrationBuilder.DropForeignKey(
                name: "FK_Leaderboards_AspNetUsers_UserId1",
                table: "Leaderboards");

            migrationBuilder.DropIndex(
                name: "IX_Leaderboards_UserId1",
                table: "Leaderboards");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "Leaderboards");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Email",
                table: "AspNetUsers",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Leaderboards_AspNetUsers_UserId",
                table: "Leaderboards",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leaderboards_AspNetUsers_UserId",
                table: "Leaderboards");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_Email",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "UserId1",
                table: "Leaderboards",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leaderboards_UserId1",
                table: "Leaderboards",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Leaderboards_AspNetUsers_UserId",
                table: "Leaderboards",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Leaderboards_AspNetUsers_UserId1",
                table: "Leaderboards",
                column: "UserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
