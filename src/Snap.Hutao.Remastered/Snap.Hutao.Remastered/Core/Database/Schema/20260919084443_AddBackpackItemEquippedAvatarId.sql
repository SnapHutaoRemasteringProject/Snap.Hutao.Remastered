ALTER TABLE "backpack_items" ADD "EquippedAvatarId" INTEGER NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260919084443_AddBackpackItemEquippedAvatarId', '11.0.0-rc.3.26522.148');
