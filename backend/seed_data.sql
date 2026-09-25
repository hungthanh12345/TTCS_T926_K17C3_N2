USE `internship_management`;

SET NAMES utf8mb4;
SET CHARACTER SET utf8mb4;

-- ========================================================
-- 1. INSERT SYSTEM ROLES
-- ========================================================
INSERT INTO `roles` (`id`, `name`) VALUES
(1, 'ROLE_ADMIN'),
(2, 'ROLE_HR'),
(3, 'ROLE_MENTOR'),
(4, 'ROLE_STUDENT')
ON DUPLICATE KEY UPDATE `name` = VALUES(`name`);

-- ========================================================
-- 2. INSERT SYSTEM USERS (Password: Admin@123)
-- BCrypt Hash: $2b$10$hTMC70vv1GUJ/jKIj66Rye300BycH167s1dBMG7ijn2v2BI.dJlZ.
-- ========================================================
INSERT INTO `users` (`id`, `email`, `password_hash`, `role_id`, `status`) VALUES
-- Quản trị viên (Admin)
(1, 'hung.nt.admin@gmail.com', '$2b$10$hTMC70vv1GUJ/jKIj66Rye300BycH167s1dBMG7ijn2v2BI.dJlZ.', 1, 'ACTIVE'),

-- Quản lý Nhân sự (HR)
(2, 'customer.hr@company.com', '$2b$10$hTMC70vv1GUJ/jKIj66Rye300BycH167s1dBMG7ijn2v2BI.dJlZ.', 2, 'ACTIVE'),

-- Mentor Doanh nghiệp Duy nhất (Nguyễn Khánh Tùng)
(3, 'tung.nk@gmail.com', '$2b$10$hTMC70vv1GUJ/jKIj66Rye300BycH167s1dBMG7ijn2v2BI.dJlZ.', 3, 'ACTIVE'),

-- Sinh viên Thực tập (10 Thành viên sinh viên)
(4, 'hung.nt@gmail.com', '$2b$10$hTMC70vv1GUJ/jKIj66Rye300BycH167s1dBMG7ijn2v2BI.dJlZ.', 4, 'ACTIVE'),
(5, 'hung.dm@gmail.com', '$2b$10$hTMC70vv1GUJ/jKIj66Rye300BycH167s1dBMG7ijn2v2BI.dJlZ.', 4, 'ACTIVE'),
(6, 'hung.ht@gmail.com', '$2b$10$hTMC70vv1GUJ/jKIj66Rye300BycH167s1dBMG7ijn2v2BI.dJlZ.', 4, 'ACTIVE'),
(7, 'giang.td@gmail.com', '$2b$10$hTMC70vv1GUJ/jKIj66Rye300BycH167s1dBMG7ijn2v2BI.dJlZ.', 4, 'ACTIVE'),
(8, 'duong.dh@gmail.com', '$2b$10$hTMC70vv1GUJ/jKIj66Rye300BycH167s1dBMG7ijn2v2BI.dJlZ.', 4, 'ACTIVE'),
(9, 'han.dd@gmail.com', '$2b$10$hTMC70vv1GUJ/jKIj66Rye300BycH167s1dBMG7ijn2v2BI.dJlZ.', 4, 'ACTIVE'),
(10, 'huan.bn@gmail.com', '$2b$10$hTMC70vv1GUJ/jKIj66Rye300BycH167s1dBMG7ijn2v2BI.dJlZ.', 4, 'ACTIVE'),
(11, 'hai.nh@gmail.com', '$2b$10$hTMC70vv1GUJ/jKIj66Rye300BycH167s1dBMG7ijn2v2BI.dJlZ.', 4, 'ACTIVE'),
(12, 'huong.ph@gmail.com', '$2b$10$hTMC70vv1GUJ/jKIj66Rye300BycH167s1dBMG7ijn2v2BI.dJlZ.', 4, 'ACTIVE'),
(13, 'giang.nt@gmail.com', '$2b$10$hTMC70vv1GUJ/jKIj66Rye300BycH167s1dBMG7ijn2v2BI.dJlZ.', 4, 'ACTIVE')
ON DUPLICATE KEY UPDATE 
    `email` = VALUES(`email`),
    `password_hash` = VALUES(`password_hash`), 
    `role_id` = VALUES(`role_id`),
    `status` = VALUES(`status`);

-- ========================================================
-- 3. CLEAN UP & ENSURE EXACTLY ONE CORPORATE MENTOR
-- ========================================================
-- First update students referring to other mentors
UPDATE `students` SET `mentor_id` = NULL WHERE `mentor_id` != 1;
DELETE FROM `students` WHERE `id` > 10;
DELETE FROM `mentors` WHERE `id` != 1;
DELETE FROM `users` WHERE `id` NOT IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13);

-- Insert / Update exactly 1 mentor: Nguyễn Khánh Tùng
INSERT INTO `mentors` (`id`, `user_id`, `full_name`, `phone_number`, `department`, `specialization`) VALUES
(1, 3, N'Nguyễn Khánh Tùng', '0912345678', N'Kỹ thuật phần mềm', N'Full-stack Web và Cloud Native')
ON DUPLICATE KEY UPDATE 
    `user_id` = VALUES(`user_id`),
    `full_name` = VALUES(`full_name`), 
    `phone_number` = VALUES(`phone_number`),
    `department` = VALUES(`department`),
    `specialization` = VALUES(`specialization`);

-- ========================================================
-- 4. INSERT STUDENTS (Clean Vietnamese Diacritics)
-- All 10 students assigned to Mentor 1 (Nguyễn Khánh Tùng)
-- All 10 students: Đại học Công nghệ Thông tin và Truyền thông — ĐHTN | Kỹ thuật Phần mềm
-- ========================================================
INSERT INTO `students` (`id`, `user_id`, `student_code`, `full_name`, `phone_number`, `university`, `major`, `mentor_id`) VALUES
(1, 4, 'SV2026001', N'Nguyễn Thành Hưng', '0987654321', N'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN', N'Kỹ thuật Phần mềm', 1),
(2, 5, 'SV2026002', N'Dương Minh Hưng', '0987654322', N'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN', N'Kỹ thuật Phần mềm', 1),
(3, 6, 'SV2026003', N'Hoàng Thanh Hùng', '0987654323', N'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN', N'Kỹ thuật Phần mềm', 1),
(4, 7, 'SV2026004', N'Trương Đình Giang', '0987654324', N'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN', N'Kỹ thuật Phần mềm', 1),
(5, 8, 'SV2026005', N'Dương Hải Dương', '0987654325', N'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN', N'Kỹ thuật Phần mềm', 1),
(6, 9, 'SV2026006', N'Diệp Đình Hân', '0987654326', N'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN', N'Kỹ thuật Phần mềm', 1),
(7, 10, 'SV2026007', N'Bùi Ngọc Huân', '0987654327', N'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN', N'Kỹ thuật Phần mềm', 1),
(8, 11, 'SV2026008', N'Nguyễn Hồng Hải', '0987654328', N'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN', N'Kỹ thuật Phần mềm', 1),
(9, 12, 'SV2026009', N'Phạm Hải Hướng', '0987654329', N'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN', N'Kỹ thuật Phần mềm', 1),
(10, 13, 'SV2026010', N'Nguyễn Thị Giang', '0987654330', N'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN', N'Kỹ thuật Phần mềm', 1)
ON DUPLICATE KEY UPDATE 
    `user_id` = VALUES(`user_id`),
    `full_name` = VALUES(`full_name`),
    `student_code` = VALUES(`student_code`),
    `phone_number` = VALUES(`phone_number`),
    `university` = VALUES(`university`),
    `major` = VALUES(`major`),
    `mentor_id` = VALUES(`mentor_id`);