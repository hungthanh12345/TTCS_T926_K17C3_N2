-- US22: Attendance and Leave Tables
USE `internship_management`;

CREATE TABLE IF NOT EXISTS `attendances` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `student_id` INT NOT NULL,
    `date` DATE NOT NULL,
    `check_in` TIME NULL,
    `check_out` TIME NULL,
    `status` ENUM('PRESENT', 'LATE', 'ABSENT') NOT NULL DEFAULT 'ABSENT',
    `created_at` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `updated_at` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_attendances_student_date` (`student_id`, `date`),
    KEY `idx_attendances_date` (`date`),
    CONSTRAINT `fk_attendances_student`
        FOREIGN KEY (`student_id`) REFERENCES `students` (`id`)
        ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `leave_requests` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `student_id` INT NOT NULL,
    `start_date` DATE NOT NULL,
    `end_date` DATE NOT NULL,
    `reason` TEXT NOT NULL,
    `status` ENUM('PENDING', 'APPROVED', 'REJECTED') NOT NULL DEFAULT 'PENDING',
    `created_at` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `updated_at` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`id`),
    KEY `idx_leave_requests_student` (`student_id`),
    KEY `idx_leave_requests_dates` (`start_date`, `end_date`),
    CONSTRAINT `fk_leave_requests_student`
        FOREIGN KEY (`student_id`) REFERENCES `students` (`id`)
        ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
