ALTER TABLE "daily_notes" ADD "DailyTaskDotVisible" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "daily_notes" ADD "ExpeditionDotVisible" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "daily_notes" ADD "HomeCoinDotVisible" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "daily_notes" ADD "ResinDotVisible" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "daily_notes" ADD "TransformerDotVisible" INTEGER NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250929023624_DailyNoteRedDotVisible', '11.0.0-rc.3.26522.148');
