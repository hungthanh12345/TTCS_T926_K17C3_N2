-- Part 4 (US06): add registration states to the existing users.status column.
-- Existing ACTIVE / INACTIVE / LOCKED values and the ACTIVE default are preserved.
ALTER TABLE `users`
    MODIFY COLUMN `status`
        ENUM('ACTIVE', 'INACTIVE', 'LOCKED', 'PENDING_APPROVAL', 'REJECTED')
        NOT NULL DEFAULT 'ACTIVE';
