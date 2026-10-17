START TRANSACTION;

ALTER TABLE `internship_tasks` DROP FOREIGN KEY `fk_sprint2_internship_tasks_mentor`;

ALTER TABLE `internship_tasks` ADD CONSTRAINT `fk_sprint2_internship_tasks_mentor` FOREIGN KEY (`mentor_id`) REFERENCES `mentors` (`id`) ON UPDATE CASCADE ON DELETE RESTRICT;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261017091954_RestrictMentorTaskDelete', '8.0.2');

COMMIT;
