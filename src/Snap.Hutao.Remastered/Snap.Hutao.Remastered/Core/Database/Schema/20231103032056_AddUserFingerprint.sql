ALTER TABLE "users" ADD "Fingerprint" TEXT NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20231103032056_AddUserFingerprint', '11.0.0-rc.3.26522.148');
