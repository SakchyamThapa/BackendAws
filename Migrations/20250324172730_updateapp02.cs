using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SonicPoints.Migrations
{
    /// <inheritdoc />
    public partial class updateapp02 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RedeemHistories_AspNetUsers_UserId",
                table: "RedeemHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_RedeemHistories_AspNetUsers_UserId1",
                table: "RedeemHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_RedeemHistories_Projects_ProjectId",
                table: "RedeemHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_RedeemHistories_Projects_ProjectId1",
                table: "RedeemHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_RedeemHistories_RedeemableItems_RedeemableItemId",
                table: "RedeemHistories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RedeemHistories",
                table: "RedeemHistories");

            migrationBuilder.RenameTable(
                name: "RedeemHistories",
                newName: "RedeemHistory");

            migrationBuilder.RenameIndex(
                name: "IX_RedeemHistories_UserId1",
                table: "RedeemHistory",
                newName: "IX_RedeemHistory_UserId1");

            migrationBuilder.RenameIndex(
                name: "IX_RedeemHistories_UserId",
                table: "RedeemHistory",
                newName: "IX_RedeemHistory_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_RedeemHistories_RedeemableItemId",
                table: "RedeemHistory",
                newName: "IX_RedeemHistory_RedeemableItemId");

            migrationBuilder.RenameIndex(
                name: "IX_RedeemHistories_ProjectId1",
                table: "RedeemHistory",
                newName: "IX_RedeemHistory_ProjectId1");

            migrationBuilder.RenameIndex(
                name: "IX_RedeemHistories_ProjectId",
                table: "RedeemHistory",
                newName: "IX_RedeemHistory_ProjectId");

            migrationBuilder.AddColumn<int>(
                name: "PointsRedeemed",
                table: "ProjectUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TasksCompleted",
                table: "ProjectUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RedeemedPoints",
                table: "Leaderboards",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_RedeemHistory",
                table: "RedeemHistory",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RedeemHistory_AspNetUsers_UserId",
                table: "RedeemHistory",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RedeemHistory_AspNetUsers_UserId1",
                table: "RedeemHistory",
                column: "UserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RedeemHistory_Projects_ProjectId",
                table: "RedeemHistory",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RedeemHistory_Projects_ProjectId1",
                table: "RedeemHistory",
                column: "ProjectId1",
                principalTable: "Projects",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RedeemHistory_RedeemableItems_RedeemableItemId",
                table: "RedeemHistory",
                column: "RedeemableItemId",
                principalTable: "RedeemableItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RedeemHistory_AspNetUsers_UserId",
                table: "RedeemHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_RedeemHistory_AspNetUsers_UserId1",
                table: "RedeemHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_RedeemHistory_Projects_ProjectId",
                table: "RedeemHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_RedeemHistory_Projects_ProjectId1",
                table: "RedeemHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_RedeemHistory_RedeemableItems_RedeemableItemId",
                table: "RedeemHistory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RedeemHistory",
                table: "RedeemHistory");

            migrationBuilder.DropColumn(
                name: "PointsRedeemed",
                table: "ProjectUsers");

            migrationBuilder.DropColumn(
                name: "TasksCompleted",
                table: "ProjectUsers");

            migrationBuilder.DropColumn(
                name: "RedeemedPoints",
                table: "Leaderboards");

            migrationBuilder.RenameTable(
                name: "RedeemHistory",
                newName: "RedeemHistories");

            migrationBuilder.RenameIndex(
                name: "IX_RedeemHistory_UserId1",
                table: "RedeemHistories",
                newName: "IX_RedeemHistories_UserId1");

            migrationBuilder.RenameIndex(
                name: "IX_RedeemHistory_UserId",
                table: "RedeemHistories",
                newName: "IX_RedeemHistories_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_RedeemHistory_RedeemableItemId",
                table: "RedeemHistories",
                newName: "IX_RedeemHistories_RedeemableItemId");

            migrationBuilder.RenameIndex(
                name: "IX_RedeemHistory_ProjectId1",
                table: "RedeemHistories",
                newName: "IX_RedeemHistories_ProjectId1");

            migrationBuilder.RenameIndex(
                name: "IX_RedeemHistory_ProjectId",
                table: "RedeemHistories",
                newName: "IX_RedeemHistories_ProjectId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RedeemHistories",
                table: "RedeemHistories",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RedeemHistories_AspNetUsers_UserId",
                table: "RedeemHistories",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RedeemHistories_AspNetUsers_UserId1",
                table: "RedeemHistories",
                column: "UserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RedeemHistories_Projects_ProjectId",
                table: "RedeemHistories",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RedeemHistories_Projects_ProjectId1",
                table: "RedeemHistories",
                column: "ProjectId1",
                principalTable: "Projects",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RedeemHistories_RedeemableItems_RedeemableItemId",
                table: "RedeemHistories",
                column: "RedeemableItemId",
                principalTable: "RedeemableItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
