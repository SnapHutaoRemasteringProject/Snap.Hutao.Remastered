INSERT INTO beyond_gacha_items (InnerId, ArchiveId, GachaType, QueryType, ItemId, Time, Id, ScheduleId, RankType, Uid, IsUp)
SELECT InnerId, ArchiveId, GachaType, QueryType, ItemId, Time, Id, 0, 0, '', 0
FROM gacha_items
WHERE QueryType IN (1000, 2000);

DELETE FROM gacha_items WHERE QueryType IN (1000, 2000);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260613055435_MigrateCloudGachaItemToBeyondGachaItem', '11.0.0-rc.3.26522.148');
