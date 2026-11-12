CREATE TABLE "backpack_archives" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_backpack_archives" PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "IsSelected" INTEGER NOT NULL
);

CREATE TABLE "backpack_items" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_backpack_items" PRIMARY KEY,
    "ArchiveId" TEXT NOT NULL,
    "ItemId" INTEGER NOT NULL,
    "Guid" INTEGER NOT NULL,
    "Count" INTEGER NOT NULL,
    "Level" INTEGER NOT NULL,
    "PromoteLevel" INTEGER NOT NULL,
    "RefinementRank" INTEGER NOT NULL,
    "MainPropId" INTEGER NULL,
    "AppendPropIdListJson" TEXT NULL,
    "IsLocked" INTEGER NOT NULL,
    "IsMarked" INTEGER NOT NULL,
    CONSTRAINT "FK_backpack_items_backpack_archives_ArchiveId" FOREIGN KEY ("ArchiveId") REFERENCES "backpack_archives" ("InnerId") ON DELETE CASCADE
);

CREATE INDEX "IX_backpack_items_ArchiveId" ON "backpack_items" ("ArchiveId");
