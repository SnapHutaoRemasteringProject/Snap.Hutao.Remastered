CREATE TABLE "GachaArchives" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_GachaArchives" PRIMARY KEY,
    "Uid" TEXT NOT NULL,
    "IsSelected" INTEGER NOT NULL
);

CREATE TABLE "GachaItems" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_GachaItems" PRIMARY KEY,
    "ArchiveId" TEXT NOT NULL,
    "GachaType" INTEGER NOT NULL,
    "ItemId" INTEGER NOT NULL,
    "Time" TEXT NOT NULL,
    "Id" INTEGER NOT NULL,
    CONSTRAINT "FK_GachaItems_GachaArchives_ArchiveId" FOREIGN KEY ("ArchiveId") REFERENCES "GachaArchives" ("InnerId") ON DELETE CASCADE
);

CREATE INDEX "IX_GachaItems_ArchiveId" ON "GachaItems" ("ArchiveId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20220910080051_AddGacha', '11.0.0-rc.3.26522.148');
