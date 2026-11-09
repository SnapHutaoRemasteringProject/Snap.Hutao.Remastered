ALTER TABLE "users" ADD "CookieTokenLastUpdateTime" TEXT NOT NULL DEFAULT '0001-01-01 00:00:00+00:00';

ALTER TABLE "users" ADD "FingerprintLastUpdateTime" TEXT NOT NULL DEFAULT '0001-01-01 00:00:00+00:00';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20231126113631_AddUserLastUpdateTime', '11.0.0-rc.3.26522.148');
