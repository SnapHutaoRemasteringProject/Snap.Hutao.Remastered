CREATE TABLE "cultivate_entry_level_informations" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_cultivate_entry_level_informations" PRIMARY KEY,
    "EntryId" TEXT NOT NULL,
    "AvatarLevelFrom" INTEGER NOT NULL,
    "AvatarLevelTo" INTEGER NOT NULL,
    "SkillALevelFrom" INTEGER NOT NULL,
    "SkillALevelTo" INTEGER NOT NULL,
    "SkillELevelFrom" INTEGER NOT NULL,
    "SkillELevelTo" INTEGER NOT NULL,
    "SkillQLevelFrom" INTEGER NOT NULL,
    "SkillQLevelTo" INTEGER NOT NULL,
    "WeaponLevelFrom" INTEGER NOT NULL,
    "WeaponLevelTo" INTEGER NOT NULL,
    CONSTRAINT "FK_cultivate_entry_level_informations_cultivate_entries_EntryId" FOREIGN KEY ("EntryId") REFERENCES "cultivate_entries" ("InnerId") ON DELETE CASCADE
);

CREATE INDEX "IX_cultivate_entry_level_informations_EntryId" ON "cultivate_entry_level_informations" ("EntryId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20231207085530_AddCultivateEntryLevelInformation', '11.0.0-rc.3.26522.148');
