using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Snap.Hutao.Remastered.Migrations
{
    /// <inheritdoc />
    public partial class MakeBackpackItemEquippedAvatarIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<uint>(
                name: "EquippedAvatarId",
                table: "backpack_items",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(uint),
                oldType: "INTEGER");

            // Backfilling the column with its default value marked every pre-existing item as
            // "not equipped" instead of "unknown". Reset those values so the equipment state of
            // archives that were never refreshed stays unknown rather than being misreported.
            migrationBuilder.Sql("UPDATE backpack_items SET EquippedAvatarId = NULL WHERE EquippedAvatarId = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The column is not nullable again, so unknown values have to be filled in first.
            migrationBuilder.Sql("UPDATE backpack_items SET EquippedAvatarId = 0 WHERE EquippedAvatarId IS NULL;");

            migrationBuilder.AlterColumn<uint>(
                name: "EquippedAvatarId",
                table: "backpack_items",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u,
                oldClrType: typeof(uint),
                oldType: "INTEGER",
                oldNullable: true);
        }
    }
}
