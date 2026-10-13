-- Rehearsal-only delta for an existing US-09 table on internship_restore_check.
-- Preflight first: verify the exact schema, table, FK name, and current CASCADE rule
-- through INFORMATION_SCHEMA; verify the migration account is restricted to this schema.
-- This one atomic ALTER replaces only the Student FK delete rule. It changes no rows,
-- does not drop a table, and must not be redirected to internship_management.
ALTER TABLE `internship_restore_check`.`internship_contracts`
    DROP FOREIGN KEY `fk_internship_contracts_student`,
    ADD CONSTRAINT `fk_internship_contracts_student`
        FOREIGN KEY (`student_id`)
        REFERENCES `internship_restore_check`.`students` (`id`)
        ON UPDATE CASCADE
        ON DELETE RESTRICT;