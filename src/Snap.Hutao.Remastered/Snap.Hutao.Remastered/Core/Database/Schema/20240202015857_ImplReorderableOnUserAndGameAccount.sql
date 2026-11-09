DROP INDEX "IX_cultivate_entry_level_informations_EntryId";

ALTER TABLE "users" ADD "Index" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "game_accounts" ADD "Index" INTEGER NOT NULL DEFAULT 0;

CREATE UNIQUE INDEX "IX_cultivate_entry_level_informations_EntryId" ON "cultivate_entry_level_informations" ("EntryId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240202015857_ImplReorderableOnUserAndGameAccount', '11.0.0-rc.3.26522.148');
