-- Forward SQL alternative for the raw-SQL bootstrap path only.
-- Do not edit or replay 20261003_part05_mentor_task_assignment.sql on an existing database.
-- Preflight must confirm this exact FK name and mentors(id) target.
-- The matching EF migration has the same effect; do not run both paths in one deployment.
-- This changes only the FK action and does not update or delete task rows.
ALTER TABLE `internship_tasks`
    DROP FOREIGN KEY `fk_sprint2_internship_tasks_mentor`,
    ADD CONSTRAINT `fk_sprint2_internship_tasks_mentor`
        FOREIGN KEY (`mentor_id`) REFERENCES `mentors` (`id`)
        ON UPDATE CASCADE ON DELETE RESTRICT;
