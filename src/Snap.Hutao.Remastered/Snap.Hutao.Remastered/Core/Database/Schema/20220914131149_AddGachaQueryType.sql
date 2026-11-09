ALTER TABLE "GachaItems" ADD "QueryType" INTEGER NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20220914131149_AddGachaQueryType', '11.0.0-rc.3.26522.148');
