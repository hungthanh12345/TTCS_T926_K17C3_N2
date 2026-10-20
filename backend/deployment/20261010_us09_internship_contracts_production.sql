-- Production deployment artifact for US-09 (internship contracts).
-- Target and all parent tables are explicitly scoped to internship_management.
-- Preflight: confirm this table is absent, parent IDs have matching signed INT types,
-- and parent tables use InnoDB. This CREATE fails if the target already exists.
-- Retention policy: ON DELETE RESTRICT prevents deleting a student while a contract exists,
-- preserving the contract file and confirmation history.
-- This file contains schema creation only; it does not copy or modify existing rows.
CREATE TABLE `internship_management`.`internship_contracts` (
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
        FOREIGN KEY (`student_id`) REFERENCES `internship_management`.`students` (`id`)
        ON UPDATE CASCADE ON DELETE RESTRICT,
    CONSTRAINT `fk_internship_contracts_uploaded_by`
        FOREIGN KEY (`uploaded_by_user_id`) REFERENCES `internship_management`.`users` (`id`)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT `fk_internship_contracts_confirmed_by`
        FOREIGN KEY (`confirmed_by_user_id`) REFERENCES `internship_management`.`users` (`id`)
        ON UPDATE CASCADE ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
