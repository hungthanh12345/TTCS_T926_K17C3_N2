-- ============================================================================
-- Project: Intern Management System (Sprint 1)
-- Database Schema Definition (DDL)
-- Target DBMS: MySQL 8.0+
-- Storage Engine: InnoDB
-- Character Set: utf8mb4 (Collation: utf8mb4_unicode_ci)
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 1. DATABASE CREATION
-- ----------------------------------------------------------------------------
CREATE DATABASE IF NOT EXISTS `internship_management`
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE `internship_management`;

-- Disable foreign key checks during schema creation/reset
SET FOREIGN_KEY_CHECKS = 0;

-- Drop tables if they already exist (in reverse dependency order)
DROP TABLE IF EXISTS `students`;
DROP TABLE IF EXISTS `mentors`;
DROP TABLE IF EXISTS `users`;
DROP TABLE IF EXISTS `roles`;

-- Re-enable foreign key checks
SET FOREIGN_KEY_CHECKS = 1;

-- ----------------------------------------------------------------------------
-- 2. TABLE DEFINITIONS
-- ----------------------------------------------------------------------------

-- ----------------------------------------------------------------------------
-- Table: roles
-- Description: Stores system roles for RBAC (Role-Based Access Control)
-- Applicable User Stories: US 39, US 40
-- ----------------------------------------------------------------------------
CREATE TABLE `roles` (
    `id` INT AUTO_INCREMENT,
    `name` VARCHAR(50) NOT NULL,
    `description` VARCHAR(255) NULL,
    `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    CONSTRAINT `pk_roles` PRIMARY KEY (`id`),
    CONSTRAINT `uk_roles_name` UNIQUE (`name`)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci
  COMMENT = 'System access roles for authentication and RBAC';

-- ----------------------------------------------------------------------------
-- Table: users
-- Description: Stores authentication credentials and account status
-- Applicable User Stories: US 39 (Admin creates accounts), US 40 (Auth & JWT)
-- ----------------------------------------------------------------------------
CREATE TABLE `users` (
    `id` INT AUTO_INCREMENT,
    `email` VARCHAR(150) NOT NULL,
    `password_hash` VARCHAR(255) NOT NULL,
    `role_id` INT NOT NULL,
    `status` ENUM('ACTIVE', 'INACTIVE', 'LOCKED') NOT NULL DEFAULT 'ACTIVE',
    `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT `pk_users` PRIMARY KEY (`id`),
    CONSTRAINT `uk_users_email` UNIQUE (`email`),
    CONSTRAINT `fk_users_role` FOREIGN KEY (`role_id`)
        REFERENCES `roles` (`id`)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci
  COMMENT = 'User login credentials and account states';

-- ----------------------------------------------------------------------------
-- Table: mentors
-- Description: Stores mentor profile details
-- Applicable User Stories: US 29 (Create mentor profile), US 30 (Mentor assignment)
-- ----------------------------------------------------------------------------
CREATE TABLE `mentors` (
    `id` INT AUTO_INCREMENT,
    `user_id` INT NOT NULL,
    `full_name` VARCHAR(100) NOT NULL,
    `phone_number` VARCHAR(20) NULL,
    `department` VARCHAR(100) NOT NULL,
    `specialization` VARCHAR(150) NULL,
    `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT `pk_mentors` PRIMARY KEY (`id`),
    CONSTRAINT `uk_mentors_user_id` UNIQUE (`user_id`),
    CONSTRAINT `fk_mentors_user` FOREIGN KEY (`user_id`)
        REFERENCES `users` (`id`)
        ON UPDATE CASCADE
        ON DELETE CASCADE
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci
  COMMENT = 'Mentor professional profiles linked to user accounts';

-- ----------------------------------------------------------------------------
-- Table: students
-- Description: Stores intern personal profiles and mentor assignment
-- Applicable User Stories:
--   US 1  (Create intern profile)
--   US 2  (Update intern profile)
--   US 3  (Search & filter by university and major)
--   US 30 (Assign mentor to intern)
-- ----------------------------------------------------------------------------
CREATE TABLE `students` (
    `id` INT AUTO_INCREMENT,
    `user_id` INT NULL,
    `student_code` VARCHAR(50) NOT NULL,
    `full_name` VARCHAR(100) NOT NULL,
    `phone_number` VARCHAR(20) NULL,
    `university` VARCHAR(150) NOT NULL,
    `major` VARCHAR(100) NOT NULL,
    `mentor_id` INT NULL,
    `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT `pk_students` PRIMARY KEY (`id`),
    CONSTRAINT `uk_students_student_code` UNIQUE (`student_code`),
    CONSTRAINT `uk_students_user_id` UNIQUE (`user_id`),
    CONSTRAINT `fk_students_user` FOREIGN KEY (`user_id`)
        REFERENCES `users` (`id`)
        ON UPDATE CASCADE
        ON DELETE SET NULL,
    CONSTRAINT `fk_students_mentor` FOREIGN KEY (`mentor_id`)
        REFERENCES `mentors` (`id`)
        ON UPDATE CASCADE
        ON DELETE SET NULL
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci
  COMMENT = 'Intern profiles, academic info, and mentor assignments';

-- ----------------------------------------------------------------------------
-- 3. PERFORMANCE INDEXES
-- Support search and filter requirements for US 3 & relational lookups
-- ----------------------------------------------------------------------------

-- Indexes on students table for fast filtering by University and Major (US 3)
CREATE INDEX `idx_students_university` ON `students` (`university`);
CREATE INDEX `idx_students_major` ON `students` (`major`);
CREATE INDEX `idx_students_university_major` ON `students` (`university`, `major`);

-- Foreign key lookup indexes
CREATE INDEX `idx_students_mentor_id` ON `students` (`mentor_id`);
CREATE INDEX `idx_users_role_id` ON `users` (`role_id`);
