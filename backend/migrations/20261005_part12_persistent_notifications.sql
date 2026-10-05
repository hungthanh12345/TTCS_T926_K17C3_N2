-- Safe additive migration: preserves existing records and adds a per-user notification inbox.
CREATE TABLE IF NOT EXISTS `notifications` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `user_id` INT NOT NULL,
    `source_key` VARCHAR(191) NOT NULL,
    `title` VARCHAR(160) NOT NULL,
    `message` VARCHAR(1000) NOT NULL,
    `route` VARCHAR(255) NOT NULL,
    `created_at` DATETIME(6) NOT NULL,
    `read_at` DATETIME(6) NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_notifications_user_source` (`user_id`, `source_key`),
    KEY `idx_notifications_user_created` (`user_id`, `created_at`),
    CONSTRAINT `fk_notifications_user` FOREIGN KEY (`user_id`)
        REFERENCES `users` (`id`) ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
