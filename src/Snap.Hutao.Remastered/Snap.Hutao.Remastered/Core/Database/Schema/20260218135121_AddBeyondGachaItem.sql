CREATE TABLE "beyond_gacha_items" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_beyond_gacha_items" PRIMARY KEY,
    "ArchiveId" TEXT NOT NULL,
    "GachaType" INTEGER NOT NULL,
    "ItemId" INTEGER NOT NULL,
    "Time" TEXT NOT NULL,
    "Id" INTEGER NOT NULL,
    "ItemType" TEXT NOT NULL,
    "ItemName" TEXT NOT NULL,
    "ScheduleId" INTEGER NOT NULL,
    "RankType" INTEGER NOT NULL,
    "Uid" TEXT NOT NULL,
    "IsUp" INTEGER NOT NULL,
    CONSTRAINT "FK_beyond_gacha_items_gacha_archives_ArchiveId" FOREIGN KEY ("ArchiveId") REFERENCES "gacha_archives" ("InnerId") ON DELETE CASCADE
);

CREATE INDEX "IX_beyond_gacha_items_ArchiveId" ON "beyond_gacha_items" ("ArchiveId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260218135121_AddBeyondGachaItem', '11.0.0-rc.3.26522.148');
