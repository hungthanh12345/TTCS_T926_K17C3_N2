-- Part 11: associate existing student profiles and applications with an internship program.
-- Additive and idempotent: existing rows remain unassigned (program_id = NULL).
USE `internship_management`;

SET @part11_add_program_id = IF(
    EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'students'
          AND COLUMN_NAME = 'program_id'
    ),
    'SELECT 1',
    'ALTER TABLE `students` ADD COLUMN `program_id` INT NULL'
);
PREPARE part11_program_id_stmt FROM @part11_add_program_id;
EXECUTE part11_program_id_stmt;
DEALLOCATE PREPARE part11_program_id_stmt;

SET @part11_add_program_index = IF(
    EXISTS (
        SELECT 1 FROM information_schema.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'students'
          AND INDEX_NAME = 'idx_students_program_id'
    ),
    'SELECT 1',
    'CREATE INDEX `idx_students_program_id` ON `students` (`program_id`)'
);
PREPARE part11_program_index_stmt FROM @part11_add_program_index;
EXECUTE part11_program_index_stmt;
DEALLOCATE PREPARE part11_program_index_stmt;

SET @part11_add_program_fk = IF(
    EXISTS (
        SELECT 1 FROM information_schema.KEY_COLUMN_USAGE
        WHERE CONSTRAINT_SCHEMA = DATABASE()
          AND TABLE_NAME = 'students'
          AND COLUMN_NAME = 'program_id'
          AND REFERENCED_TABLE_NAME = 'internship_programs'
          AND REFERENCED_COLUMN_NAME = 'id'
    ),
    'SELECT 1',
    'ALTER TABLE `students` ADD CONSTRAINT `fk_students_program` FOREIGN KEY (`program_id`) REFERENCES `internship_programs` (`id`) ON UPDATE CASCADE ON DELETE SET NULL'
);
PREPARE part11_program_fk_stmt FROM @part11_add_program_fk;
EXECUTE part11_program_fk_stmt;
DEALLOCATE PREPARE part11_program_fk_stmt;
