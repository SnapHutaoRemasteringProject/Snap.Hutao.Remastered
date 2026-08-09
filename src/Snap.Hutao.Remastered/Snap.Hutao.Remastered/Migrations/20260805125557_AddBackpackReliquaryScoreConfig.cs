using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Snap.Hutao.Remastered.Migrations
{
    /// <inheritdoc />
    public partial class AddBackpackReliquaryScoreConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "backpack_reliquary_score_config",
                columns: table => new
                {
                    InnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PresetKey = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CritWeight = table.Column<double>(type: "REAL", nullable: false),
                    CritHurtWeight = table.Column<double>(type: "REAL", nullable: false),
                    AttackPercentWeight = table.Column<double>(type: "REAL", nullable: false),
                    ChargeEfficiencyWeight = table.Column<double>(type: "REAL", nullable: false),
                    ElementalMasteryWeight = table.Column<double>(type: "REAL", nullable: false),
                    HpPercentWeight = table.Column<double>(type: "REAL", nullable: false),
                    DefensePercentWeight = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backpack_reliquary_score_config", x => x.InnerId);
                });

            migrationBuilder.InsertData(
                table: "backpack_reliquary_score_config",
                columns: ["InnerId", "PresetKey", "Name", "IsActive", "CritWeight", "CritHurtWeight", "AttackPercentWeight", "ChargeEfficiencyWeight", "ElementalMasteryWeight", "HpPercentWeight", "DefensePercentWeight"],
                values: new object[] { Guid.NewGuid(), 0, string.Empty, true, 1.0, 1.0, 0.2, 0.2, 0.2, 0.0, 0.0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "backpack_reliquary_score_config");
        }
    }
}
