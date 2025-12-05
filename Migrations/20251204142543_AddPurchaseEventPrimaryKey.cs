using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace COMP_2139_Assignment_1.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseEventPrimaryKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PurchaseEventId",
                table: "PurchaseEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PurchaseEventId",
                table: "PurchaseEvents");
        }
    }
}
