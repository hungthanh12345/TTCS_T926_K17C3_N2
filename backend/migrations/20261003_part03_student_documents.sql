-- Part 3 (US04): private student documents stored with their owning student profile.
-- This migration also upgrades the legacy US04 table without discarding stored metadata.
CREATE TABLE IF NOT EXISTS `student_documents` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `student_id` INT NOT NULL,
    `document_type` VARCHAR(40) NOT NULL,
    `original_file_name` VARCHAR(255) NOT NULL,
    `stored_file_name` VARCHAR(255) NULL,
    `content_type` VARCHAR(150) NOT NULL,
    `size_bytes` BIGINT NOT NULL,
    `file_content` LONGBLOB NULL,
    `uploaded_at` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`id`),
    KEY `idx_student_documents_student_uploaded` (`student_id`, `uploaded_at`),
    CONSTRAINT `fk_student_documents_student`
        FOREIGN KEY (`student_id`) REFERENCES `students` (`id`)
        ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

SET @add_student_document_file_content = IF(
    EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'student_documents'
          AND COLUMN_NAME = 'file_content'
    ),
    'SELECT 1',
    'ALTER TABLE `student_documents` ADD COLUMN `file_content` LONGBLOB NULL'
);
PREPARE add_student_document_file_content_stmt FROM @add_student_document_file_content;
EXECUTE add_student_document_file_content_stmt;
DEALLOCATE PREPARE add_student_document_file_content_stmt;

SET @add_student_documents_owner_index = IF(
    EXISTS (
        SELECT 1 FROM information_schema.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'student_documents'
          AND INDEX_NAME = 'idx_student_documents_student_uploaded'
    ),
    'SELECT 1',
    'CREATE INDEX `idx_student_documents_student_uploaded` ON `student_documents` (`student_id`, `uploaded_at`)'
);
PREPARE add_student_documents_owner_index_stmt FROM @add_student_documents_owner_index;
EXECUTE add_student_documents_owner_index_stmt;
DEALLOCATE PREPARE add_student_documents_owner_index_stmt;

SET @add_student_documents_student_fk = IF(
    EXISTS (
        SELECT 1 FROM information_schema.KEY_COLUMN_USAGE
        WHERE CONSTRAINT_SCHEMA = DATABASE()
          AND TABLE_NAME = 'student_documents'
          AND COLUMN_NAME = 'student_id'
          AND REFERENCED_TABLE_NAME = 'students'
          AND REFERENCED_COLUMN_NAME = 'id'
    ),
    'SELECT 1',
    'ALTER TABLE `student_documents` ADD CONSTRAINT `fk_student_documents_student` FOREIGN KEY (`student_id`) REFERENCES `students` (`id`) ON UPDATE CASCADE ON DELETE CASCADE'
);
PREPARE add_student_documents_student_fk_stmt FROM @add_student_documents_student_fk;
EXECUTE add_student_documents_student_fk_stmt;
DEALLOCATE PREPARE add_student_documents_student_fk_stmt;
