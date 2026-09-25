USE `internship_management`;

SET NAMES utf8mb4;
SET CHARACTER SET utf8mb4;

-- ========================================================
-- 1. CONVERT DATABASE CHARSET & COLLATION
-- ========================================================
ALTER DATABASE `internship_management` 
    CHARACTER SET utf8mb4 
    COLLATE utf8mb4_unicode_ci;

-- ========================================================
-- 2. CONVERT TABLES & ALL TEXT COLUMNS TO UTF8MB4
-- ========================================================
ALTER TABLE `roles` 
    CONVERT TO CHARACTER SET utf8mb4 
    COLLATE utf8mb4_unicode_ci;

ALTER TABLE `users` 
    CONVERT TO CHARACTER SET utf8mb4 
    COLLATE utf8mb4_unicode_ci;

ALTER TABLE `mentors` 
    CONVERT TO CHARACTER SET utf8mb4 
    COLLATE utf8mb4_unicode_ci;

ALTER TABLE `students` 
    CONVERT TO CHARACTER SET utf8mb4 
    COLLATE utf8mb4_unicode_ci;
