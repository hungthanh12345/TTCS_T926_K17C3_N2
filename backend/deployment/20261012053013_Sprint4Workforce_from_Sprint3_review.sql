START TRANSACTION;

CREATE TABLE `allowances` (
    `id` int NOT NULL AUTO_INCREMENT,
    `student_id` int NOT NULL,
    `allowance_type` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `description` varchar(1000) CHARACTER SET utf8mb4 NULL,
    `amount` decimal(12,2) NOT NULL,
    `paid_amount` decimal(12,2) NOT NULL DEFAULT 0.0,
    `period_start` date NOT NULL,
    `period_end` date NOT NULL,
    `status` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    CONSTRAINT `PK_allowances` PRIMARY KEY (`id`),
    CONSTRAINT `ck_allowances_amount` CHECK (`amount` > 0 AND `paid_amount` >= 0 AND `paid_amount` <= `amount`),
    CONSTRAINT `ck_allowances_period` CHECK (`period_end` >= `period_start`),
    CONSTRAINT `ck_allowances_status` CHECK (`status` IN ('ACTIVE', 'CANCELLED')),
    CONSTRAINT `FK_allowances_students_student_id` FOREIGN KEY (`student_id`) REFERENCES `students` (`id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `attendances` (
    `id` int NOT NULL AUTO_INCREMENT,
    `student_id` int NOT NULL,
    `date` date NOT NULL,
    `check_in_time` datetime(6) NOT NULL,
    `check_out_time` datetime(6) NULL,
    `duration_minutes` int NULL,
    `status` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `notes` varchar(500) CHARACTER SET utf8mb4 NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    CONSTRAINT `PK_attendances` PRIMARY KEY (`id`),
    CONSTRAINT `ck_attendances_checkout_order` CHECK (`check_out_time` IS NULL OR `check_out_time` >= `check_in_time`),
    CONSTRAINT `ck_attendances_duration_nonnegative` CHECK (`duration_minutes` IS NULL OR `duration_minutes` >= 0),
    CONSTRAINT `ck_attendances_status` CHECK (`status` IN ('CHECKED_IN', 'COMPLETED')),
    CONSTRAINT `FK_attendances_students_student_id` FOREIGN KEY (`student_id`) REFERENCES `students` (`id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `leave_requests` (
    `id` int NOT NULL AUTO_INCREMENT,
    `student_id` int NOT NULL,
    `start_date` date NOT NULL,
    `end_date` date NOT NULL,
    `reason` varchar(2000) CHARACTER SET utf8mb4 NOT NULL,
    `status` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `reviewed_by_user_id` int NULL,
    `reviewed_at` datetime(6) NULL,
    `rejection_reason` varchar(1000) CHARACTER SET utf8mb4 NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    CONSTRAINT `PK_leave_requests` PRIMARY KEY (`id`),
    CONSTRAINT `ck_leave_requests_date_order` CHECK (`end_date` >= `start_date`),
    CONSTRAINT `ck_leave_requests_status` CHECK (`status` IN ('PENDING', 'APPROVED', 'REJECTED')),
    CONSTRAINT `FK_leave_requests_students_student_id` FOREIGN KEY (`student_id`) REFERENCES `students` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_leave_requests_users_reviewed_by_user_id` FOREIGN KEY (`reviewed_by_user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) CHARACTER SET=utf8mb4;

CREATE TABLE `student_groups` (
    `id` int NOT NULL AUTO_INCREMENT,
    `name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `description` varchar(500) CHARACTER SET utf8mb4 NULL,
    `status` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    CONSTRAINT `PK_student_groups` PRIMARY KEY (`id`),
    CONSTRAINT `ck_student_groups_status` CHECK (`status` IN ('ACTIVE', 'INACTIVE'))
) CHARACTER SET=utf8mb4;

CREATE TABLE `support_requests` (
    `id` int NOT NULL AUTO_INCREMENT,
    `student_id` int NOT NULL,
    `subject` varchar(160) CHARACTER SET utf8mb4 NOT NULL,
    `description` varchar(5000) CHARACTER SET utf8mb4 NOT NULL,
    `category` varchar(80) CHARACTER SET utf8mb4 NOT NULL,
    `status` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `response` varchar(4000) CHARACTER SET utf8mb4 NULL,
    `handled_by_user_id` int NULL,
    `handled_at` datetime(6) NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    CONSTRAINT `PK_support_requests` PRIMARY KEY (`id`),
    CONSTRAINT `ck_support_requests_status` CHECK (`status` IN ('PENDING', 'IN_PROGRESS', 'RESOLVED')),
    CONSTRAINT `FK_support_requests_students_student_id` FOREIGN KEY (`student_id`) REFERENCES `students` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_support_requests_users_handled_by_user_id` FOREIGN KEY (`handled_by_user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) CHARACTER SET=utf8mb4;

CREATE TABLE `allowance_payments` (
    `id` int NOT NULL AUTO_INCREMENT,
    `allowance_id` int NOT NULL,
    `amount` decimal(12,2) NOT NULL,
    `paid_at` datetime(6) NOT NULL,
    `recorded_by_user_id` int NOT NULL,
    `reference` varchar(150) CHARACTER SET utf8mb4 NULL,
    `created_at` datetime(6) NOT NULL,
    CONSTRAINT `PK_allowance_payments` PRIMARY KEY (`id`),
    CONSTRAINT `ck_allowance_payments_amount` CHECK (`amount` > 0),
    CONSTRAINT `FK_allowance_payments_allowances_allowance_id` FOREIGN KEY (`allowance_id`) REFERENCES `allowances` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_allowance_payments_users_recorded_by_user_id` FOREIGN KEY (`recorded_by_user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `student_group_memberships` (
    `student_id` int NOT NULL,
    `student_group_id` int NOT NULL,
    `joined_at` datetime(6) NOT NULL,
    CONSTRAINT `PK_student_group_memberships` PRIMARY KEY (`student_group_id`, `student_id`),
    CONSTRAINT `FK_student_group_memberships_student_groups_student_group_id` FOREIGN KEY (`student_group_id`) REFERENCES `student_groups` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_student_group_memberships_students_student_id` FOREIGN KEY (`student_id`) REFERENCES `students` (`id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `work_schedules` (
    `id` int NOT NULL AUTO_INCREMENT,
    `name` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
    `target_type` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `student_group_id` int NULL,
    `student_id` int NULL,
    `is_flexible` tinyint(1) NOT NULL,
    `status` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    CONSTRAINT `PK_work_schedules` PRIMARY KEY (`id`),
    CONSTRAINT `ck_work_schedules_status` CHECK (`status` IN ('ACTIVE', 'INACTIVE')),
    CONSTRAINT `ck_work_schedules_target` CHECK ((`target_type` = 'STUDENT' AND `student_id` IS NOT NULL AND `student_group_id` IS NULL) OR (`target_type` = 'GROUP' AND `student_id` IS NULL AND `student_group_id` IS NOT NULL)),
    CONSTRAINT `FK_work_schedules_student_groups_student_group_id` FOREIGN KEY (`student_group_id`) REFERENCES `student_groups` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_work_schedules_students_student_id` FOREIGN KEY (`student_id`) REFERENCES `students` (`id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `work_schedule_days` (
    `id` int NOT NULL AUTO_INCREMENT,
    `work_schedule_id` int NOT NULL,
    `day_of_week` int NOT NULL,
    `start_time` time(6) NOT NULL,
    `end_time` time(6) NOT NULL,
    `break_start` time(6) NULL,
    `break_end` time(6) NULL,
    CONSTRAINT `PK_work_schedule_days` PRIMARY KEY (`id`),
    CONSTRAINT `ck_work_schedule_days_break_pair` CHECK ((`break_start` IS NULL AND `break_end` IS NULL) OR (`break_start` IS NOT NULL AND `break_end` IS NOT NULL AND `break_start` < `break_end` AND `break_start` >= `start_time` AND `break_end` <= `end_time`)),
    CONSTRAINT `ck_work_schedule_days_time_order` CHECK (`start_time` < `end_time`),
    CONSTRAINT `ck_work_schedule_days_weekday` CHECK (`day_of_week` BETWEEN 0 AND 6),
    CONSTRAINT `FK_work_schedule_days_work_schedules_work_schedule_id` FOREIGN KEY (`work_schedule_id`) REFERENCES `work_schedules` (`id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_allowance_payments_allowance_id_paid_at` ON `allowance_payments` (`allowance_id`, `paid_at`);

CREATE INDEX `IX_allowance_payments_recorded_by_user_id` ON `allowance_payments` (`recorded_by_user_id`);

CREATE UNIQUE INDEX `IX_allowances_student_id_allowance_type_period_start_period_end` ON `allowances` (`student_id`, `allowance_type`, `period_start`, `period_end`);

CREATE INDEX `IX_allowances_student_id_period_start_period_end` ON `allowances` (`student_id`, `period_start`, `period_end`);

CREATE INDEX `IX_attendances_date` ON `attendances` (`date`);

CREATE UNIQUE INDEX `IX_attendances_student_id_date` ON `attendances` (`student_id`, `date`);

CREATE INDEX `IX_leave_requests_reviewed_by_user_id` ON `leave_requests` (`reviewed_by_user_id`);

CREATE INDEX `IX_leave_requests_student_id_status_start_date_end_date` ON `leave_requests` (`student_id`, `status`, `start_date`, `end_date`);

CREATE INDEX `IX_student_group_memberships_student_group_id` ON `student_group_memberships` (`student_group_id`);

CREATE INDEX `IX_student_group_memberships_student_id` ON `student_group_memberships` (`student_id`);

CREATE UNIQUE INDEX `IX_student_groups_name` ON `student_groups` (`name`);

CREATE INDEX `IX_student_groups_status` ON `student_groups` (`status`);

CREATE INDEX `IX_support_requests_handled_by_user_id` ON `support_requests` (`handled_by_user_id`);

CREATE INDEX `IX_support_requests_status_created_at` ON `support_requests` (`status`, `created_at`);

CREATE INDEX `IX_support_requests_student_id_created_at` ON `support_requests` (`student_id`, `created_at`);

CREATE UNIQUE INDEX `IX_work_schedule_days_work_schedule_id_day_of_week` ON `work_schedule_days` (`work_schedule_id`, `day_of_week`);

CREATE INDEX `IX_work_schedules_student_group_id` ON `work_schedules` (`student_group_id`);

CREATE INDEX `IX_work_schedules_student_id` ON `work_schedules` (`student_id`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261012053013_Sprint4Workforce', '8.0.2');

COMMIT;
