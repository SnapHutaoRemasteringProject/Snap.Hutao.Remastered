ALTER TABLE "cultivate_projects" ADD "AttachedUid" TEXT NULL;

CREATE TABLE "inventory_items" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_inventory_items" PRIMARY KEY,
    "ProjectId" TEXT NOT NULL,
    "ItemId" INTEGER NOT NULL,
    "Count" INTEGER NOT NULL,
    CONSTRAINT "FK_inventory_items_cultivate_projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "cultivate_projects" ("InnerId") ON DELETE CASCADE
);

CREATE TABLE "inventory_reliquaries" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_inventory_reliquaries" PRIMARY KEY,
    "ProjectId" TEXT NOT NULL,
    "ItemId" INTEGER NOT NULL,
    "Level" INTEGER NOT NULL,
    "MainPropId" INTEGER NOT NULL,
    "AppendPropIdList" TEXT NOT NULL,
    CONSTRAINT "FK_inventory_reliquaries_cultivate_projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "cultivate_projects" ("InnerId") ON DELETE CASCADE
);

CREATE TABLE "inventory_weapons" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_inventory_weapons" PRIMARY KEY,
    "ProjectId" TEXT NOT NULL,
    "ItemId" INTEGER NOT NULL,
    "Level" INTEGER NOT NULL,
    "PromoteLevel" INTEGER NOT NULL,
    CONSTRAINT "FK_inventory_weapons_cultivate_projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "cultivate_projects" ("InnerId") ON DELETE CASCADE
);

CREATE INDEX "IX_inventory_items_ProjectId" ON "inventory_items" ("ProjectId");

CREATE INDEX "IX_inventory_reliquaries_ProjectId" ON "inventory_reliquaries" ("ProjectId");

CREATE INDEX "IX_inventory_weapons_ProjectId" ON "inventory_weapons" ("ProjectId");
