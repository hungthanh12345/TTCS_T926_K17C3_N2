-- TEST-ONLY target-specific copy; review source v2 SHA-256: 3E3ECA047A0A723893B50487BAA33B9A7098F2C32C8900EC80E14010547DB71F
-- Fixed target: internship_sprint5_v2_verify_20261017. Fresh empty schema only; not an upgrade script.
-- TTCS database install bundle v2 (2026-10-17)
-- v2 aligns student_documents with the current EF model. v1 remains unchanged as the Owner-installed artifact.
-- Source snapshot: develop commit 058c3b8e1cb47ed3a49a138b1ac831d53f9d7bf5.
-- Remote fetch was unavailable during preparation; verify develop SHA before using this artifact.
-- Installs into the pre-created internship_sprint5_v2_verify_20261017 schema only; this is NOT an upgrade script.
-- REQUIRED READ-ONLY PREFLIGHT (run separately; do not execute this file before reviewing the results):
--   SELECT DATABASE() AS selected_schema, CURRENT_USER() AS authenticated_account;
--   SHOW GRANTS;
--   SELECT SCHEMA_NAME FROM information_schema.SCHEMATA
--     WHERE SCHEMA_NAME = 'internship_sprint5_v2_verify_20261017';
--   SELECT COUNT(*) AS existing_objects FROM information_schema.TABLES
--     WHERE TABLE_SCHEMA = 'internship_sprint5_v2_verify_20261017';
-- Continue only if the target schema exists, it has zero tables/views, the account is ttcs_s5_setup,
-- and grants include SELECT, INSERT, CREATE, ALTER, INDEX, REFERENCES on internship_sprint5_v2_verify_20261017 only;
-- Global USAGE is acceptable; any other global/schema grant is not. If a grant is missing or broader, STOP.
-- This SQL file cannot inspect SHOW GRANTS or enforce privilege scope itself. Use a fail-fast SQL client (never use --force).
-- MySQL DDL auto-commits; on any error stop and inspect; do not rerun blindly.
-- Every DDL/DML table reference below is explicitly qualified with internship_sprint5_v2_verify_20261017.
-- No user accounts or user passwords are seeded. Only standard role rows and the two current EF email templates are seeded.
-- Raw bootstrap DDL below is assembled from reviewed source SQL, excluding demo-password update and destructive schema.sql.
-- EF history entries below are emitted by IMigrator for source migrations whose operations are included in this file.
-- Missing source migration 20261008065402_AddRegistrationReceivedEmailTemplate is intentionally not synthesized or recorded.
-- application_status is created with the known old physical default (APPROVED) as a prerequisite; the source US33 migration
-- then changes only the default to PENDING_APPROVAL. No existing student rows exist in this new database.
-- Do not start the API unless the entire script completes successfully.

USE `internship_sprint5_v2_verify_20261017`;

-- BEGIN source: backend\migrations\000_initial_core_schema.sql (no account/data dump)
-- Safe initial schema for a new or empty MySQL database.
-- Run this before the dated Sprint 2 migrations in this folder.
-- This additive baseline does not drop tables, records, or databases.
-- If a table already exists with a different shape, review and apply a migration instead.

CREATE TABLE IF NOT EXISTS `internship_sprint5_v2_verify_20261017`.`roles` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `name` VARCHAR(50) NOT NULL,
    `description` VARCHAR(255) NULL,
    `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_roles_name` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT IGNORE INTO `internship_sprint5_v2_verify_20261017`.`roles` (`id`, `name`, `description`) VALUES
    (1, 'ROLE_ADMIN', 'Quản trị viên hệ thống'),
    (2, 'ROLE_HR', 'Nhân sự quản lý thực tập sinh'),
    (3, 'ROLE_MENTOR', 'Mentor hướng dẫn thực tập'),
    (4, 'ROLE_STUDENT', 'Sinh viên thực tập');

CREATE TABLE IF NOT EXISTS `internship_sprint5_v2_verify_20261017`.`users` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `email` VARCHAR(150) NOT NULL,
    `password_hash` VARCHAR(255) NOT NULL,
    `role_id` INT NOT NULL,
    `status` ENUM('ACTIVE', 'INACTIVE', 'LOCKED') NOT NULL DEFAULT 'ACTIVE',
    `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_users_email` (`email`),
    KEY `idx_users_role_id` (`role_id`),
    CONSTRAINT `fk_users_role` FOREIGN KEY (`role_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`roles` (`id`)
        ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `internship_sprint5_v2_verify_20261017`.`mentors` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `user_id` INT NOT NULL,
    `full_name` VARCHAR(100) NOT NULL,
    `phone_number` VARCHAR(20) NULL,
    `department` VARCHAR(100) NOT NULL,
    `specialization` VARCHAR(150) NULL,
    `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_mentors_user_id` (`user_id`),
    CONSTRAINT `fk_mentors_user` FOREIGN KEY (`user_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`users` (`id`)
        ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `internship_sprint5_v2_verify_20261017`.`students` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `user_id` INT NULL,
    `student_code` VARCHAR(50) NOT NULL,
    `full_name` VARCHAR(100) NOT NULL,
    `phone_number` VARCHAR(20) NULL,
    `university` VARCHAR(150) NOT NULL,
    `major` VARCHAR(100) NOT NULL,
    `mentor_id` INT NULL,
    `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_students_student_code` (`student_code`),
    UNIQUE KEY `uk_students_user_id` (`user_id`),
    KEY `idx_students_university` (`university`),
    KEY `idx_students_major` (`major`),
    KEY `idx_students_university_major` (`university`, `major`),
    KEY `idx_students_mentor_id` (`mentor_id`),
    CONSTRAINT `fk_students_user` FOREIGN KEY (`user_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`users` (`id`)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT `fk_students_mentor` FOREIGN KEY (`mentor_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`mentors` (`id`)
        ON UPDATE CASCADE ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
-- END source: backend\migrations\000_initial_core_schema.sql
-- BEGIN source: backend\migrations\20261002_part01_internship_programs.sql (no account/data dump)
-- Part 1 (US11): create departments and internship programs for this install target.
-- Both tables are explicitly scoped to internship_sprint5_v2_verify_20261017.
CREATE TABLE IF NOT EXISTS `internship_sprint5_v2_verify_20261017`.`departments` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `name` VARCHAR(100) NOT NULL,
    `description` VARCHAR(500) NULL,
    `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_departments_name` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `internship_sprint5_v2_verify_20261017`.`internship_programs` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `name` VARCHAR(150) NOT NULL,
    `description` VARCHAR(1000) NULL,
    `department_id` INT NOT NULL,
    `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `idx_programs_department_id` (`department_id`),
    UNIQUE KEY `uk_programs_department_name` (`department_id`, `name`),
    CONSTRAINT `fk_programs_department` FOREIGN KEY (`department_id`)
        REFERENCES `internship_sprint5_v2_verify_20261017`.`departments` (`id`) ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
-- END source: backend\migrations\20261002_part01_internship_programs.sql
-- BEGIN source: backend\migrations\20261002_part02_program_dates.sql (no account/data dump)
-- Part 2 (US13) depends on Part 1's internship_programs table.
-- Run with the Part 1 database selected; this migration adds only US13 date columns.
SET @part2_add_start_date = IF(
    EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = 'internship_sprint5_v2_verify_20261017'
          AND TABLE_NAME = 'internship_programs'
          AND COLUMN_NAME = 'start_date'
    ),
    'SELECT 1',
    'ALTER TABLE `internship_sprint5_v2_verify_20261017`.`internship_programs` ADD COLUMN `start_date` DATE NULL'
);
PREPARE part2_start_date_stmt FROM @part2_add_start_date;
EXECUTE part2_start_date_stmt;
DEALLOCATE PREPARE part2_start_date_stmt;

SET @part2_add_end_date = IF(
    EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = 'internship_sprint5_v2_verify_20261017'
          AND TABLE_NAME = 'internship_programs'
          AND COLUMN_NAME = 'end_date'
    ),
    'SELECT 1',
    'ALTER TABLE `internship_sprint5_v2_verify_20261017`.`internship_programs` ADD COLUMN `end_date` DATE NULL'
);
PREPARE part2_end_date_stmt FROM @part2_add_end_date;
EXECUTE part2_end_date_stmt;
DEALLOCATE PREPARE part2_end_date_stmt;
-- END source: backend\migrations\20261002_part02_program_dates.sql
-- BEGIN source: backend\migrations\20261003_part03_student_documents.sql (no account/data dump)
-- Part 3 (US04): private student documents stored with their owning student profile.
-- This migration also upgrades the legacy US04 table without discarding stored metadata.
CREATE TABLE IF NOT EXISTS `internship_sprint5_v2_verify_20261017`.`student_documents` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `student_id` INT NOT NULL,
    `document_type` VARCHAR(30) NOT NULL,
    `original_file_name` VARCHAR(255) NOT NULL,
    `stored_file_name` VARCHAR(100) NOT NULL,
    `content_type` VARCHAR(100) NOT NULL,
    `size_bytes` BIGINT NOT NULL,
    `file_content` LONGBLOB NULL,
    `uploaded_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `idx_student_documents_student_uploaded` (`student_id`, `uploaded_at`),
    CONSTRAINT `fk_student_documents_student`
        FOREIGN KEY (`student_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`students` (`id`)
        ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

SET @add_student_document_file_content = IF(
    EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = 'internship_sprint5_v2_verify_20261017'
          AND TABLE_NAME = 'student_documents'
          AND COLUMN_NAME = 'file_content'
    ),
    'SELECT 1',
    'ALTER TABLE `internship_sprint5_v2_verify_20261017`.`student_documents` ADD COLUMN `file_content` LONGBLOB NULL'
);
PREPARE add_student_document_file_content_stmt FROM @add_student_document_file_content;
EXECUTE add_student_document_file_content_stmt;
DEALLOCATE PREPARE add_student_document_file_content_stmt;

SET @add_student_documents_owner_index = IF(
    EXISTS (
        SELECT 1 FROM information_schema.STATISTICS
        WHERE TABLE_SCHEMA = 'internship_sprint5_v2_verify_20261017'
          AND TABLE_NAME = 'student_documents'
          AND INDEX_NAME = 'idx_student_documents_student_uploaded'
    ),
    'SELECT 1',
    'CREATE INDEX `idx_student_documents_student_uploaded` ON `internship_sprint5_v2_verify_20261017`.`student_documents` (`student_id`, `uploaded_at`)'
);
PREPARE add_student_documents_owner_index_stmt FROM @add_student_documents_owner_index;
EXECUTE add_student_documents_owner_index_stmt;
DEALLOCATE PREPARE add_student_documents_owner_index_stmt;

SET @add_student_documents_student_fk = IF(
    EXISTS (
        SELECT 1 FROM information_schema.KEY_COLUMN_USAGE
        WHERE CONSTRAINT_SCHEMA = 'internship_sprint5_v2_verify_20261017'
          AND TABLE_NAME = 'student_documents'
          AND COLUMN_NAME = 'student_id'
          AND REFERENCED_TABLE_NAME = 'students'
          AND REFERENCED_COLUMN_NAME = 'id'
    ),
    'SELECT 1',
    'ALTER TABLE `internship_sprint5_v2_verify_20261017`.`student_documents` ADD CONSTRAINT `fk_student_documents_student` FOREIGN KEY (`student_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`students` (`id`) ON UPDATE CASCADE ON DELETE CASCADE'
);
PREPARE add_student_documents_student_fk_stmt FROM @add_student_documents_student_fk;
EXECUTE add_student_documents_student_fk_stmt;
DEALLOCATE PREPARE add_student_documents_student_fk_stmt;
-- END source: backend\migrations\20261003_part03_student_documents.sql
-- BEGIN source: backend\migrations\20261003_part04_student_registration_approval.sql (no account/data dump)
-- Part 4 (US06): add registration states to the existing users.status column.
-- Existing ACTIVE / INACTIVE / LOCKED values and the ACTIVE default are preserved.
ALTER TABLE `internship_sprint5_v2_verify_20261017`.`users`
    MODIFY COLUMN `status`
        ENUM('ACTIVE', 'INACTIVE', 'LOCKED', 'PENDING_APPROVAL', 'REJECTED')
        NOT NULL DEFAULT 'ACTIVE';
-- END source: backend\migrations\20261003_part04_student_registration_approval.sql
-- BEGIN source: backend\migrations\20261003_part05_mentor_task_assignment.sql (no account/data dump)
-- Part 5 (US15): mentor-created tasks linked to their assigned mentor and student.
-- Reuses the existing internship_tasks table when the deployment already has it.
-- Only the initial TO_DO state is included; progress states belong to Part 6.
CREATE TABLE IF NOT EXISTS `internship_sprint5_v2_verify_20261017`.`internship_tasks` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `mentor_id` INT NOT NULL,
    `student_id` INT NOT NULL,
    `title` VARCHAR(200) NOT NULL,
    `description` VARCHAR(2000) NULL,
    `due_date` DATE NULL,
    `status` VARCHAR(20) NOT NULL DEFAULT 'TO_DO',
    `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `idx_sprint2_internship_tasks_mentor_student` (`mentor_id`, `student_id`),
    KEY `idx_sprint2_internship_tasks_due_date` (`due_date`),
    KEY `IX_internship_tasks_student_id` (`student_id`),
    CONSTRAINT `fk_sprint2_internship_tasks_mentor` FOREIGN KEY (`mentor_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`mentors` (`id`)
        ON UPDATE CASCADE ON DELETE CASCADE,
    CONSTRAINT `fk_sprint2_internship_tasks_student` FOREIGN KEY (`student_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`students` (`id`)
        ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
-- END source: backend\migrations\20261003_part05_mentor_task_assignment.sql
-- BEGIN source: backend\migrations\20261004_part08_weekly_reports_and_mentor_feedback.sql (no account/data dump)
-- Part 8 (US17/US18): student weekly reports and one mentor review per report.
-- Additive migration. Apply after the earlier Sprint 2 migrations.
CREATE TABLE IF NOT EXISTS `internship_sprint5_v2_verify_20261017`.`weekly_reports` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `student_id` INT NOT NULL,
    `week_start_date` DATE NOT NULL,
    `work_summary` TEXT NOT NULL,
    `results` TEXT NULL,
    `challenges` TEXT NULL,
    `next_week_plan` TEXT NULL,
    `attachment_url` VARCHAR(2048) NULL,
    `status` VARCHAR(20) NOT NULL DEFAULT 'SUBMITTED',
    `created_at` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `updated_at` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_weekly_reports_student_week` (`student_id`, `week_start_date`),
    CONSTRAINT `fk_weekly_reports_student`
        FOREIGN KEY (`student_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`students` (`id`)
        ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `internship_sprint5_v2_verify_20261017`.`mentor_feedback` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `weekly_report_id` INT NOT NULL,
    `mentor_id` INT NOT NULL,
    `content` TEXT NOT NULL,
    `created_at` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `updated_at` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_mentor_feedback_weekly_report` (`weekly_report_id`),
    KEY `idx_mentor_feedback_mentor` (`mentor_id`),
    CONSTRAINT `fk_mentor_feedback_weekly_report`
        FOREIGN KEY (`weekly_report_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`weekly_reports` (`id`)
        ON UPDATE CASCADE ON DELETE CASCADE,
    CONSTRAINT `fk_mentor_feedback_mentor`
        FOREIGN KEY (`mentor_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`mentors` (`id`)
        ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
-- END source: backend\migrations\20261004_part08_weekly_reports_and_mentor_feedback.sql
-- BEGIN source: backend\migrations\20261004_part09_internship_evaluations.sql (no account/data dump)
-- Part 09 (US19/US20): final mentor evaluation for each current student profile.
-- Students are not associated with internship_programs in the current schema;
-- this migration deliberately preserves that existing domain boundary.
CREATE TABLE IF NOT EXISTS `internship_sprint5_v2_verify_20261017`.`internship_evaluations` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `student_id` INT NOT NULL,
    `mentor_id` INT NULL,
    `mentor_name_snapshot` VARCHAR(100) NOT NULL,
    `skills_score` INT NOT NULL,
    `attitude_score` INT NOT NULL,
    `comments` TEXT NOT NULL,
    `evaluated_at` DATETIME(6) NOT NULL,
    `created_at` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `updated_at` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_internship_evaluations_student` (`student_id`),
    KEY `idx_internship_evaluations_mentor` (`mentor_id`),
    CONSTRAINT `ck_internship_evaluations_scores`
        CHECK (`skills_score` BETWEEN 1 AND 10 AND `attitude_score` BETWEEN 1 AND 10),
    CONSTRAINT `fk_internship_evaluations_student`
        FOREIGN KEY (`student_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`students` (`id`)
        ON UPDATE CASCADE ON DELETE CASCADE,
    CONSTRAINT `fk_internship_evaluations_mentor`
        FOREIGN KEY (`mentor_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`mentors` (`id`)
        ON UPDATE CASCADE ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
-- END source: backend\migrations\20261004_part09_internship_evaluations.sql
-- BEGIN source: backend\migrations\20261004_part11_student_program_link.sql (no account/data dump)
-- Part 11: associate existing student profiles and applications with an internship program.
-- Additive and idempotent: existing rows remain unassigned (program_id = NULL).
SET @part11_add_program_id = IF(
    EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = 'internship_sprint5_v2_verify_20261017'
          AND TABLE_NAME = 'students'
          AND COLUMN_NAME = 'program_id'
    ),
    'SELECT 1',
    'ALTER TABLE `internship_sprint5_v2_verify_20261017`.`students` ADD COLUMN `program_id` INT NULL'
);
PREPARE part11_program_id_stmt FROM @part11_add_program_id;
EXECUTE part11_program_id_stmt;
DEALLOCATE PREPARE part11_program_id_stmt;

SET @part11_add_program_index = IF(
    EXISTS (
        SELECT 1 FROM information_schema.STATISTICS
        WHERE TABLE_SCHEMA = 'internship_sprint5_v2_verify_20261017'
          AND TABLE_NAME = 'students'
          AND INDEX_NAME = 'idx_students_program_id'
    ),
    'SELECT 1',
    'CREATE INDEX `idx_students_program_id` ON `internship_sprint5_v2_verify_20261017`.`students` (`program_id`)'
);
PREPARE part11_program_index_stmt FROM @part11_add_program_index;
EXECUTE part11_program_index_stmt;
DEALLOCATE PREPARE part11_program_index_stmt;

SET @part11_add_program_fk = IF(
    EXISTS (
        SELECT 1 FROM information_schema.KEY_COLUMN_USAGE
        WHERE CONSTRAINT_SCHEMA = 'internship_sprint5_v2_verify_20261017'
          AND TABLE_NAME = 'students'
          AND COLUMN_NAME = 'program_id'
          AND REFERENCED_TABLE_NAME = 'internship_programs'
          AND REFERENCED_COLUMN_NAME = 'id'
    ),
    'SELECT 1',
    'ALTER TABLE `internship_sprint5_v2_verify_20261017`.`students` ADD CONSTRAINT `fk_students_program` FOREIGN KEY (`program_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`internship_programs` (`id`) ON UPDATE CASCADE ON DELETE SET NULL'
);
PREPARE part11_program_fk_stmt FROM @part11_add_program_fk;
EXECUTE part11_program_fk_stmt;
DEALLOCATE PREPARE part11_program_fk_stmt;
-- END source: backend\migrations\20261004_part11_student_program_link.sql
-- BEGIN source: backend\migrations\20261005_part12_persistent_notifications.sql (no account/data dump)
-- Safe additive migration: preserves existing records and adds a per-user notification inbox.
CREATE TABLE IF NOT EXISTS `internship_sprint5_v2_verify_20261017`.`notifications` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `user_id` INT NOT NULL,
    `source_key` VARCHAR(191) NOT NULL,
    `title` VARCHAR(160) NOT NULL,
    `message` VARCHAR(1000) NOT NULL,
    `route` VARCHAR(255) NOT NULL,
    `created_at` DATETIME(6) NOT NULL,
    `read_at` DATETIME(6) NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_notifications_user_source` (`user_id`, `source_key`),
    KEY `idx_notifications_user_created` (`user_id`, `created_at`),
    CONSTRAINT `fk_notifications_user` FOREIGN KEY (`user_id`)
        REFERENCES `internship_sprint5_v2_verify_20261017`.`users` (`id`) ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
-- END source: backend\migrations\20261005_part12_persistent_notifications.sql
-- BEGIN US09 baseline table from source SQL, aligned to current EF Restrict mapping (source SQL file remains unchanged)
-- US09: HR tải lên / cập nhật hợp đồng thực tập (bản số hóa) và liên kết với từng sinh viên.
-- Bảng cũng chứa trạng thái và thông tin xác nhận để US10 (sinh viên xác nhận hợp đồng) dùng lại.
-- Mỗi sinh viên có tối đa một hợp đồng; trạng thái: PENDING_CONFIRMATION -> CONFIRMED (hoặc CANCELLED).
CREATE TABLE IF NOT EXISTS `internship_sprint5_v2_verify_20261017`.`internship_contracts` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `student_id` INT NOT NULL,
    `contract_number` VARCHAR(50) NOT NULL,
    `file_name` VARCHAR(255) NULL,
    `content_type` VARCHAR(150) NULL,
    `size_bytes` BIGINT NOT NULL DEFAULT 0,
    `file_content` LONGBLOB NULL,
    `content_hash` CHAR(64) NULL,
    `uploaded_by_user_id` INT NULL,
    `file_uploaded_at` DATETIME(6) NULL,
    `status` VARCHAR(30) NOT NULL DEFAULT 'PENDING_CONFIRMATION',
    `issued_at` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `confirmed_at` DATETIME(6) NULL,
    `confirmed_by_user_id` INT NULL,
    `created_at` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `updated_at` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_internship_contracts_student` (`student_id`),
    UNIQUE KEY `uq_internship_contracts_number` (`contract_number`),
    KEY `idx_internship_contracts_status` (`status`),
    KEY `IX_internship_contracts_confirmed_by_user_id` (`confirmed_by_user_id`),
    KEY `IX_internship_contracts_uploaded_by_user_id` (`uploaded_by_user_id`),
    CONSTRAINT `chk_internship_contracts_status`
        CHECK (`status` IN ('PENDING_CONFIRMATION', 'CONFIRMED', 'CANCELLED')),
    CONSTRAINT `fk_internship_contracts_student`
        FOREIGN KEY (`student_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`students` (`id`)
            ON UPDATE CASCADE ON DELETE RESTRICT,
    CONSTRAINT `fk_internship_contracts_uploaded_by`
        FOREIGN KEY (`uploaded_by_user_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`users` (`id`)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT `fk_internship_contracts_confirmed_by`
        FOREIGN KEY (`confirmed_by_user_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`users` (`id`)
        ON UPDATE CASCADE ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
-- END US09 baseline table
-- Current source US33 migration assumes this pre-existing column and changes only its default.
-- The old default is based on Owner-verified Test metadata and migration oldDefaultValue; historical creator remains unknown.
ALTER TABLE `internship_sprint5_v2_verify_20261017`.`students`
    ADD COLUMN `application_status` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
    NOT NULL DEFAULT 'APPROVED';

-- BEGIN EF Core IMigrator script: source migrations only; each history row follows its included operations.
CREATE TABLE IF NOT EXISTS `internship_sprint5_v2_verify_20261017`.`__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

START TRANSACTION;

ALTER TABLE `internship_sprint5_v2_verify_20261017`.`students` ADD `rejection_reason` text CHARACTER SET utf8mb4 NULL;

ALTER TABLE `internship_sprint5_v2_verify_20261017`.`students` ADD `reviewed_at` datetime(6) NULL;

ALTER TABLE `internship_sprint5_v2_verify_20261017`.`students` ADD `reviewed_by_user_id` int NULL;

ALTER TABLE `internship_sprint5_v2_verify_20261017`.`student_documents` ADD `rejection_reason` text CHARACTER SET utf8mb4 NULL;

ALTER TABLE `internship_sprint5_v2_verify_20261017`.`student_documents` ADD `review_status` varchar(20) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'PENDING';

ALTER TABLE `internship_sprint5_v2_verify_20261017`.`student_documents` ADD `reviewed_at` datetime(6) NULL;

ALTER TABLE `internship_sprint5_v2_verify_20261017`.`student_documents` ADD `reviewed_by_user_id` int NULL;

CREATE TABLE `internship_sprint5_v2_verify_20261017`.`email_templates` (
    `id` int NOT NULL AUTO_INCREMENT,
    `code` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `subject_template` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `body_template` text CHARACTER SET utf8mb4 NOT NULL,
    `is_active` tinyint(1) NOT NULL DEFAULT TRUE,
    `created_at` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `updated_at` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    CONSTRAINT `PK_email_templates` PRIMARY KEY (`id`),
    CONSTRAINT `AK_email_templates_code` UNIQUE (`code`)
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `internship_sprint5_v2_verify_20261017`.`email_logs` (
    `id` int NOT NULL AUTO_INCREMENT,
    `student_id` int NULL,
    `recipient_email` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `template_code` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `status` varchar(20) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'PENDING',
    `error_message` text CHARACTER SET utf8mb4 NULL,
    `retry_count` int NOT NULL DEFAULT 0,
    `created_at` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `sent_at` datetime(6) NULL,
    `next_attempt_at` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CONSTRAINT `PK_email_logs` PRIMARY KEY (`id`),
    CONSTRAINT `FK_email_logs_email_templates_template_code` FOREIGN KEY (`template_code`) REFERENCES `internship_sprint5_v2_verify_20261017`.`email_templates` (`code`) ON DELETE RESTRICT,
    CONSTRAINT `FK_email_logs_students_student_id` FOREIGN KEY (`student_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`students` (`id`) ON DELETE SET NULL
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO `internship_sprint5_v2_verify_20261017`.`email_templates` (`id`, `body_template`, `code`, `created_at`, `is_active`, `subject_template`)
VALUES (1, CONCAT('Chào {{StudentName}},', CHAR(10), '', CHAR(10), 'Hồ sơ đăng ký thực tập của bạn đã được duyệt cho chương trình {{ProgramName}}{{StartDate}}.', CHAR(10), '', CHAR(10), 'Bước tiếp theo: {{NextSteps}}', CHAR(10), '', CHAR(10), 'Trân trọng,', CHAR(10), 'Bộ phận Nhân sự'), 'APP_APPROVED', TIMESTAMP '2026-01-01 00:00:00', TRUE, 'Kết quả xét duyệt hồ sơ thực tập'),
(2, CONCAT('Chào {{StudentName}},', CHAR(10), '', CHAR(10), 'Cảm ơn bạn đã đăng ký chương trình {{ProgramName}}. Sau khi xem xét, hồ sơ của bạn chưa được duyệt.', CHAR(10), '', CHAR(10), 'Lý do: {{RejectionReason}}', CHAR(10), '', CHAR(10), 'Nếu cần hỗ trợ thêm, vui lòng liên hệ bộ phận Nhân sự.', CHAR(10), '', CHAR(10), 'Trân trọng,', CHAR(10), 'Bộ phận Nhân sự'), 'APP_REJECTED', TIMESTAMP '2026-01-01 00:00:00', TRUE, 'Kết quả xét duyệt hồ sơ thực tập');

CREATE INDEX `IX_students_reviewed_by_user_id` ON `internship_sprint5_v2_verify_20261017`.`students` (`reviewed_by_user_id`);

CREATE INDEX `IX_student_documents_review_status` ON `internship_sprint5_v2_verify_20261017`.`student_documents` (`review_status`);

CREATE INDEX `IX_student_documents_reviewed_by_user_id` ON `internship_sprint5_v2_verify_20261017`.`student_documents` (`reviewed_by_user_id`);

CREATE INDEX `IX_email_logs_status_next_attempt_at_id` ON `internship_sprint5_v2_verify_20261017`.`email_logs` (`status`, `next_attempt_at`, `id`);

CREATE INDEX `IX_email_logs_student_id_created_at` ON `internship_sprint5_v2_verify_20261017`.`email_logs` (`student_id`, `created_at`);

CREATE INDEX `IX_email_logs_template_code` ON `internship_sprint5_v2_verify_20261017`.`email_logs` (`template_code`);

ALTER TABLE `internship_sprint5_v2_verify_20261017`.`student_documents` ADD CONSTRAINT `FK_student_documents_users_reviewed_by_user_id` FOREIGN KEY (`reviewed_by_user_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`users` (`id`) ON DELETE SET NULL;

ALTER TABLE `internship_sprint5_v2_verify_20261017`.`students` ADD CONSTRAINT `FK_students_users_reviewed_by_user_id` FOREIGN KEY (`reviewed_by_user_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`users` (`id`) ON DELETE SET NULL;

INSERT INTO `internship_sprint5_v2_verify_20261017`.`__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261007101530_Sprint3ReviewAndEmail', '8.0.2');

COMMIT;

START TRANSACTION;

CREATE TABLE `internship_sprint5_v2_verify_20261017`.`allowances` (
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
    CONSTRAINT `FK_allowances_students_student_id` FOREIGN KEY (`student_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`students` (`id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `internship_sprint5_v2_verify_20261017`.`attendances` (
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
    CONSTRAINT `FK_attendances_students_student_id` FOREIGN KEY (`student_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`students` (`id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `internship_sprint5_v2_verify_20261017`.`leave_requests` (
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
    CONSTRAINT `FK_leave_requests_students_student_id` FOREIGN KEY (`student_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`students` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_leave_requests_users_reviewed_by_user_id` FOREIGN KEY (`reviewed_by_user_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`users` (`id`) ON DELETE SET NULL
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `internship_sprint5_v2_verify_20261017`.`student_groups` (
    `id` int NOT NULL AUTO_INCREMENT,
    `name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `description` varchar(500) CHARACTER SET utf8mb4 NULL,
    `status` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    CONSTRAINT `PK_student_groups` PRIMARY KEY (`id`),
    CONSTRAINT `ck_student_groups_status` CHECK (`status` IN ('ACTIVE', 'INACTIVE'))
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `internship_sprint5_v2_verify_20261017`.`support_requests` (
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
    CONSTRAINT `FK_support_requests_students_student_id` FOREIGN KEY (`student_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`students` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_support_requests_users_handled_by_user_id` FOREIGN KEY (`handled_by_user_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`users` (`id`) ON DELETE SET NULL
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `internship_sprint5_v2_verify_20261017`.`allowance_payments` (
    `id` int NOT NULL AUTO_INCREMENT,
    `allowance_id` int NOT NULL,
    `amount` decimal(12,2) NOT NULL,
    `paid_at` datetime(6) NOT NULL,
    `recorded_by_user_id` int NOT NULL,
    `reference` varchar(150) CHARACTER SET utf8mb4 NULL,
    `created_at` datetime(6) NOT NULL,
    CONSTRAINT `PK_allowance_payments` PRIMARY KEY (`id`),
    CONSTRAINT `ck_allowance_payments_amount` CHECK (`amount` > 0),
    CONSTRAINT `FK_allowance_payments_allowances_allowance_id` FOREIGN KEY (`allowance_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`allowances` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_allowance_payments_users_recorded_by_user_id` FOREIGN KEY (`recorded_by_user_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`users` (`id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `internship_sprint5_v2_verify_20261017`.`student_group_memberships` (
    `student_id` int NOT NULL,
    `student_group_id` int NOT NULL,
    `joined_at` datetime(6) NOT NULL,
    CONSTRAINT `PK_student_group_memberships` PRIMARY KEY (`student_group_id`, `student_id`),
    CONSTRAINT `FK_student_group_memberships_student_groups_student_group_id` FOREIGN KEY (`student_group_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`student_groups` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_student_group_memberships_students_student_id` FOREIGN KEY (`student_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`students` (`id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `internship_sprint5_v2_verify_20261017`.`work_schedules` (
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
    CONSTRAINT `FK_work_schedules_student_groups_student_group_id` FOREIGN KEY (`student_group_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`student_groups` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_work_schedules_students_student_id` FOREIGN KEY (`student_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`students` (`id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `internship_sprint5_v2_verify_20261017`.`work_schedule_days` (
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
    CONSTRAINT `FK_work_schedule_days_work_schedules_work_schedule_id` FOREIGN KEY (`work_schedule_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`work_schedules` (`id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE INDEX `IX_allowance_payments_allowance_id_paid_at` ON `internship_sprint5_v2_verify_20261017`.`allowance_payments` (`allowance_id`, `paid_at`);

CREATE INDEX `IX_allowance_payments_recorded_by_user_id` ON `internship_sprint5_v2_verify_20261017`.`allowance_payments` (`recorded_by_user_id`);

CREATE UNIQUE INDEX `IX_allowances_student_id_allowance_type_period_start_period_end` ON `internship_sprint5_v2_verify_20261017`.`allowances` (`student_id`, `allowance_type`, `period_start`, `period_end`);

CREATE INDEX `IX_allowances_student_id_period_start_period_end` ON `internship_sprint5_v2_verify_20261017`.`allowances` (`student_id`, `period_start`, `period_end`);

CREATE INDEX `IX_attendances_date` ON `internship_sprint5_v2_verify_20261017`.`attendances` (`date`);

CREATE UNIQUE INDEX `IX_attendances_student_id_date` ON `internship_sprint5_v2_verify_20261017`.`attendances` (`student_id`, `date`);

CREATE INDEX `IX_leave_requests_reviewed_by_user_id` ON `internship_sprint5_v2_verify_20261017`.`leave_requests` (`reviewed_by_user_id`);

CREATE INDEX `IX_leave_requests_student_id_status_start_date_end_date` ON `internship_sprint5_v2_verify_20261017`.`leave_requests` (`student_id`, `status`, `start_date`, `end_date`);

CREATE INDEX `IX_student_group_memberships_student_group_id` ON `internship_sprint5_v2_verify_20261017`.`student_group_memberships` (`student_group_id`);

CREATE INDEX `IX_student_group_memberships_student_id` ON `internship_sprint5_v2_verify_20261017`.`student_group_memberships` (`student_id`);

CREATE UNIQUE INDEX `IX_student_groups_name` ON `internship_sprint5_v2_verify_20261017`.`student_groups` (`name`);

CREATE INDEX `IX_student_groups_status` ON `internship_sprint5_v2_verify_20261017`.`student_groups` (`status`);

CREATE INDEX `IX_support_requests_handled_by_user_id` ON `internship_sprint5_v2_verify_20261017`.`support_requests` (`handled_by_user_id`);

CREATE INDEX `IX_support_requests_status_created_at` ON `internship_sprint5_v2_verify_20261017`.`support_requests` (`status`, `created_at`);

CREATE INDEX `IX_support_requests_student_id_created_at` ON `internship_sprint5_v2_verify_20261017`.`support_requests` (`student_id`, `created_at`);

CREATE UNIQUE INDEX `IX_work_schedule_days_work_schedule_id_day_of_week` ON `internship_sprint5_v2_verify_20261017`.`work_schedule_days` (`work_schedule_id`, `day_of_week`);

CREATE INDEX `IX_work_schedules_student_group_id` ON `internship_sprint5_v2_verify_20261017`.`work_schedules` (`student_group_id`);

CREATE INDEX `IX_work_schedules_student_id` ON `internship_sprint5_v2_verify_20261017`.`work_schedules` (`student_id`);

INSERT INTO `internship_sprint5_v2_verify_20261017`.`__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261012053013_Sprint4Workforce', '8.0.2');

COMMIT;

START TRANSACTION;

ALTER TABLE `internship_sprint5_v2_verify_20261017`.`work_schedules` ADD `effective_end_date` date NULL;

ALTER TABLE `internship_sprint5_v2_verify_20261017`.`work_schedules` ADD `effective_start_date` date NULL;

ALTER TABLE `internship_sprint5_v2_verify_20261017`.`work_schedules` ADD `recurrence_type` varchar(20) CHARACTER SET utf8mb4 NULL;

ALTER TABLE `internship_sprint5_v2_verify_20261017`.`work_schedules` ADD `recurrence_weeks` int NULL;

ALTER TABLE `internship_sprint5_v2_verify_20261017`.`work_schedules` ADD CONSTRAINT `ck_work_schedules_recurrence` CHECK ((`effective_start_date` IS NULL AND `recurrence_type` IS NULL AND `recurrence_weeks` IS NULL AND `effective_end_date` IS NULL) OR (`effective_start_date` IS NOT NULL AND `recurrence_type` IS NOT NULL AND ((`recurrence_type` = 'NONE' AND `recurrence_weeks` IS NOT NULL AND `recurrence_weeks` = 1 AND `effective_end_date` IS NOT NULL AND `effective_end_date` >= `effective_start_date`) OR (`recurrence_type` = 'WEEKLY' AND ((`recurrence_weeks` IS NULL AND `effective_end_date` IS NULL) OR (`recurrence_weeks` IS NOT NULL AND `recurrence_weeks` BETWEEN 1 AND 52 AND `effective_end_date` IS NOT NULL AND `effective_end_date` >= `effective_start_date`))))));

INSERT INTO `internship_sprint5_v2_verify_20261017`.`__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261013100022_AddWorkScheduleRecurrence', '8.0.2');

COMMIT;

START TRANSACTION;

ALTER TABLE `internship_sprint5_v2_verify_20261017`.`students` MODIFY COLUMN `application_status` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'PENDING_APPROVAL';

CREATE TABLE `internship_sprint5_v2_verify_20261017`.`student_program_audit_events` (
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
    CONSTRAINT `FK_student_program_audit_events_internship_programs_program_id` FOREIGN KEY (`program_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`internship_programs` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_student_program_audit_events_students_student_id` FOREIGN KEY (`student_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`students` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_student_program_audit_events_users_actor_user_id` FOREIGN KEY (`actor_user_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`users` (`id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE INDEX `IX_student_program_audit_events_actor_user_id` ON `internship_sprint5_v2_verify_20261017`.`student_program_audit_events` (`actor_user_id`);

CREATE INDEX `ix_student_program_audit_events_program_id` ON `internship_sprint5_v2_verify_20261017`.`student_program_audit_events` (`program_id`, `id`);

CREATE INDEX `ix_student_program_audit_events_student_program_id` ON `internship_sprint5_v2_verify_20261017`.`student_program_audit_events` (`student_id`, `program_id`, `id`);

INSERT INTO `internship_sprint5_v2_verify_20261017`.`__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261016040114_AddProgramParticipationAuditAndPendingApplicationDefault', '8.0.2');

COMMIT;

START TRANSACTION;

ALTER TABLE `internship_sprint5_v2_verify_20261017`.`internship_tasks` DROP FOREIGN KEY `fk_sprint2_internship_tasks_mentor`;

ALTER TABLE `internship_sprint5_v2_verify_20261017`.`internship_tasks` ADD CONSTRAINT `fk_sprint2_internship_tasks_mentor` FOREIGN KEY (`mentor_id`) REFERENCES `internship_sprint5_v2_verify_20261017`.`mentors` (`id`) ON UPDATE CASCADE ON DELETE RESTRICT;

INSERT INTO `internship_sprint5_v2_verify_20261017`.`__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261017091954_RestrictMentorTaskDelete', '8.0.2');

COMMIT;
-- END EF Core IMigrator script.
