CREATE TABLE "game_accounts" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_game_accounts" PRIMARY KEY,
    "AttachUid" TEXT NULL,
    "Type" INTEGER NOT NULL,
    "Name" TEXT NOT NULL,
    "MihoyoSDK" TEXT NOT NULL
);
