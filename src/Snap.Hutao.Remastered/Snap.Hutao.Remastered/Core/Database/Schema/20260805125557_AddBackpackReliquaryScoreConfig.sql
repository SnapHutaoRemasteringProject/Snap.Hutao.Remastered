CREATE TABLE "backpack_reliquary_score_config" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_backpack_reliquary_score_config" PRIMARY KEY,
    "PresetKey" INTEGER NOT NULL,
    "Name" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CritWeight" REAL NOT NULL,
    "CritHurtWeight" REAL NOT NULL,
    "AttackPercentWeight" REAL NOT NULL,
    "ChargeEfficiencyWeight" REAL NOT NULL,
    "ElementalMasteryWeight" REAL NOT NULL,
    "HpPercentWeight" REAL NOT NULL,
    "DefensePercentWeight" REAL NOT NULL
);

INSERT INTO "backpack_reliquary_score_config" ("InnerId", "PresetKey", "Name", "IsActive", "CritWeight", "CritHurtWeight", "AttackPercentWeight", "ChargeEfficiencyWeight", "ElementalMasteryWeight", "HpPercentWeight", "DefensePercentWeight")
VALUES ('01D3D65E-56DB-4730-9474-575F65133FBA', 0, '', 1, 1.0, 1.0, 0.20000000000000001, 0.20000000000000001, 0.20000000000000001, 0.0, 0.0);
SELECT changes();

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260805125557_AddBackpackReliquaryScoreConfig', '11.0.0-rc.3.26522.148');
