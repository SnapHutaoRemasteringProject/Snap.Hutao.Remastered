CREATE TABLE "avatar_strategies" (
    "AvatarId" INTEGER NOT NULL CONSTRAINT "PK_avatar_strategies" PRIMARY KEY AUTOINCREMENT,
    "ChineseStrategyId" INTEGER NOT NULL,
    "OverseaStrategyId" INTEGER NOT NULL
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250109084513_AddAvatarStrategy', '11.0.0-rc.3.26522.148');
