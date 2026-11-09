ALTER TABLE "cultivate_items" ADD "IsFinished" INTEGER NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20221217061817_ItemFinishable', '11.0.0-rc.3.26522.148');
