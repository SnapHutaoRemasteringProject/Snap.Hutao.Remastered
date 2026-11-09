ALTER TABLE "backpack_items" ADD "DefiniteAppendPropIdListJson" TEXT NULL;

ALTER TABLE "backpack_items" ADD "PurchasedAppendPropIdListJson" TEXT NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260913082551_AddBackpackReliquaryPurchasedDefinitePropList', '11.0.0-rc.3.26522.148');
