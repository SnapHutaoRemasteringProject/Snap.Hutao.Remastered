ALTER TABLE "daily_notes" ADD "RefreshTime" TEXT NOT NULL DEFAULT '0001-01-01 00:00:00+00:00';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20230824084447_AddRefreshTimeOnDailyNoteEntry', '11.0.0-rc.3.26522.148');
