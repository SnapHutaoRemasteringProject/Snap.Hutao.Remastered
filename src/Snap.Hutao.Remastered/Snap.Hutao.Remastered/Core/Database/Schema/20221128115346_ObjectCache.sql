CREATE TABLE "object_cache" (
    "Key" TEXT NOT NULL CONSTRAINT "PK_object_cache" PRIMARY KEY,
    "ExpireTime" TEXT NOT NULL,
    "Value" TEXT NULL
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20221128115346_ObjectCache', '11.0.0-rc.3.26522.148');
