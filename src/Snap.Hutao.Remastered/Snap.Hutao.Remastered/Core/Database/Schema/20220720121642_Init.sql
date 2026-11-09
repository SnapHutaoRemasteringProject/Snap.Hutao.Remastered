CREATE TABLE "settings" (
    "Key" TEXT NOT NULL CONSTRAINT "PK_settings" PRIMARY KEY,
    "Value" TEXT NULL
);

CREATE TABLE "users" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_users" PRIMARY KEY,
    "IsSelected" INTEGER NOT NULL,
    "Cookie" TEXT NULL
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20220720121642_Init', '11.0.0-rc.3.26522.148');
