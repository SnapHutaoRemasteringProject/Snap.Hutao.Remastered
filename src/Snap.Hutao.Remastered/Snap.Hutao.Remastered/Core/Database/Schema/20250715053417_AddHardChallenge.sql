CREATE TABLE "hard_challenges" (
    "InnerId" TEXT NOT NULL CONSTRAINT "PK_hard_challenges" PRIMARY KEY,
    "ScheduleId" INTEGER NOT NULL,
    "Uid" TEXT NOT NULL,
    "HardChallengeData" TEXT NOT NULL
);
