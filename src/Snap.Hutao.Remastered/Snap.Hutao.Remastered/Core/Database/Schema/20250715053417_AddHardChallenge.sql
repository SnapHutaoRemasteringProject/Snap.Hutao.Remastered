CREATE TABLE "hard_challenges" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_hard_challenges" PRIMARY KEY,
    "ScheduleId" INTEGER NOT NULL,
    "Uid" TEXT NOT NULL,
    "HardChallengeData" TEXT NOT NULL
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250715053417_AddHardChallenge', '11.0.0-rc.3.26522.148');
