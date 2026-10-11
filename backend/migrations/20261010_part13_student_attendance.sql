-- Safe additive migration for US21: Student Attendance Check-in / Check-out
CREATE TABLE IF NOT EXISTS `attendances` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `student_id` INT NOT NULL,
    `date` DATE NOT NULL,
    `check_in_time` DATETIME(6) NOT NULL,
    `check_out_time` DATETIME(6) NULL,
    `duration_minutes` INT NULL,
    `status` VARCHAR(20) NOT NULL DEFAULT 'CHECKED_IN',
    `notes` VARCHAR(500) NULL,
    `created_at` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `updated_at` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_attendances_student_date` (`student_id`, `date`),
    KEY `idx_attendances_student_id` (`student_id`),
    KEY `idx_attendances_date` (`date`),
    CONSTRAINT `fk_attendances_student` FOREIGN KEY (`student_id`)
        REFERENCES `students` (`id`) ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
