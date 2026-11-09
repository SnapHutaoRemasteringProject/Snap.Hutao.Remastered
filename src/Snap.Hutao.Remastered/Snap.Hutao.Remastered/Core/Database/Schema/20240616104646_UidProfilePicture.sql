CREATE TABLE "uid_profile_pictures" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_uid_profile_pictures" PRIMARY KEY,
    "Uid" TEXT NOT NULL,
    "ProfilePictureId" INTEGER NOT NULL,
    "AvatarId" INTEGER NOT NULL,
    "CostumeId" INTEGER NOT NULL,
    "RefreshTime" TEXT NOT NULL
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240616104646_UidProfilePicture', '11.0.0-rc.3.26522.148');
