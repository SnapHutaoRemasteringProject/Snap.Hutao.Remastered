ALTER TABLE "users" RENAME COLUMN "Cookie" TO "Ltoken";

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20221123060511_RenameCookieToLtoken', '11.0.0-rc.3.26522.148');
