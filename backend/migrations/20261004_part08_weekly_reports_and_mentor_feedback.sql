-- Part 8 (US17/US18): student weekly reports and one mentor review per report.
-- Additive migration. Apply after the earlier Sprint 2 migrations.
CREATE TABLE IF NOT EXISTS `weekly_reports` (
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
        FOREIGN KEY (`student_id`) REFERENCES `students` (`id`)
        ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `mentor_feedback` (
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
        FOREIGN KEY (`weekly_report_id`) REFERENCES `weekly_reports` (`id`)
        ON UPDATE CASCADE ON DELETE CASCADE,
    CONSTRAINT `fk_mentor_feedback_mentor`
        FOREIGN KEY (`mentor_id`) REFERENCES `mentors` (`id`)
        ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
