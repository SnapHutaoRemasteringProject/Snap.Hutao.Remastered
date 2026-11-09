ALTER TABLE "cultivate_entry_level_informations" ADD "AvatarIsPromoting" INTEGER NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250717232456_AddCultivationEntryLevelInformationAvatarIsPromoting', '11.0.0-rc.3.26522.148');
