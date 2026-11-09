ALTER TABLE "users" ADD "PreferredUid" TEXT NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240219020258_AddPerferredUidOnUser', '11.0.0-rc.3.26522.148');
