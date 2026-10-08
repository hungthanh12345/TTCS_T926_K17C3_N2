-- Part 10: Set the shared development password on every user already present.
-- Schema verified against User/AppDbContext: users.password_hash stores BCrypt hashes.
-- This updates no email, role, profile, or row count and creates/deletes no accounts.
UPDATE `users`
SET `password_hash` = '$2a$11$9qdMiMhsZy239X8zOXcS0O/Iiab9s/GYC.B2Bt2FB5/oMuChTd8ie';
