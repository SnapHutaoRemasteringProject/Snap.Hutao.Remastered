ALTER TABLE "avatar_infos" ADD "CalculatorRefreshTime" TEXT NOT NULL DEFAULT '0001-01-01 00:00:00+00:00';

ALTER TABLE "avatar_infos" ADD "GameRecordRefreshTime" TEXT NOT NULL DEFAULT '0001-01-01 00:00:00+00:00';

ALTER TABLE "avatar_infos" ADD "ShowcaseRefreshTime" TEXT NOT NULL DEFAULT '0001-01-01 00:00:00+00:00';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20230827040435_AddRefreshTimeOnAvatarInfo', '11.0.0-rc.3.26522.148');
