using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Snap.Hutao.Remastered.Migrations
{
    /// <inheritdoc />
    public partial class AddBackpackReliquaryPurchasedDefinitePropList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefiniteAppendPropIdListJson",
                table: "backpack_items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurchasedAppendPropIdListJson",
                table: "backpack_items",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefiniteAppendPropIdListJson",
                table: "backpack_items");

            migrationBuilder.DropColumn(
                name: "PurchasedAppendPropIdListJson",
                table: "backpack_items");
        }
    }
}
