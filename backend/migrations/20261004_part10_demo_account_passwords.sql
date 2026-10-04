-- Part 10: Set the shared development password on the four existing demo users.
-- This migration only updates matching email/role pairs; it never creates users.
UPDATE `users` AS u
INNER JOIN `roles` AS r ON r.`id` = u.`role_id`
SET u.`password_hash` = CASE u.`email`
    WHEN 'admin@gmail.com' THEN '$2a$11$DmQNujx.XQHrkXbxyuItV.e8AlqCxjyR7zyd/5w.YGLubrmlu2giC'
    WHEN 'customer.hr@company.com' THEN '$2a$11$D/DCgZpscmOXOipgXrjScuu/Shcrl7XLWLShcwhlbyp8I/bJnmiqy'
    WHEN 'tung.nk@gmail.com' THEN '$2a$11$eOM5Tf4R2rsxO3dgzmNg7eUfBEyEJv0xvL3LZ.5ZGDJhIPi4swrmi'
    WHEN 'hung.nt@gmail.com' THEN '$2a$11$BcoAzdN.rmC0yJltmOURi.b0CCISr0TeQrJ8at4UhP4y53LJlkjQK'
END
WHERE (u.`email` = 'admin@gmail.com' AND r.`name` = 'ROLE_ADMIN')
   OR (u.`email` = 'customer.hr@company.com' AND r.`name` = 'ROLE_HR')
   OR (u.`email` = 'tung.nk@gmail.com' AND r.`name` = 'ROLE_MENTOR')
   OR (u.`email` = 'hung.nt@gmail.com' AND r.`name` = 'ROLE_STUDENT');
