CREATE TABLE "achievements" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_achievements" PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "Id" INTEGER NOT NULL,
    "Current" INTEGER NOT NULL,
    "Time" TEXT NOT NULL,
    CONSTRAINT "FK_achievements_users_UserId" FOREIGN KEY ("UserId") REFERENCES "users" ("InnerId") ON DELETE CASCADE
);

CREATE INDEX "IX_achievements_UserId" ON "achievements" ("UserId");
