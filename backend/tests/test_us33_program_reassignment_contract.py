from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[1]


class ProgramReassignmentContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.controller = (ROOT / "Controllers" / "InternshipProgramsController.cs").read_text(encoding="utf-8")
        cls.audit_service = (ROOT / "Services" / "ProgramCompletionService.cs").read_text(encoding="utf-8")
        cls.policy = (ROOT / "Services" / "ProgramAssignmentAuditPolicy.cs").read_text(encoding="utf-8")

    def test_assignment_is_protected_by_existing_hr_admin_policy(self):
        self.assertIn('[Authorize(Roles = "ROLE_HR,ROLE_ADMIN")]', self.controller)

    def test_audit_actor_comes_from_authenticated_claims(self):
        self.assertIn('User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId")', self.controller)
        self.assertIn("ResetParticipationForProgramAssignmentAsync(\n                        studentId,\n                        affectedProgramId,\n                        actorUserId,", self.controller)

    def test_update_and_audit_share_row_lock_and_transaction(self):
        method = self.controller.split("AssignStudentProgram(", 1)[1].split("[HttpPost(\"programs\")]")[0]
        self.assertIn("BeginTransactionAsync(\n                IsolationLevel.ReadCommitted", method)
        self.assertIn("FOR UPDATE", method)
        self.assertLess(method.index("ResetParticipationForProgramAssignmentAsync"), method.index("ExecuteUpdateAsync"))
        self.assertIn("await transaction.CommitAsync(cancellationToken);", method)
        self.assertIn("await transaction.RollbackAsync(CancellationToken.None);", method)

    def test_unchanged_program_id_skips_audit_reset(self):
        method = self.controller.split("AssignStudentProgram(", 1)[1].split("[HttpPost(\"programs\")]")[0]
        self.assertLess(method.index("previousProgramId == requestedProgramId"), method.index("GetAffectedProgramIds"))

    def test_failed_conditional_update_rolls_back_appended_audit(self):
        method = self.controller.split("AssignStudentProgram(", 1)[1].split("[HttpPost(\"programs\")]")[0]
        update = method.index("if (changedRows != 1)")
        rollback = method.index("await transaction.RollbackAsync(cancellationToken);", update)
        self.assertLess(method.index("ResetParticipationForProgramAssignmentAsync"), update)
        self.assertLess(update, rollback)

    def test_reset_is_append_only_and_records_revocation(self):
        reset = self.audit_service.split("ResetParticipationForProgramAssignmentAsync(", 1)[1].split("private async Task ChangeCompletionAsync", 1)[0]
        self.assertIn("_db.StudentProgramAuditEvents.Add(NewEvent(", reset)
        self.assertIn("StudentProgramAuditEvent.CompletionRevoked", reset)
        self.assertIn("StudentProgramAuditEvent.ParticipationStatusChanged", reset)
        self.assertNotIn("Remove(", reset)
        self.assertNotIn("ExecuteDelete", reset)

    def test_policy_resets_both_previous_and_target_program_when_changed(self):
        self.assertIn("if (previousProgramId == requestedProgramId)", self.policy)
        self.assertIn("affectedProgramIds.Add(previousProgramId.Value)", self.policy)
        self.assertIn("affectedProgramIds.Add(requestedProgramId.Value)", self.policy)


if __name__ == "__main__":
    unittest.main(verbosity=2)
