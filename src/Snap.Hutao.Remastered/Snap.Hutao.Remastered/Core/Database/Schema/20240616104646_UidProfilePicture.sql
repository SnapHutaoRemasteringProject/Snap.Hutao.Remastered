CREATE TABLE "uid_profile_pictures" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_uid_profile_pictures" PRIMARY KEY,
    "Uid" TEXT NOT NULL,
    "ProfilePictureId" INTEGER NOT NULL,
    "AvatarId" INTEGER NOT NULL,
    "CostumeId" INTEGER NOT NULL,
    "RefreshTime" TEXT NOT NULL
);
