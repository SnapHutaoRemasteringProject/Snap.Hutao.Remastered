ALTER TABLE "cultivate_entry_level_informations" ADD "WeaponIsPromoting" INTEGER NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250718111131_AddCultivationEntryLevelInformationWeaponIsPromoting', '11.0.0-rc.3.26522.148');
