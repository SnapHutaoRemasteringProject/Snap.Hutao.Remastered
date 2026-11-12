ALTER TABLE "avatar_infos" ADD "Info2" TEXT NOT NULL DEFAULT '';

ALTER TABLE "avatar_infos" ADD "RefreshTime" TEXT NOT NULL DEFAULT '0001-01-01 00:00:00+00:00';

CREATE TABLE "ef_temp_cultivate_projects" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_cultivate_projects" PRIMARY KEY,
    "IsSelected" INTEGER NOT NULL,
    "Name" TEXT NOT NULL
);

INSERT INTO "ef_temp_cultivate_projects" ("InnerId", "IsSelected", "Name")
SELECT "InnerId", "IsSelected", "Name"
FROM "cultivate_projects";

CREATE TABLE "ef_temp_avatar_infos" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_avatar_infos" PRIMARY KEY,
    "Info2" TEXT NOT NULL,
    "RefreshTime" TEXT NOT NULL,
    "Uid" TEXT NOT NULL
);

INSERT INTO "ef_temp_avatar_infos" ("InnerId", "Info2", "RefreshTime", "Uid")
SELECT "InnerId", "Info2", "RefreshTime", "Uid"
FROM "avatar_infos";

PRAGMA foreign_keys = 0;

DROP TABLE "cultivate_projects";

ALTER TABLE "ef_temp_cultivate_projects" RENAME TO "cultivate_projects";

DROP TABLE "avatar_infos";

ALTER TABLE "ef_temp_avatar_infos" RENAME TO "avatar_infos";

PRAGMA foreign_keys = 1;
