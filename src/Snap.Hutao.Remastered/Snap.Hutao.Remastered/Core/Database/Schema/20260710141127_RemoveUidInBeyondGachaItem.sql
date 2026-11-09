CREATE TABLE "ef_temp_beyond_gacha_items" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_beyond_gacha_items" PRIMARY KEY,
    "ArchiveId" TEXT NOT NULL,
    "GachaType" INTEGER NOT NULL,
    "Id" INTEGER NOT NULL,
    "IsUp" INTEGER NOT NULL,
    "ItemId" INTEGER NOT NULL,
    "QueryType" INTEGER NOT NULL,
    "ScheduleId" INTEGER NOT NULL,
    "Time" TEXT NOT NULL,
    CONSTRAINT "FK_beyond_gacha_items_gacha_archives_ArchiveId" FOREIGN KEY ("ArchiveId") REFERENCES "gacha_archives" ("InnerId") ON DELETE CASCADE
);

INSERT INTO "ef_temp_beyond_gacha_items" ("InnerId", "ArchiveId", "GachaType", "Id", "IsUp", "ItemId", "QueryType", "ScheduleId", "Time")
SELECT "InnerId", "ArchiveId", "GachaType", "Id", "IsUp", "ItemId", "QueryType", "ScheduleId", "Time"
FROM "beyond_gacha_items";

PRAGMA foreign_keys = 0;

DROP TABLE "beyond_gacha_items";

ALTER TABLE "ef_temp_beyond_gacha_items" RENAME TO "beyond_gacha_items";

PRAGMA foreign_keys = 1;

CREATE INDEX "IX_beyond_gacha_items_ArchiveId" ON "beyond_gacha_items" ("ArchiveId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260710141127_RemoveUidInBeyondGachaItem', '11.0.0-rc.3.26522.148');
