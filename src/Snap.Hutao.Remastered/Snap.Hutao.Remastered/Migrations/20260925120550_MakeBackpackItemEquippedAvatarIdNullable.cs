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

            // The backfill of pre-existing rows lives in a separate migration
            // (BackfillBackpackItemEquippedAvatarIdUnknown). SQLite performs AlterColumn with a
            // table rebuild, which EF Core defers to the end of the migration, so a raw SqlOperation
            // placed here would run while the column is still NOT NULL and fail on non-empty tables.
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
