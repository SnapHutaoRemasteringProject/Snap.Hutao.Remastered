CREATE TABLE "ef_temp_daily_notes" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_daily_notes" PRIMARY KEY,
    "DailyNote" TEXT NULL,
    "DailyTaskNotify" INTEGER NOT NULL,
    "DailyTaskNotifySuppressed" INTEGER NOT NULL,
    "ExpeditionNotify" INTEGER NOT NULL,
    "ExpeditionNotifySuppressed" INTEGER NOT NULL,
    "HomeCoinNotifySuppressed" INTEGER NOT NULL,
    "HomeCoinNotifyThreshold" INTEGER NOT NULL,
    "ResinNotifySuppressed" INTEGER NOT NULL,
    "ResinNotifyThreshold" INTEGER NOT NULL,
    "TransformerNotify" INTEGER NOT NULL,
    "TransformerNotifySuppressed" INTEGER NOT NULL,
    "Uid" TEXT NOT NULL,
    "UserId" TEXT NOT NULL,
    CONSTRAINT "FK_daily_notes_users_UserId" FOREIGN KEY ("UserId") REFERENCES "users" ("InnerId") ON DELETE CASCADE
);

INSERT INTO "ef_temp_daily_notes" ("InnerId", "DailyNote", "DailyTaskNotify", "DailyTaskNotifySuppressed", "ExpeditionNotify", "ExpeditionNotifySuppressed", "HomeCoinNotifySuppressed", "HomeCoinNotifyThreshold", "ResinNotifySuppressed", "ResinNotifyThreshold", "TransformerNotify", "TransformerNotifySuppressed", "Uid", "UserId")
SELECT "InnerId", "DailyNote", "DailyTaskNotify", "DailyTaskNotifySuppressed", "ExpeditionNotify", "ExpeditionNotifySuppressed", "HomeCoinNotifySuppressed", "HomeCoinNotifyThreshold", "ResinNotifySuppressed", "ResinNotifyThreshold", "TransformerNotify", "TransformerNotifySuppressed", "Uid", "UserId"
FROM "daily_notes";

PRAGMA foreign_keys = 0;

DROP TABLE "daily_notes";

ALTER TABLE "ef_temp_daily_notes" RENAME TO "daily_notes";

PRAGMA foreign_keys = 1;

CREATE INDEX "IX_daily_notes_UserId" ON "daily_notes" ("UserId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20230506065837_RemoveDailyNoteEntryShowInHomeWidget', '11.0.0-rc.3.26522.148');
