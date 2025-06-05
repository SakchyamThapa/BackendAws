using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SonicPoints.Migrations
{
    /// <inheritdoc />
    public partial class AddQuantityToRedeemHistoryAndItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "RedeemHistory",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "RedeemableItems",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "RedeemHistory");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "RedeemableItems");
        }
    }
}
