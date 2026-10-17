-- REVIEW ONLY: DO NOT EXECUTE.
-- Target if separately approved: internship_restore_check on verified Test MySQL only.
-- This artifact intentionally does not bypass SOURCE_MISSING.
-- Source migration 20261008065402_AddRegistrationReceivedEmailTemplate remains unreconciled.
-- Read-only preflight statements below report current evidence; candidate DDL is block-commented so this file cannot apply it.
-- A separate approved deployment step is required after the missing-source blocker is resolved.

SELECT DATABASE() AS selected_schema, VERSION() AS mysql_version, CURRENT_USER() AS database_user;

SELECT TABLE_NAME, ENGINE, TABLE_COLLATION
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME IN ('students', 'student_program_audit_events', 'internship_programs', 'users', 'work_schedules', 'internship_tasks');

SELECT COLUMN_NAME, COLUMN_TYPE, IS_NULLABLE, COLUMN_DEFAULT, COLLATION_NAME
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'students'
  AND COLUMN_NAME = 'application_status';

SELECT COUNT(*) AS recurrence_columns_found
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'work_schedules'
  AND COLUMN_NAME IN ('effective_start_date', 'effective_end_date', 'recurrence_type', 'recurrence_weeks');

SELECT CONSTRAINT_NAME, DELETE_RULE, UPDATE_RULE
FROM information_schema.REFERENTIAL_CONSTRAINTS
WHERE CONSTRAINT_SCHEMA = DATABASE()
  AND CONSTRAINT_NAME = 'fk_sprint2_internship_tasks_mentor';

SELECT MigrationId
FROM `__EFMigrationsHistory`
WHERE MigrationId IN (
  '20261008065402_AddRegistrationReceivedEmailTemplate',
  '20261013100022_AddWorkScheduleRecurrence',
  '20261016040114_AddProgramParticipationAuditAndPendingApplicationDefault'
);

-- Expected on Owner-reported Test snapshot: audit table missing, application_status default APPROVED,
-- recurrence columns present, Mentor-Task RESTRICT. The missing email source is a blocker, not waived here.
-- BEGIN COMMENTED EF-generated US33 DDL candidate from IMigrator.
/*
START TRANSACTION;

ALTER TABLE `students` MODIFY COLUMN `application_status` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'PENDING_APPROVAL';

CREATE TABLE `student_program_audit_events` (
    `id` int NOT NULL AUTO_INCREMENT,
    `student_id` int NOT NULL,
    `program_id` int NOT NULL,
    `actor_user_id` int NOT NULL,
    `event_type` varchar(40) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    `previous_status` varchar(16) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
    `new_status` varchar(16) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
    `reason` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
    `occurred_at_utc` datetime(6) NOT NULL,
    CONSTRAINT `PK_student_program_audit_events` PRIMARY KEY (`id`),
    CONSTRAINT `ck_student_program_audit_events_status` CHECK ((`event_type` = 'PARTICIPATION_STATUS_CHANGED' AND `new_status` IS NOT NULL AND `new_status` IN ('UNVERIFIED', 'ACTIVE', 'ENDED', 'CANCELLED') AND (`previous_status` IS NULL OR `previous_status` IN ('UNVERIFIED', 'ACTIVE', 'ENDED', 'CANCELLED'))) OR (`event_type` IN ('COMPLETION_CONFIRMED', 'COMPLETION_REVOKED') AND `previous_status` IS NULL AND `new_status` IS NULL)),
    CONSTRAINT `ck_student_program_audit_events_type` CHECK (`event_type` IN ('PARTICIPATION_STATUS_CHANGED', 'COMPLETION_CONFIRMED', 'COMPLETION_REVOKED')),
    CONSTRAINT `FK_student_program_audit_events_internship_programs_program_id` FOREIGN KEY (`program_id`) REFERENCES `internship_programs` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_student_program_audit_events_students_student_id` FOREIGN KEY (`student_id`) REFERENCES `students` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_student_program_audit_events_users_actor_user_id` FOREIGN KEY (`actor_user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE INDEX `IX_student_program_audit_events_actor_user_id` ON `student_program_audit_events` (`actor_user_id`);

CREATE INDEX `ix_student_program_audit_events_program_id` ON `student_program_audit_events` (`program_id`, `id`);

CREATE INDEX `ix_student_program_audit_events_student_program_id` ON `student_program_audit_events` (`student_id`, `program_id`, `id`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261016040114_AddProgramParticipationAuditAndPendingApplicationDefault', '8.0.2');

COMMIT;
*/
-- END COMMENTED CANDIDATE.
