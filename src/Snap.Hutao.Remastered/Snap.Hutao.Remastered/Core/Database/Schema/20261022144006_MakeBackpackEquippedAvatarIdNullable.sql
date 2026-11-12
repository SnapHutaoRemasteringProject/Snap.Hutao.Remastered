CREATE TABLE "ef_temp_backpack_items" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_backpack_items" PRIMARY KEY,
    "AppendPropIdListJson" TEXT NULL,
    "ArchiveId" TEXT NOT NULL,
    "Count" INTEGER NOT NULL,
    "DefiniteAppendPropIdListJson" TEXT NULL,
    "EquippedAvatarId" INTEGER NULL,
    "Guid" INTEGER NOT NULL,
    "IsLocked" INTEGER NOT NULL,
    "IsMarked" INTEGER NOT NULL,
    "ItemId" INTEGER NOT NULL,
    "Level" INTEGER NOT NULL,
    "MainPropId" INTEGER NULL,
    "PromoteLevel" INTEGER NOT NULL,
    "PurchasedAppendPropIdListJson" TEXT NULL,
    "RefinementRank" INTEGER NOT NULL,
    CONSTRAINT "FK_backpack_items_backpack_archives_ArchiveId" FOREIGN KEY ("ArchiveId") REFERENCES "backpack_archives" ("InnerId") ON DELETE CASCADE
);

INSERT INTO "ef_temp_backpack_items" ("InnerId", "AppendPropIdListJson", "ArchiveId", "Count", "DefiniteAppendPropIdListJson", "EquippedAvatarId", "Guid", "IsLocked", "IsMarked", "ItemId", "Level", "MainPropId", "PromoteLevel", "PurchasedAppendPropIdListJson", "RefinementRank")
SELECT "InnerId", "AppendPropIdListJson", "ArchiveId", "Count", "DefiniteAppendPropIdListJson", "EquippedAvatarId", "Guid", "IsLocked", "IsMarked", "ItemId", "Level", "MainPropId", "PromoteLevel", "PurchasedAppendPropIdListJson", "RefinementRank"
FROM "backpack_items";

PRAGMA foreign_keys = 0;

DROP TABLE "backpack_items";

ALTER TABLE "ef_temp_backpack_items" RENAME TO "backpack_items";

PRAGMA foreign_keys = 1;

CREATE INDEX "IX_backpack_items_ArchiveId" ON "backpack_items" ("ArchiveId");
