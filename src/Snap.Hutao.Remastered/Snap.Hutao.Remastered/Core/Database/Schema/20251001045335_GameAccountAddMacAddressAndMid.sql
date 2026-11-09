ALTER TABLE "game_accounts" ADD "MacAddress" TEXT NULL;

ALTER TABLE "game_accounts" ADD "Mid" TEXT NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251001045335_GameAccountAddMacAddressAndMid', '11.0.0-rc.3.26522.148');
