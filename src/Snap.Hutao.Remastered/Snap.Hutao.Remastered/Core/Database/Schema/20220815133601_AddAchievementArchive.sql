ALTER TABLE "achievements" RENAME COLUMN "UserId" TO "ArchiveId";

DROP INDEX "IX_achievements_UserId";

CREATE INDEX "IX_achievements_ArchiveId" ON "achievements" ("ArchiveId");

ALTER TABLE "achievements" ADD "Status" INTEGER NOT NULL DEFAULT 0;

CREATE TABLE "achievement_archives" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_achievement_archives" PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "IsSelected" INTEGER NOT NULL
);

CREATE TABLE "ef_temp_achievements" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_achievements" PRIMARY KEY,
    "ArchiveId" TEXT NOT NULL,
    "Current" INTEGER NOT NULL,
    "Id" INTEGER NOT NULL,
    "Status" INTEGER NOT NULL,
    "Time" TEXT NOT NULL,
    CONSTRAINT "FK_achievements_achievement_archives_ArchiveId" FOREIGN KEY ("ArchiveId") REFERENCES "achievement_archives" ("InnerId") ON DELETE CASCADE
);

INSERT INTO "ef_temp_achievements" ("InnerId", "ArchiveId", "Current", "Id", "Status", "Time")
SELECT "InnerId", "ArchiveId", "Current", "Id", "Status", "Time"
FROM "achievements";

PRAGMA foreign_keys = 0;

DROP TABLE "achievements";

ALTER TABLE "ef_temp_achievements" RENAME TO "achievements";

PRAGMA foreign_keys = 1;

CREATE INDEX "IX_achievements_ArchiveId" ON "achievements" ("ArchiveId");
