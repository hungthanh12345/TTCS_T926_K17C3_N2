-- Part 09 (US19/US20): final mentor evaluation for each current student profile.
-- Students are not associated with internship_programs in the current schema;
-- this migration deliberately preserves that existing domain boundary.
CREATE TABLE IF NOT EXISTS `internship_evaluations` (
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
        FOREIGN KEY (`student_id`) REFERENCES `students` (`id`)
        ON UPDATE CASCADE ON DELETE CASCADE,
    CONSTRAINT `fk_internship_evaluations_mentor`
        FOREIGN KEY (`mentor_id`) REFERENCES `mentors` (`id`)
        ON UPDATE CASCADE ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
