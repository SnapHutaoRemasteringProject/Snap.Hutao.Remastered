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

            // Adding the column backfilled every pre-existing item with its default value, marking
            // items as "not equipped" when their equipment state was never actually collected.
            // Reset those values to unknown, but only for archives that hold no collected state at
            // all: a non-zero value can only have been written by a refresh, so an archive that has
            // one was refreshed and its zeros mean "not equipped" for real.
            migrationBuilder.Sql(
                """
                UPDATE backpack_items
                SET EquippedAvatarId = NULL
                WHERE EquippedAvatarId = 0
                  AND ArchiveId NOT IN (SELECT ArchiveId FROM backpack_items WHERE EquippedAvatarId > 0);
                """);
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
