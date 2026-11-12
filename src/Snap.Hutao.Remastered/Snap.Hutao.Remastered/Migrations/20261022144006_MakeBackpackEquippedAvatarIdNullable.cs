using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Snap.Hutao.Remastered.Migrations;

/// <inheritdoc />
public partial class _20261022144006_MakeBackpackEquippedAvatarIdNullable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The original migration was edited in place after release. Databases
        // that had already applied it still have the NOT NULL/default-0 column.
        migrationBuilder.AlterColumn<uint>(
            name: "EquippedAvatarId",
            table: "backpack_items",
            type: "INTEGER",
            nullable: true,
            oldClrType: typeof(uint),
            oldType: "INTEGER",
            oldDefaultValue: 0U);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // This repairs the nullable contract already present in the previous
        // model. Do not reintroduce the invalid constraint or discard unknowns.
    }
}
