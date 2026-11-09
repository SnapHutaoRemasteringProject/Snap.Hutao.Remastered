ALTER TABLE "users" ADD "Stoken" TEXT NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20221118095755_SplitStoken', '11.0.0-rc.3.26522.148');
