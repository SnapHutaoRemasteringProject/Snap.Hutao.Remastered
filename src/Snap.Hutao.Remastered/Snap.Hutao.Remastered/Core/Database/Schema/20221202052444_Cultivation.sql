CREATE TABLE "cultivate_projects" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_cultivate_projects" PRIMARY KEY,
    "IsSelected" INTEGER NOT NULL,
    "Name" TEXT NOT NULL
);

CREATE TABLE "cultivate_entries" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_cultivate_entries" PRIMARY KEY,
    "ProjectId" TEXT NOT NULL,
    "Type" INTEGER NOT NULL,
    "Id" INTEGER NOT NULL,
    CONSTRAINT "FK_cultivate_entries_cultivate_projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "cultivate_projects" ("InnerId") ON DELETE CASCADE
);

CREATE TABLE "cultivate_items" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_cultivate_items" PRIMARY KEY,
    "EntryId" TEXT NOT NULL,
    "ItemId" INTEGER NOT NULL,
    "Count" INTEGER NOT NULL,
    CONSTRAINT "FK_cultivate_items_cultivate_entries_EntryId" FOREIGN KEY ("EntryId") REFERENCES "cultivate_entries" ("InnerId") ON DELETE CASCADE
);

CREATE INDEX "IX_cultivate_entries_ProjectId" ON "cultivate_entries" ("ProjectId");

CREATE INDEX "IX_cultivate_items_EntryId" ON "cultivate_items" ("EntryId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20221202052444_Cultivation', '11.0.0-rc.3.26522.148');
