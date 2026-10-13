-- Rehearsal-only copy of US-09; do not execute until the exact SQL plan is approved.
-- All created/referenced objects are explicitly scoped to internship_restore_check.
-- Preflight required: confirm parent id columns are signed INT, parent tables use InnoDB,
-- target table does not already exist, and the connected account is restricted to this schema.
-- The Student FK uses ON DELETE RESTRICT to preserve the contract when deletion is attempted.
-- Keep UploadedByUserId and ConfirmedByUserId delete behavior unchanged (SET NULL).

CREATE TABLE `internship_restore_check`.`internship_contracts` (
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
    CONSTRAINT `chk_internship_contracts_status`
        CHECK (`status` IN ('PENDING_CONFIRMATION', 'CONFIRMED', 'CANCELLED')),
    CONSTRAINT `fk_internship_contracts_student`
        FOREIGN KEY (`student_id`) REFERENCES `internship_restore_check`.`students` (`id`)
        ON UPDATE CASCADE ON DELETE RESTRICT,
    CONSTRAINT `fk_internship_contracts_uploaded_by`
        FOREIGN KEY (`uploaded_by_user_id`) REFERENCES `internship_restore_check`.`users` (`id`)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT `fk_internship_contracts_confirmed_by`
        FOREIGN KEY (`confirmed_by_user_id`) REFERENCES `internship_restore_check`.`users` (`id`)
        ON UPDATE CASCADE ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;