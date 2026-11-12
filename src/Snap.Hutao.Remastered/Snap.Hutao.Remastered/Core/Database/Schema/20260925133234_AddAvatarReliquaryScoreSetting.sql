CREATE TABLE "avatar_reliquary_score_setting" (
    "AvatarId" INTEGER NOT NULL CONSTRAINT "PK_avatar_reliquary_score_setting" PRIMARY KEY AUTOINCREMENT,
    "Algorithm" INTEGER NOT NULL,
    "PresetKey" INTEGER NOT NULL,
    "ConfigId" TEXT NOT NULL
);
