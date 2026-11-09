CREATE TABLE "spiral_abysses" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_spiral_abysses" PRIMARY KEY,
    "ScheduleId" INTEGER NOT NULL,
    "Uid" TEXT NOT NULL,
    "SpiralAbyss" TEXT NOT NULL
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20221231104727_SpiralAbyssEntry', '11.0.0-rc.3.26522.148');
