using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Snap.Hutao.Remastered.Migrations
{
    /// <inheritdoc />
    public partial class AddBackpack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "backpack_archives",
                columns: table => new
                {
                    InnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    IsSelected = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backpack_archives", x => x.InnerId);
                });

            migrationBuilder.CreateTable(
                name: "backpack_items",
                columns: table => new
                {
                    InnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ArchiveId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ItemId = table.Column<uint>(type: "INTEGER", nullable: false),
                    Guid = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Count = table.Column<uint>(type: "INTEGER", nullable: false),
                    Level = table.Column<uint>(type: "INTEGER", nullable: false),
                    PromoteLevel = table.Column<uint>(type: "INTEGER", nullable: false),
                    RefinementRank = table.Column<uint>(type: "INTEGER", nullable: false),
                    MainPropId = table.Column<uint>(type: "INTEGER", nullable: true),
                    AppendPropIdListJson = table.Column<string>(type: "TEXT", nullable: true),
                    IsLocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsMarked = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backpack_items", x => x.InnerId);
                    table.ForeignKey(
                        name: "FK_backpack_items_backpack_archives_ArchiveId",
                        column: x => x.ArchiveId,
                        principalTable: "backpack_archives",
                        principalColumn: "InnerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_backpack_items_ArchiveId",
                table: "backpack_items",
                column: "ArchiveId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "backpack_items");

            migrationBuilder.DropTable(
                name: "backpack_archives");
        }
    }
}
