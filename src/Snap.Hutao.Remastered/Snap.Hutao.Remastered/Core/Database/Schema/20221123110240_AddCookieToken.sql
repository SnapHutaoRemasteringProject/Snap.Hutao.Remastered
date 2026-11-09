ALTER TABLE "users" ADD "CookieToken" TEXT NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20221123110240_AddCookieToken', '11.0.0-rc.3.26522.148');
