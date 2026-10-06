-- Part 5 (US15): mentor-created tasks linked to their assigned mentor and student.
-- Reuses the existing internship_tasks table when the deployment already has it.
-- Only the initial TO_DO state is included; progress states belong to Part 6.
CREATE TABLE IF NOT EXISTS `internship_tasks` (
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
    CONSTRAINT `fk_sprint2_internship_tasks_mentor` FOREIGN KEY (`mentor_id`) REFERENCES `mentors` (`id`)
        ON UPDATE CASCADE ON DELETE CASCADE,
    CONSTRAINT `fk_sprint2_internship_tasks_student` FOREIGN KEY (`student_id`) REFERENCES `students` (`id`)
        ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
