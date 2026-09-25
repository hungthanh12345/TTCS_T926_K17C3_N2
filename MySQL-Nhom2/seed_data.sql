USE `internship_management`;

-- 1. Insert System Roles
INSERT INTO `roles` (`id`, `role_name`) VALUES
(1, 'ROLE_ADMIN'),
(2, 'ROLE_HR'),
(3, 'ROLE_MENTOR'),
(4, 'ROLE_STUDENT')
ON DUPLICATE KEY UPDATE `role_name` = VALUES(`role_name`);

-- 2. Insert Default Users (Password: Admin@123 hashed via Bcrypt for testing)
INSERT INTO `users` (`id`, `email`, `password_hash`, `role_id`, `status`) VALUES
-- Admin / HR Account
(1, 'admin@gmail.com', '$2a$10$e8pA70W0p8V5Qn7zI7K3A.e2U8vJz.9G3E7x.xY4zY6y8w7x9v1e', 1, 'ACTIVE'),

-- Mentor Account (Product Owner)
(2, 'tung.nk@gmail.com', '$2a$10$e8pA70W0p8V5Qn7zI7K3A.e2U8vJz.9G3E7x.xY4zY6y8w7x9v1e', 3, 'ACTIVE'),

-- Student Accounts ( Exactly 10 Group Members)
(3, 'hung.nt@gmail.com', '$2a$10$e8pA70W0p8V5Qn7zI7K3A.e2U8vJz.9G3E7x.xY4zY6y8w7x9v1e', 4, 'ACTIVE'),
(4, 'hung.dm@gmail.com', '$2a$10$e8pA70W0p8V5Qn7zI7K3A.e2U8vJz.9G3E7x.xY4zY6y8w7x9v1e', 4, 'ACTIVE'),
(5, 'hung.ht@gmail.com', '$2a$10$e8pA70W0p8V5Qn7zI7K3A.e2U8vJz.9G3E7x.xY4zY6y8w7x9v1e', 4, 'ACTIVE'),
(6, 'giang.td@gmail.com', '$2a$10$e8pA70W0p8V5Qn7zI7K3A.e2U8vJz.9G3E7x.xY4zY6y8w7x9v1e', 4, 'ACTIVE'),
(7, 'duong.dh@gmail.com', '$2a$10$e8pA70W0p8V5Qn7zI7K3A.e2U8vJz.9G3E7x.xY4zY6y8w7x9v1e', 4, 'ACTIVE'),
(8, 'han.dd@gmail.com', '$2a$10$e8pA70W0p8V5Qn7zI7K3A.e2U8vJz.9G3E7x.xY4zY6y8w7x9v1e', 4, 'ACTIVE'),
(9, 'huan.bn@gmail.com', '$2a$10$e8pA70W0p8V5Qn7zI7K3A.e2U8vJz.9G3E7x.xY4zY6y8w7x9v1e', 4, 'ACTIVE'),
(10, 'hai.nh@gmail.com', '$2a$10$e8pA70W0p8V5Qn7zI7K3A.e2U8vJz.9G3E7x.xY4zY6y8w7x9v1e', 4, 'ACTIVE'),
(11, 'huong.ph@gmail.com', '$2a$10$e8pA70W0p8V5Qn7zI7K3A.e2U8vJz.9G3E7x.xY4zY6y8w7x9v1e', 4, 'ACTIVE'),
(12, 'giang.nt@gmail.com', '$2a$10$e8pA70W0p8V5Qn7zI7K3A.e2U8vJz.9G3E7x.xY4zY6y8w7x9v1e', 4, 'ACTIVE')
ON DUPLICATE KEY UPDATE `email` = VALUES(`email`), `role_id` = VALUES(`role_id`);

-- 3. Insert Mentor Profiles
INSERT INTO `mentors` (`id`, `user_id`, `full_name`, `phone_number`, `department`, `specialization`) VALUES
(1, 2, 'Nguyễn Khánh Tùng', '0912345678', 'Product Management', 'Product Owner & Agile Coach')
ON DUPLICATE KEY UPDATE `full_name` = VALUES(`full_name`), `department` = VALUES(`department`);

-- 4. Insert Student Profiles (Exactly 10 Group Members)
INSERT INTO `students` (`id`, `user_id`, `student_code`, `full_name`, `phone_number`, `university`, `major`, `mentor_id`) VALUES
(1, 3, 'SV2026001', 'Nguyễn Thành Hưng', '0987654321', 'ICTU', 'Software Engineering', 1),
(2, 4, 'SV2026002', 'Dương Minh Hưng', '0987654322', 'ICTU', 'Software Engineering', 1),
(3, 5, 'SV2026003', 'Hoàng Thanh Hùng', '0987654323', 'ICTU', 'Software Engineering', 1),
(4, 6, 'SV2026004', 'Trương Đình Giang', '0987654324', 'ICTU', 'Software Engineering', 1),
(5, 7, 'SV2026005', 'Dương Hải Dương', '0987654325', 'ICTU', 'Software Engineering', 1),
(6, 8, 'SV2026006', 'Diệp Đình Hân', '0987654326', 'ICTU', 'Software Engineering', 1),
(7, 9, 'SV2026007', 'Bùi Ngọc Huan', '0987654327', 'ICTU', 'Software Engineering', 1),
(8, 10, 'SV2026008', 'Nguyễn Hồng Hải', '0987654328', 'ICTU', 'Software Engineering', 1),
(9, 11, 'SV2026009', 'Phạm Hải Hướng', '0987654329', 'ICTU', 'Software Engineering', 1),
(10, 12, 'SV2026010', 'Nguyễn Thị Giang', '0987654330', 'ICTU', 'Software Engineering', 1)
ON DUPLICATE KEY UPDATE 
    `full_name` = VALUES(`full_name`),
    `student_code` = VALUES(`student_code`),
    `university` = VALUES(`university`),
    `major` = VALUES(`major`);