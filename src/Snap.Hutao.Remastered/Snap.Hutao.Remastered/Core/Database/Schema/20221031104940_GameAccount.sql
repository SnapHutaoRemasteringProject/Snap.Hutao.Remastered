CREATE TABLE "game_accounts" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_game_accounts" PRIMARY KEY,
    "AttachUid" TEXT NULL,
    "Type" INTEGER NOT NULL,
    "Name" TEXT NOT NULL,
    "MihoyoSDK" TEXT NOT NULL
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20221031104940_GameAccount', '11.0.0-rc.3.26522.148');
