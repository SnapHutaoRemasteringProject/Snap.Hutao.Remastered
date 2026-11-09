CREATE TABLE "role_combats" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_role_combats" PRIMARY KEY,
    "ScheduleId" INTEGER NOT NULL,
    "Uid" TEXT NOT NULL,
    "RoleCombatData" TEXT NOT NULL
);

CREATE TABLE "ef_temp_game_accounts" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_game_accounts" PRIMARY KEY,
    "Index" INTEGER NOT NULL,
    "MihoyoSDK" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Type" INTEGER NOT NULL
);

INSERT INTO "ef_temp_game_accounts" ("InnerId", "Index", "MihoyoSDK", "Name", "Type")
SELECT "InnerId", "Index", "MihoyoSDK", "Name", "Type"
FROM "game_accounts";

CREATE TABLE "ef_temp_avatar_infos" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_avatar_infos" PRIMARY KEY,
    "Info2" TEXT NULL,
    "RefreshTime" TEXT NOT NULL,
    "Uid" TEXT NOT NULL
);

INSERT INTO "ef_temp_avatar_infos" ("InnerId", "Info2", "RefreshTime", "Uid")
SELECT "InnerId", "Info2", "RefreshTime", "Uid"
FROM "avatar_infos";

PRAGMA foreign_keys = 0;

DROP TABLE "game_accounts";

ALTER TABLE "ef_temp_game_accounts" RENAME TO "game_accounts";

DROP TABLE "avatar_infos";

ALTER TABLE "ef_temp_avatar_infos" RENAME TO "avatar_infos";

PRAGMA foreign_keys = 1;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20241107151107_AddRoleCombatEntry', '11.0.0-rc.3.26522.148');
