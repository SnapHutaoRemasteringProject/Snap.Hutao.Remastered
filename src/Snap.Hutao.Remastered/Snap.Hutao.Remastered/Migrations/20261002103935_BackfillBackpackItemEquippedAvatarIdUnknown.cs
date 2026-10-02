using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Snap.Hutao.Remastered.Migrations
{
    /// <inheritdoc />
    public partial class BackfillBackpackItemEquippedAvatarIdUnknown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Adding the column in AddBackpackItemEquippedAvatarId backfilled every pre-existing item
            // with its default value (0), marking items as "not equipped" even though their equipment
            // state was never actually collected. Reset those values to unknown, but only for archives
            // that hold no collected state at all: a non-zero value can only have been written by a
            // refresh, so an archive that has one was refreshed and its zeros mean "not equipped" for
            // real.
            //
            // This runs as its own migration because EF Core defers the SQLite table rebuild performed
            // by MakeBackpackItemEquippedAvatarIdNullable to the end of that migration, so a SqlOperation
            // placed there would execute while the column is still NOT NULL and fail on non-empty tables.
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
            // This data reset cannot be reversed: there is no way to tell which zeros were real
            // "not equipped" values and which were backfilled defaults.
        }
    }
}
