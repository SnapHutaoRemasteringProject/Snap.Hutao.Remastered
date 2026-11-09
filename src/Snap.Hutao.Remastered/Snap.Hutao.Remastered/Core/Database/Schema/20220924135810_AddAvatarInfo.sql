CREATE TABLE "avatar_infos" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_avatar_infos" PRIMARY KEY,
    "Uid" TEXT NOT NULL,
    "Info" TEXT NOT NULL
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20220924135810_AddAvatarInfo', '11.0.0-rc.3.26522.148');
