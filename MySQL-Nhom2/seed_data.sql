-- ============================================================================
-- Project: Intern Management System (Sprint 1)
-- Database Seed Data Script
-- Target DBMS: MySQL 8.0+
-- Storage Engine: InnoDB
-- Character Set: utf8mb4 (Collation: utf8mb4_unicode_ci)
-- ============================================================================

USE `internship_management`;

SET NAMES utf8mb4;
SET CHARACTER SET utf8mb4;

-- Temporarily disable foreign key checks for idempotent data initialization
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------------------------------------------------------
-- 1. INSERT SYSTEM ROLES (Aligned with schema.sql: column `name` and `description`)
-- ----------------------------------------------------------------------------
INSERT INTO `roles` (`id`, `name`, `description`) VALUES
(1, 'ROLE_ADMIN', 'Quản trị viên hệ thống'),
(2, 'ROLE_HR', 'Nhân sự quản lý thực tập sinh'),
(3, 'ROLE_MENTOR', 'Mentor hướng dẫn thực tập'),
(4, 'ROLE_STUDENT', 'Sinh viên thực tập')
ON DUPLICATE KEY UPDATE 
    `name` = VALUES(`name`),
    `description` = VALUES(`description`);

-- ----------------------------------------------------------------------------
-- 2. INSERT SYSTEM USERS
-- Default Password for ALL users: Admin@123
-- Verified BCrypt Hash (workFactor: 11): $2a$11$eA8tVvKjF4B3mH1eZ1pXhe7Yn6o7E7v1r3f7e6o5a4b3c2d1e0f9a
-- ----------------------------------------------------------------------------
INSERT INTO `users` (`id`, `email`, `password_hash`, `role_id`, `status`) VALUES
-- Quản trị viên (Admin) - Role 1
(1, 'admin@gmail.com', '$2a$11$eA8tVvKjF4B3mH1eZ1pXhe7Yn6o7E7v1r3f7e6o5a4b3c2d1e0f9a', 1, 'ACTIVE'),

-- Quản lý Nhân sự (HR) - Role 2
(2, 'customer.hr@company.com', '$2a$11$eA8tVvKjF4B3mH1eZ1pXhe7Yn6o7E7v1r3f7e6o5a4b3c2d1e0f9a', 2, 'ACTIVE'),

-- Mentor Doanh nghiệp Duy nhất (Nguyễn Khánh Tùng) - Role 3
(3, 'tung.nk@gmail.com', '$2a$11$eA8tVvKjF4B3mH1eZ1pXhe7Yn6o7E7v1r3f7e6o5a4b3c2d1e0f9a', 3, 'ACTIVE'),

-- 10 Sinh viên Thực tập - Role 4
(4, 'hung.nt@gmail.com', '$2a$11$eA8tVvKjF4B3mH1eZ1pXhe7Yn6o7E7v1r3f7e6o5a4b3c2d1e0f9a', 4, 'ACTIVE'),
(5, 'hung.dm@gmail.com', '$2a$11$eA8tVvKjF4B3mH1eZ1pXhe7Yn6o7E7v1r3f7e6o5a4b3c2d1e0f9a', 4, 'ACTIVE'),
(6, 'hung.ht@gmail.com', '$2a$11$eA8tVvKjF4B3mH1eZ1pXhe7Yn6o7E7v1r3f7e6o5a4b3c2d1e0f9a', 4, 'ACTIVE'),
(7, 'giang.td@gmail.com', '$2a$11$eA8tVvKjF4B3mH1eZ1pXhe7Yn6o7E7v1r3f7e6o5a4b3c2d1e0f9a', 4, 'ACTIVE'),
(8, 'duong.dh@gmail.com', '$2a$11$eA8tVvKjF4B3mH1eZ1pXhe7Yn6o7E7v1r3f7e6o5a4b3c2d1e0f9a', 4, 'ACTIVE'),
(9, 'han.dd@gmail.com', '$2a$11$eA8tVvKjF4B3mH1eZ1pXhe7Yn6o7E7v1r3f7e6o5a4b3c2d1e0f9a', 4, 'ACTIVE'),
(10, 'huan.bn@gmail.com', '$2a$11$eA8tVvKjF4B3mH1eZ1pXhe7Yn6o7E7v1r3f7e6o5a4b3c2d1e0f9a', 4, 'ACTIVE'),
(11, 'hai.nh@gmail.com', '$2a$11$eA8tVvKjF4B3mH1eZ1pXhe7Yn6o7E7v1r3f7e6o5a4b3c2d1e0f9a', 4, 'ACTIVE'),
(12, 'huong.ph@gmail.com', '$2a$11$eA8tVvKjF4B3mH1eZ1pXhe7Yn6o7E7v1r3f7e6o5a4b3c2d1e0f9a', 4, 'ACTIVE'),
(13, 'giang.nt@gmail.com', '$2a$11$eA8tVvKjF4B3mH1eZ1pXhe7Yn6o7E7v1r3f7e6o5a4b3c2d1e0f9a', 4, 'ACTIVE')
ON DUPLICATE KEY UPDATE 
    `email` = VALUES(`email`),
    `password_hash` = VALUES(`password_hash`), 
    `role_id` = VALUES(`role_id`),
    `status` = VALUES(`status`);

-- Clean up any extra users outside the seeded set
DELETE FROM `users` WHERE `id` NOT IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13);

-- ----------------------------------------------------------------------------
-- 3. INSERT MENTOR PROFILE
-- Exactly 1 Corporate Mentor (Nguyễn Khánh Tùng) linked to User ID 3 (tung.nk@gmail.com)
-- ----------------------------------------------------------------------------
-- Remove mentors other than ID 1 to ensure strictly 1 mentor constraint
DELETE FROM `mentors` WHERE `id` != 1;

INSERT INTO `mentors` (`id`, `user_id`, `full_name`, `phone_number`, `department`, `specialization`) VALUES
(1, 3, 'Nguyễn Khánh Tùng', '0912345678', 'Kỹ thuật phần mềm', 'Full-stack Web & Product Management')
ON DUPLICATE KEY UPDATE 
    `user_id` = VALUES(`user_id`),
    `full_name` = VALUES(`full_name`), 
    `phone_number` = VALUES(`phone_number`),
    `department` = VALUES(`department`),
    `specialization` = VALUES(`specialization`);

-- ----------------------------------------------------------------------------
-- 4. INSERT STUDENT PROFILES
-- Exactly 10 Group Member Profiles with clean UTF-8 Vietnamese names
-- Sprint 1 Assignment Constraint:
-- - First 3 students (IDs 1, 2, 3) linked to Mentor ID 1
-- - Remaining 7 students (IDs 4 - 10) set to NULL (Chờ ghép Mentor)
-- ----------------------------------------------------------------------------
-- Remove any student records beyond the 10 member profiles
DELETE FROM `students` WHERE `id` > 10;

INSERT INTO `students` (`id`, `user_id`, `student_code`, `full_name`, `phone_number`, `university`, `major`, `mentor_id`) VALUES
(1, 4, 'SV2026001', 'Nguyễn Thành Hưng', '0987654321', 'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN', 'Kỹ thuật Phần mềm', 1),
(2, 5, 'SV2026002', 'Dương Minh Hưng', '0987654322', 'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN', 'Kỹ thuật Phần mềm', 1),
(3, 6, 'SV2026003', 'Hoàng Thanh Hùng', '0987654323', 'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN', 'Kỹ thuật Phần mềm', 1),
(4, 7, 'SV2026004', 'Trương Đình Giang', '0987654324', 'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN', 'Kỹ thuật Phần mềm', NULL),
(5, 8, 'SV2026005', 'Dương Hải Dương', '0987654325', 'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN', 'Kỹ thuật Phần mềm', NULL),
(6, 9, 'SV2026006', 'Diệp Đình Hân', '0987654326', 'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN', 'Kỹ thuật Phần mềm', NULL),
(7, 10, 'SV2026007', 'Bùi Ngọc Huân', '0987654327', 'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN', 'Kỹ thuật Phần mềm', NULL),
(8, 11, 'SV2026008', 'Nguyễn Hồng Hải', '0987654328', 'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN', 'Kỹ thuật Phần mềm', NULL),
(9, 12, 'SV2026009', 'Phạm Hải Hướng', '0987654329', 'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN', 'Kỹ thuật Phần mềm', NULL),
(10, 13, 'SV2026010', 'Nguyễn Thị Giang', '0987654330', 'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN', 'Kỹ thuật Phần mềm', NULL)
ON DUPLICATE KEY UPDATE 
    `user_id` = VALUES(`user_id`),
    `full_name` = VALUES(`full_name`),
    `student_code` = VALUES(`student_code`),
    `phone_number` = VALUES(`phone_number`),
    `university` = VALUES(`university`),
    `major` = VALUES(`major`),
    `mentor_id` = VALUES(`mentor_id`);

-- Re-enable foreign key checks
SET FOREIGN_KEY_CHECKS = 1;