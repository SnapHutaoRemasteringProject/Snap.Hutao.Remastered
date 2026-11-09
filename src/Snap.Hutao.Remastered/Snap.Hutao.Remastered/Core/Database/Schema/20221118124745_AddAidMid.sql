ALTER TABLE "users" ADD "Aid" TEXT NULL;

ALTER TABLE "users" ADD "Mid" TEXT NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20221118124745_AddAidMid', '11.0.0-rc.3.26522.148');
