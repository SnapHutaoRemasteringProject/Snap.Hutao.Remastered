ALTER TABLE "GachaItems" RENAME TO "gacha_items";

ALTER TABLE "GachaArchives" RENAME TO "gacha_archives";

DROP INDEX "IX_GachaItems_ArchiveId";

CREATE INDEX "IX_gacha_items_ArchiveId" ON "gacha_items" ("ArchiveId");

CREATE TABLE "ef_temp_gacha_items" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_gacha_items" PRIMARY KEY,
    "ArchiveId" TEXT NOT NULL,
    "GachaType" INTEGER NOT NULL,
    "Id" INTEGER NOT NULL,
    "ItemId" INTEGER NOT NULL,
    "QueryType" INTEGER NOT NULL,
    "Time" TEXT NOT NULL,
    CONSTRAINT "FK_gacha_items_gacha_archives_ArchiveId" FOREIGN KEY ("ArchiveId") REFERENCES "gacha_archives" ("InnerId") ON DELETE CASCADE
);

INSERT INTO "ef_temp_gacha_items" ("InnerId", "ArchiveId", "GachaType", "Id", "ItemId", "QueryType", "Time")
SELECT "InnerId", "ArchiveId", "GachaType", "Id", "ItemId", "QueryType", "Time"
FROM "gacha_items";

CREATE TABLE "ef_temp_gacha_archives" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_gacha_archives" PRIMARY KEY,
    "IsSelected" INTEGER NOT NULL,
    "Uid" TEXT NOT NULL
);

INSERT INTO "ef_temp_gacha_archives" ("InnerId", "IsSelected", "Uid")
SELECT "InnerId", "IsSelected", "Uid"
FROM "gacha_archives";

PRAGMA foreign_keys = 0;

DROP TABLE "gacha_items";

ALTER TABLE "ef_temp_gacha_items" RENAME TO "gacha_items";

DROP TABLE "gacha_archives";

ALTER TABLE "ef_temp_gacha_archives" RENAME TO "gacha_archives";

PRAGMA foreign_keys = 1;

CREATE INDEX "IX_gacha_items_ArchiveId" ON "gacha_items" ("ArchiveId");
