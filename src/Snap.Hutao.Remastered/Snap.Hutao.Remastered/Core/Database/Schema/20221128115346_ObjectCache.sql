CREATE TABLE "object_cache" (
    "Key" TEXT NOT NULL CONSTRAINT "PK_object_cache" PRIMARY KEY,
    "ExpireTime" TEXT NOT NULL,
    "Value" TEXT NULL
);
