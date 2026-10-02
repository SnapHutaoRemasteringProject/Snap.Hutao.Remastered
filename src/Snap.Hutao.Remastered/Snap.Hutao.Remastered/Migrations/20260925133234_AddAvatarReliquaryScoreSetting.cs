using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Snap.Hutao.Remastered.Migrations
{
    /// <inheritdoc />
    public partial class AddAvatarReliquaryScoreSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "avatar_reliquary_score_setting",
                columns: table => new
                {
                    AvatarId = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Algorithm = table.Column<int>(type: "INTEGER", nullable: false),
                    PresetKey = table.Column<int>(type: "INTEGER", nullable: false),
                    ConfigId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_avatar_reliquary_score_setting", x => x.AvatarId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "avatar_reliquary_score_setting");
        }
    }
}
