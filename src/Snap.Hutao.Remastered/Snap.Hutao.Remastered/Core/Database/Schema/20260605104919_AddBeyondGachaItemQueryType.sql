ALTER TABLE "beyond_gacha_items" ADD "QueryType" INTEGER NOT NULL DEFAULT 0;

UPDATE beyond_gacha_items SET QueryType = 2000 WHERE GachaType IN (20011, 20012, 20021, 20022);

UPDATE beyond_gacha_items SET QueryType = GachaType WHERE QueryType = 1000;

CREATE TABLE "ef_temp_beyond_gacha_items" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_beyond_gacha_items" PRIMARY KEY,
    "ArchiveId" TEXT NOT NULL,
    "GachaType" INTEGER NOT NULL,
    "Id" INTEGER NOT NULL,
    "IsUp" INTEGER NOT NULL,
    "ItemId" INTEGER NOT NULL,
    "QueryType" INTEGER NOT NULL,
    "RankType" INTEGER NOT NULL,
    "ScheduleId" INTEGER NOT NULL,
    "Time" TEXT NOT NULL,
    "Uid" TEXT NOT NULL,
    CONSTRAINT "FK_beyond_gacha_items_gacha_archives_ArchiveId" FOREIGN KEY ("ArchiveId") REFERENCES "gacha_archives" ("InnerId") ON DELETE CASCADE
);

INSERT INTO "ef_temp_beyond_gacha_items" ("InnerId", "ArchiveId", "GachaType", "Id", "IsUp", "ItemId", "QueryType", "RankType", "ScheduleId", "Time", "Uid")
SELECT "InnerId", "ArchiveId", "GachaType", "Id", "IsUp", "ItemId", "QueryType", "RankType", "ScheduleId", "Time", "Uid"
FROM "beyond_gacha_items";

PRAGMA foreign_keys = 0;

DROP TABLE "beyond_gacha_items";

ALTER TABLE "ef_temp_beyond_gacha_items" RENAME TO "beyond_gacha_items";

PRAGMA foreign_keys = 1;

CREATE INDEX "IX_beyond_gacha_items_ArchiveId" ON "beyond_gacha_items" ("ArchiveId");
