ALTER TABLE "cultivate_projects" ADD "ServerTimeZoneOffset" TEXT NOT NULL DEFAULT '08:00:00';

UPDATE `cultivate_projects` 
                                   SET `ServerTimeZoneOffset` = (
                                       SELECT IFNULL(
                                           (SELECT `Value` 
                                            FROM `settings` 
                                            WHERE `Key` = 'CalendarServerTimeZoneOffset'), 
                                           '08:00:00'
                                       )
                                   );
