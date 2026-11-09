CREATE TABLE "avatar_reliquary_score_setting" (
    "AvatarId" INTEGER NOT NULL CONSTRAINT "PK_avatar_reliquary_score_setting" PRIMARY KEY AUTOINCREMENT,
    "Algorithm" INTEGER NOT NULL,
    "PresetKey" INTEGER NOT NULL,
    "ConfigId" TEXT NOT NULL
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260925133234_AddAvatarReliquaryScoreSetting', '11.0.0-rc.3.26522.148');
