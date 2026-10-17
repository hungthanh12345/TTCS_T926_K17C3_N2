START TRANSACTION;

ALTER TABLE `work_schedules` ADD `effective_end_date` date NULL;

ALTER TABLE `work_schedules` ADD `effective_start_date` date NULL;

ALTER TABLE `work_schedules` ADD `recurrence_type` varchar(20) CHARACTER SET utf8mb4 NULL;

ALTER TABLE `work_schedules` ADD `recurrence_weeks` int NULL;

ALTER TABLE `work_schedules` ADD CONSTRAINT `ck_work_schedules_recurrence` CHECK ((`effective_start_date` IS NULL AND `recurrence_type` IS NULL AND `recurrence_weeks` IS NULL AND `effective_end_date` IS NULL) OR (`effective_start_date` IS NOT NULL AND `recurrence_type` IS NOT NULL AND ((`recurrence_type` = 'NONE' AND `recurrence_weeks` IS NOT NULL AND `recurrence_weeks` = 1 AND `effective_end_date` IS NOT NULL AND `effective_end_date` >= `effective_start_date`) OR (`recurrence_type` = 'WEEKLY' AND ((`recurrence_weeks` IS NULL AND `effective_end_date` IS NULL) OR (`recurrence_weeks` IS NOT NULL AND `recurrence_weeks` BETWEEN 1 AND 52 AND `effective_end_date` IS NOT NULL AND `effective_end_date` >= `effective_start_date`))))));

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261013100022_AddWorkScheduleRecurrence', '8.0.2');

COMMIT;

