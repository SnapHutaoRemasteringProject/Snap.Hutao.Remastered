CREATE TABLE "settings" (
    "Key" TEXT NOT NULL CONSTRAINT "PK_settings" PRIMARY KEY,
    "Value" TEXT NULL
);

CREATE TABLE "users" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_users" PRIMARY KEY,
    "IsSelected" INTEGER NOT NULL,
    "Cookie" TEXT NULL
);
