CREATE TABLE "avatar_strategies" (
    "AvatarId" INTEGER NOT NULL CONSTRAINT "PK_avatar_strategies" PRIMARY KEY AUTOINCREMENT,
    "ChineseStrategyId" INTEGER NOT NULL,
    "OverseaStrategyId" INTEGER NOT NULL
);
