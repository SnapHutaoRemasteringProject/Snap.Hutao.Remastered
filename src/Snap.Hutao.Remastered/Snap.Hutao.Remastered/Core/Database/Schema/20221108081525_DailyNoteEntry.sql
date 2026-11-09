CREATE TABLE "daily_notes" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_daily_notes" PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "Uid" TEXT NOT NULL,
    "DailyNote" TEXT NULL,
    "ResinNotifyThreshold" INTEGER NOT NULL,
    "ResinNotifySuppressed" INTEGER NOT NULL,
    "HomeCoinNotifyThreshold" INTEGER NOT NULL,
    "HomeCoinNotifySuppressed" INTEGER NOT NULL,
    "TransformerNotify" INTEGER NOT NULL,
    "TransformerNotifySuppressed" INTEGER NOT NULL,
    "DailyTaskNotify" INTEGER NOT NULL,
    "DailyTaskNotifySuppressed" INTEGER NOT NULL,
    "ExpeditionNotify" INTEGER NOT NULL,
    "ExpeditionNotifySuppressed" INTEGER NOT NULL,
    "ShowInHomeWidget" INTEGER NOT NULL,
    CONSTRAINT "FK_daily_notes_users_UserId" FOREIGN KEY ("UserId") REFERENCES "users" ("InnerId") ON DELETE CASCADE
);

CREATE INDEX "IX_daily_notes_UserId" ON "daily_notes" ("UserId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20221108081525_DailyNoteEntry', '11.0.0-rc.3.26522.148');
