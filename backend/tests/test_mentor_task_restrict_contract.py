from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
MIGRATION_ID = "20261017091954_RestrictMentorTaskDelete"
FK_NAME = "fk_sprint2_internship_tasks_mentor"

class MentorTaskRestrictContractTests(unittest.TestCase):
    def test_context_restricts_only_the_mentor_task_relationship(self):
        context = (ROOT / "backend/Data/AppDbContext.cs").read_text(encoding="utf-8")
        block = re.search(r"entity\.HasOne\(task => task\.Mentor\)(.*?)entity\.HasOne\(task => task\.Student\)", context, re.S)
        self.assertIsNotNone(block)
        self.assertIn("OnDelete(DeleteBehavior.Restrict)", block.group(1))
        self.assertIn(f'HasConstraintName("{FK_NAME}")', block.group(1))

    def test_historical_bootstrap_is_preserved_and_forward_sql_is_restrict(self):
        historical = (ROOT / "backend/migrations/20261003_part05_mentor_task_assignment.sql").read_text(encoding="utf-8")
        forward = (ROOT / f"backend/migrations/{MIGRATION_ID}.sql").read_text(encoding="utf-8")
        self.assertRegex(historical, r"ON UPDATE CASCADE ON DELETE CASCADE")
        self.assertIn("ON UPDATE CASCADE ON DELETE RESTRICT", forward)
        self.assertIn("ALTER TABLE `internship_tasks`", forward)
        self.assertNotRegex(forward, r"(?i)\b(DELETE\s+FROM|UPDATE\s+\w+\s+SET|DROP\s+TABLE)\b")
        self.assertNotIn("__EFMigrationsHistory", forward)

    def test_mentor_delete_service_blocks_tasks_and_review_history(self):
        service = (ROOT / "backend/Services/MentorService.cs").read_text(encoding="utf-8")
        repository = (ROOT / "backend/Repositories/MentorRepository.cs").read_text(encoding="utf-8")
        delete_block = re.search(r"public async Task DeleteMentorAsync\(int id\)(.*?)\n        }", service, re.S)
        self.assertIsNotNone(delete_block)
        self.assertLess(delete_block.group(1).index("HasWorkReferencesAsync"), delete_block.group(1).index("DeleteAsync"))
        self.assertIn("_context.Tasks.AnyAsync(task => task.MentorId == mentorId)", repository)
        self.assertIn("_context.MentorFeedbacks.AnyAsync(feedback => feedback.MentorId == mentorId)", repository)

    def test_ef_snapshot_and_migration_match_restrict_policy(self):
        snapshot = (ROOT / "backend/migrations/AppDbContextModelSnapshot.cs").read_text(encoding="utf-8")
        migration = (ROOT / f"backend/migrations/{MIGRATION_ID}.cs").read_text(encoding="utf-8")
        task_blocks = list(re.finditer(r'modelBuilder\.Entity\("InternshipManagementApi\.Data\.Entities\.InternshipTask", b =>', snapshot))
        self.assertGreaterEqual(len(task_blocks), 2)
        relationship_start = task_blocks[-1].start()
        relationship_end = snapshot.find("modelBuilder.Entity(", relationship_start + 1)
        task_relationship = snapshot[relationship_start:relationship_end]
        mentor_relationship = re.search(r'HasOne\("InternshipManagementApi\.Data\.Entities\.Mentor", "Mentor"\)(.*?)HasOne\("InternshipManagementApi\.Data\.Entities\.Student", "Student"\)', task_relationship, re.S)
        self.assertIsNotNone(mentor_relationship)
        self.assertIn("OnDelete(DeleteBehavior.Restrict)", mentor_relationship.group(1))
        self.assertIn(f'.HasConstraintName("{FK_NAME}")', mentor_relationship.group(1))
        self.assertIn("onDelete: ReferentialAction.Restrict", migration)
        self.assertIn("onUpdate: ReferentialAction.Cascade", migration)
        self.assertIn("NotSupportedException", migration)
        self.assertNotIn("20261016040114_AddProgramParticipationAuditAndPendingApplicationDefault", migration)

    def test_ef_deployment_script_contains_only_fk_change_and_normal_history_insert(self):
        script = (ROOT / f"backend/deployment/{MIGRATION_ID}.sql").read_text(encoding="utf-8")
        self.assertIn(f"DROP FOREIGN KEY `{FK_NAME}`", script)
        self.assertIn(f"ADD CONSTRAINT `{FK_NAME}`", script)
        self.assertIn("ON UPDATE CASCADE ON DELETE RESTRICT", script)
        self.assertIn(f"VALUES ('{MIGRATION_ID}', '8.0.2')", script)
        self.assertEqual(len(re.findall(r"(?i)ALTER TABLE `internship_tasks`", script)), 2)
        self.assertNotRegex(script, r"(?i)\b(DELETE\s+FROM|UPDATE\s+\w+\s+SET|DROP\s+TABLE|CREATE\s+TABLE)\b")

if __name__ == "__main__":
    unittest.main()
