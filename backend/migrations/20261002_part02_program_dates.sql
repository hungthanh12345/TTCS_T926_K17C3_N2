-- Part 2 (US13) depends on Part 1's internship_programs table.
-- Run with the Part 1 database selected; this migration adds only US13 date columns.
SET @part2_add_start_date = IF(
    EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'internship_programs'
          AND COLUMN_NAME = 'start_date'
    ),
    'SELECT 1',
    'ALTER TABLE `internship_programs` ADD COLUMN `start_date` DATE NULL'
);
PREPARE part2_start_date_stmt FROM @part2_add_start_date;
EXECUTE part2_start_date_stmt;
DEALLOCATE PREPARE part2_start_date_stmt;

SET @part2_add_end_date = IF(
    EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'internship_programs'
          AND COLUMN_NAME = 'end_date'
    ),
    'SELECT 1',
    'ALTER TABLE `internship_programs` ADD COLUMN `end_date` DATE NULL'
);
PREPARE part2_end_date_stmt FROM @part2_add_end_date;
EXECUTE part2_end_date_stmt;
DEALLOCATE PREPARE part2_end_date_stmt;
