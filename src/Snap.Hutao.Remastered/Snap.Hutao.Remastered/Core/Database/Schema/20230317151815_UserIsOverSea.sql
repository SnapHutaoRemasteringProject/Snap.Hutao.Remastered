ALTER TABLE "users" ADD "IsOversea" INTEGER NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20230317151815_UserIsOverSea', '11.0.0-rc.3.26522.148');
