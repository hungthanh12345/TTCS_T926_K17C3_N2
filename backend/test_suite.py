"""Read-only API smoke checks; credentials and target are provided via environment."""

import json
import os
import sys
from datetime import date, timedelta
import urllib.error
import urllib.parse
import urllib.request


BASE_URL = os.getenv("VALIDATION_API_BASE_URL", "http://localhost:5000").rstrip("/")
ADMIN_EMAIL = os.getenv("VALIDATION_ADMIN_EMAIL")
ADMIN_PASSWORD = os.getenv("VALIDATION_ADMIN_PASSWORD")
HR_EMAIL = os.getenv("VALIDATION_HR_EMAIL")
HR_PASSWORD = os.getenv("VALIDATION_HR_PASSWORD")
STUDENT_EMAIL = os.getenv("VALIDATION_STUDENT_EMAIL")
STUDENT_PASSWORD = os.getenv("VALIDATION_STUDENT_PASSWORD")
OTHER_STUDENT_EMAIL = os.getenv("VALIDATION_OTHER_STUDENT_EMAIL")
OTHER_STUDENT_PASSWORD = os.getenv("VALIDATION_OTHER_STUDENT_PASSWORD")
WEEKLY_REPORT_TESTS = os.getenv("VALIDATION_WEEKLY_REPORT_TESTS") == "1"
WEEKLY_REPORT_TEST_DATABASE = os.getenv("VALIDATION_TEST_DATABASE") == "1"
REPORT_STUDENT_A_EMAIL = os.getenv("VALIDATION_REPORT_STUDENT_A_EMAIL")
REPORT_STUDENT_A_PASSWORD = os.getenv("VALIDATION_REPORT_STUDENT_A_PASSWORD")
REPORT_STUDENT_B_EMAIL = os.getenv("VALIDATION_REPORT_STUDENT_B_EMAIL")
REPORT_STUDENT_B_PASSWORD = os.getenv("VALIDATION_REPORT_STUDENT_B_PASSWORD")
REPORT_MENTOR_A_EMAIL = os.getenv("VALIDATION_REPORT_MENTOR_A_EMAIL")
REPORT_MENTOR_A_PASSWORD = os.getenv("VALIDATION_REPORT_MENTOR_A_PASSWORD")
REPORT_MENTOR_B_EMAIL = os.getenv("VALIDATION_REPORT_MENTOR_B_EMAIL")
REPORT_MENTOR_B_PASSWORD = os.getenv("VALIDATION_REPORT_MENTOR_B_PASSWORD")
PART09_TESTS = os.getenv("VALIDATION_PART09_TESTS") == "1"
EVALUATION_STUDENT_A_EMAIL = os.getenv("VALIDATION_EVALUATION_STUDENT_A_EMAIL")
EVALUATION_STUDENT_A_PASSWORD = os.getenv("VALIDATION_EVALUATION_STUDENT_A_PASSWORD")
EVALUATION_STUDENT_B_EMAIL = os.getenv("VALIDATION_EVALUATION_STUDENT_B_EMAIL")
EVALUATION_STUDENT_B_PASSWORD = os.getenv("VALIDATION_EVALUATION_STUDENT_B_PASSWORD")
EVALUATION_MENTOR_A_EMAIL = os.getenv("VALIDATION_EVALUATION_MENTOR_A_EMAIL")
EVALUATION_MENTOR_A_PASSWORD = os.getenv("VALIDATION_EVALUATION_MENTOR_A_PASSWORD")
EVALUATION_MENTOR_B_EMAIL = os.getenv("VALIDATION_EVALUATION_MENTOR_B_EMAIL")
EVALUATION_MENTOR_B_PASSWORD = os.getenv("VALIDATION_EVALUATION_MENTOR_B_PASSWORD")


def request(method, path, body=None, token=None):
    headers = {"Content-Type": "application/json", "Accept": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"

    data = json.dumps(body).encode("utf-8") if body is not None else None
    req = urllib.request.Request(f"{BASE_URL}{path}", data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(req, timeout=10) as response:
            content = response.read().decode("utf-8")
            return response.status, json.loads(content) if content else {}
    except urllib.error.HTTPError as error:
        content = error.read().decode("utf-8")
        try:
            parsed = json.loads(content)
        except json.JSONDecodeError:
            parsed = {"raw": content}
        return error.code, parsed


def check(condition, description):
    if not condition:
        raise AssertionError(description)
    print(f"[PASS] {description}")


def login(email, password, expected_role):
    status, response = request("POST", "/api/auth/login", {"email": email, "password": password})
    check(status == 200, f"{expected_role} account can log in (HTTP {status}).")
    data = response.get("data", {})
    check(data.get("user", {}).get("role") == expected_role, f"Login returns the {expected_role} role.")
    token = data.get("token")
    check(isinstance(token, str) and bool(token), "Login response contains a Bearer token.")
    return token


def run_student_schedule_tests():
    if not STUDENT_EMAIL or not STUDENT_PASSWORD:
        print("[SKIP] US14 API authorization checks: set VALIDATION_STUDENT_EMAIL and VALIDATION_STUDENT_PASSWORD.")
        return

    status, _ = request("GET", "/api/student/schedule")
    check(status == 401, f"Anonymous users cannot view the student schedule (HTTP {status}).")

    student_token = login(STUDENT_EMAIL, STUDENT_PASSWORD, "ROLE_STUDENT")
    status, response = request("GET", "/api/student/schedule", token=student_token)
    check(status == 200 and isinstance(response.get("data"), list),
          f"A student can view their own schedule (HTTP {status}).")

    schedule = response["data"]
    required_fields = {"taskId", "title", "dueDate", "status"}
    for event in schedule:
        check(required_fields.issubset(event), "Schedule events include task, date, and progress fields.")
    task_ids = {event["taskId"] for event in schedule}

    status, _ = request("GET", "/api/student/schedule/1", token=student_token)
    check(status == 404, f"The personal schedule API does not accept another student's id (HTTP {status}).")

    if OTHER_STUDENT_EMAIL and OTHER_STUDENT_PASSWORD:
        other_token = login(OTHER_STUDENT_EMAIL, OTHER_STUDENT_PASSWORD, "ROLE_STUDENT")
        status, other_response = request("GET", "/api/student/schedule", token=other_token)
        check(status == 200 and isinstance(other_response.get("data"), list),
              f"A second student can view their own schedule (HTTP {status}).")
        other_task_ids = {event["taskId"] for event in other_response["data"]}
        check(task_ids.isdisjoint(other_task_ids), "Students cannot see tasks assigned to another student.")
    else:
        print("[SKIP] US14 cross-account data check: set VALIDATION_OTHER_STUDENT_EMAIL and VALIDATION_OTHER_STUDENT_PASSWORD.")


def run_weekly_report_ownership_tests():
    if not WEEKLY_REPORT_TESTS:
        print("[SKIP] US17/US18 mutating API checks: set VALIDATION_WEEKLY_REPORT_TESTS=1 to opt in against an isolated test database.")
        return

    if not WEEKLY_REPORT_TEST_DATABASE:
        raise AssertionError("US17/US18 checks mutate data; set VALIDATION_TEST_DATABASE=1 only when the API uses a disposable test database.")

    parsed_url = urllib.parse.urlparse(BASE_URL)
    if parsed_url.hostname not in {"localhost", "127.0.0.1", "::1"}:
        raise AssertionError("US17/US18 mutating API checks are restricted to a loopback API host.")

    credentials = (
        REPORT_STUDENT_A_EMAIL, REPORT_STUDENT_A_PASSWORD,
        REPORT_STUDENT_B_EMAIL, REPORT_STUDENT_B_PASSWORD,
        REPORT_MENTOR_A_EMAIL, REPORT_MENTOR_A_PASSWORD,
        REPORT_MENTOR_B_EMAIL, REPORT_MENTOR_B_PASSWORD,
    )
    if not all(credentials):
        raise AssertionError("US17/US18 checks need two student and two mentor test accounts assigned across the two mentors.")

    def available_monday(token):
        status, response = request("GET", "/api/student/weekly-reports", token=token)
        check(status == 200 and isinstance(response.get("data"), list),
              f"Student can list their reports before the test (HTTP {status}).")
        used_dates = {item.get("weekStartDate") for item in response["data"]}
        today = date.today()
        this_monday = today - timedelta(days=today.weekday())
        for weeks_ago in range(52, 365):
            candidate = this_monday - timedelta(weeks=weeks_ago)
            if candidate.isoformat() not in used_dates:
                return candidate
        raise AssertionError("No unused historical week was available for the isolated test report.")

    def report_payload(week_start, suffix):
        return {
            "weekStartDate": week_start.isoformat(),
            "workSummary": f"Weekly report ownership test {suffix}",
            "results": "Integration test result",
            "challenges": "No test blockers",
            "nextWeekPlan": "Continue isolated integration checks",
        }

    status, _ = request("GET", "/api/student/weekly-reports")
    check(status == 401, f"Anonymous users cannot list weekly reports (HTTP {status}).")
    status, _ = request("GET", "/api/mentor/weekly-reports")
    check(status == 401, f"Anonymous users cannot list mentor reports (HTTP {status}).")

    student_a = login(REPORT_STUDENT_A_EMAIL, REPORT_STUDENT_A_PASSWORD, "ROLE_STUDENT")
    student_b = login(REPORT_STUDENT_B_EMAIL, REPORT_STUDENT_B_PASSWORD, "ROLE_STUDENT")
    mentor_a = login(REPORT_MENTOR_A_EMAIL, REPORT_MENTOR_A_PASSWORD, "ROLE_MENTOR")
    mentor_b = login(REPORT_MENTOR_B_EMAIL, REPORT_MENTOR_B_PASSWORD, "ROLE_MENTOR")

    week_a = available_monday(student_a)
    invalid_payload = report_payload(week_a + timedelta(days=1), "invalid-date")
    status, _ = request("POST", "/api/student/weekly-reports", invalid_payload, token=student_a)
    check(status == 400, f"Non-Monday report dates are rejected (HTTP {status}).")

    status, created_a = request("POST", "/api/student/weekly-reports", report_payload(week_a, "student-a"), token=student_a)
    check(status == 201 and isinstance(created_a.get("data", {}).get("id"), int),
          f"Student A can submit their own report (HTTP {status}).")
    report_a_id = created_a["data"]["id"]
    status, _ = request("POST", "/api/student/weekly-reports", report_payload(week_a, "duplicate"), token=student_a)
    check(status == 409, f"A student cannot submit two reports for one week (HTTP {status}).")
    status, _ = request("PUT", f"/api/student/weekly-reports/{report_a_id}", report_payload(week_a, "student-a-updated"), token=student_a)
    check(status == 200, f"Student A can edit their own unreviewed report (HTTP {status}).")

    week_b = available_monday(student_b)
    status, created_b = request("POST", "/api/student/weekly-reports", report_payload(week_b, "student-b"), token=student_b)
    check(status == 201 and isinstance(created_b.get("data", {}).get("id"), int),
          f"Student B can submit their own report (HTTP {status}).")
    report_b_id = created_b["data"]["id"]
    status, _ = request("GET", f"/api/student/weekly-reports/{report_b_id}", token=student_a)
    check(status == 404, f"Student A cannot view Student B's report (HTTP {status}).")
    status, _ = request("PUT", f"/api/student/weekly-reports/{report_b_id}", report_payload(week_b, "forged-update"), token=student_a)
    check(status == 404, f"Student A cannot edit Student B's report (HTTP {status}).")
    status, student_b_reports = request("GET", "/api/student/weekly-reports", token=student_b)
    check(status == 200 and report_a_id not in {item.get("id") for item in student_b_reports.get("data", [])},
          "Student B's report list does not contain Student A's report.")

    status, mentor_a_reports = request("GET", "/api/mentor/weekly-reports", token=mentor_a)
    check(status == 200 and report_a_id in {item.get("id") for item in mentor_a_reports.get("data", [])},
          "Mentor A can list reports for their assigned student.")
    status, mentor_a_detail = request("GET", f"/api/mentor/weekly-reports/{report_a_id}", token=mentor_a)
    check(status == 200 and mentor_a_detail.get("data", {}).get("id") == report_a_id,
          "Mentor A can view the assigned student's report detail.")
    status, mentor_b_reports = request("GET", "/api/mentor/weekly-reports", token=mentor_b)
    check(status == 200 and report_b_id in {item.get("id") for item in mentor_b_reports.get("data", [])},
          "Mentor B can list reports for their assigned student.")
    status, _ = request("GET", f"/api/mentor/weekly-reports/{report_b_id}", token=mentor_a)
    check(status == 404, f"Mentor A cannot view Student B's report assigned to Mentor B (HTTP {status}).")
    status, _ = request("POST", f"/api/mentor/weekly-reports/{report_b_id}/feedback", {"content": "Unauthorized mentor test"}, token=mentor_a)
    check(status == 404, f"Mentor A cannot give feedback on Student B's report (HTTP {status}).")

    status, _ = request("POST", f"/api/mentor/weekly-reports/{report_a_id}/feedback", {"content": "Student cannot impersonate a mentor"}, token=student_a)
    check(status == 403, f"Student role cannot submit mentor feedback (HTTP {status}).")
    status, feedback = request("POST", f"/api/mentor/weekly-reports/{report_a_id}/feedback", {"content": "Good weekly progress; keep documenting results."}, token=mentor_a)
    check(status == 201 and isinstance(feedback.get("data", {}).get("id"), int),
          f"Mentor A can review their assigned student's report (HTTP {status}).")
    status, _ = request("POST", f"/api/mentor/weekly-reports/{report_a_id}/feedback", {"content": "Duplicate feedback"}, token=mentor_a)
    check(status == 409, f"A report cannot receive duplicate feedback (HTTP {status}).")
    status, updated_feedback = request("PUT", f"/api/mentor/weekly-reports/{report_a_id}/feedback", {"content": "Updated mentor feedback for ownership test."}, token=mentor_a)
    check(status == 200 and updated_feedback.get("data", {}).get("content") == "Updated mentor feedback for ownership test.",
          f"The authoring mentor can update feedback (HTTP {status}).")
    status, _ = request("PUT", f"/api/mentor/weekly-reports/{report_a_id}/feedback", {"content": "Mentor B cannot update this."}, token=mentor_b)
    check(status == 404, f"Mentor B cannot access or update Mentor A's assigned report (HTTP {status}).")

    status, student_a_detail = request("GET", f"/api/student/weekly-reports/{report_a_id}", token=student_a)
    report_data = student_a_detail.get("data", {})
    check(status == 200 and report_data.get("status") == "REVIEWED" and
          report_data.get("mentorFeedback", {}).get("content") == "Updated mentor feedback for ownership test.",
          "Student A can read mentor feedback on their own report after it is saved.")
    status, _ = request("PUT", f"/api/student/weekly-reports/{report_a_id}", report_payload(week_a, "edit-after-review"), token=student_a)
    check(status == 409, f"Student A cannot edit a report after mentor review (HTTP {status}).")
    status, _ = request("GET", "/api/student/weekly-reports", token=mentor_a)
    check(status == 403, f"Mentor role cannot access student-only report routes (HTTP {status}).")
    print("[PASS] US17/US18 authenticated CRUD, role, feedback, validation, and cross-owner API checks passed.")


def run_part09_tests():
    if not PART09_TESTS:
        print("[SKIP] US19/US20 mutating API checks: set VALIDATION_PART09_TESTS=1 to opt in against a fresh isolated test database.")
        return

    if not WEEKLY_REPORT_TEST_DATABASE:
        raise AssertionError("US19/US20 tests write evaluations; set VALIDATION_TEST_DATABASE=1 only when the API uses a disposable test database.")

    parsed_url = urllib.parse.urlparse(BASE_URL)
    if parsed_url.hostname not in {"localhost", "127.0.0.1", "::1"}:
        raise AssertionError("US19/US20 mutating API checks are restricted to a loopback API host.")

    credentials = (
        EVALUATION_STUDENT_A_EMAIL, EVALUATION_STUDENT_A_PASSWORD,
        EVALUATION_STUDENT_B_EMAIL, EVALUATION_STUDENT_B_PASSWORD,
        EVALUATION_MENTOR_A_EMAIL, EVALUATION_MENTOR_A_PASSWORD,
        EVALUATION_MENTOR_B_EMAIL, EVALUATION_MENTOR_B_PASSWORD,
        HR_EMAIL, HR_PASSWORD,
    )
    if not all(credentials):
        raise AssertionError("US19/US20 checks need two student, two mentor, and one HR test account, with students assigned to different mentors.")

    def raw_request(method, path, payload, token=None):
        headers = {"Content-Type": "application/json", "Accept": "application/json"}
        if token:
            headers["Authorization"] = f"Bearer {token}"
        req = urllib.request.Request(f"{BASE_URL}{path}", data=payload, headers=headers, method=method)
        try:
            with urllib.request.urlopen(req, timeout=10) as response:
                return response.status, response.read().decode("utf-8")
        except urllib.error.HTTPError as error:
            return error.code, error.read().decode("utf-8")

    def evaluation_payload(skills=8, attitude=9, comments="Part 09 integration evaluation."):
        return {"skillsScore": skills, "attitudeScore": attitude, "comments": comments}

    status, _ = request("GET", "/api/mentor/evaluations")
    check(status == 401, f"Anonymous users cannot access mentor evaluations (HTTP {status}).")
    status, _ = request("GET", "/api/student/evaluations")
    check(status == 401, f"Anonymous users cannot view evaluations (HTTP {status}).")
    status, _ = request("GET", "/api/hr/internship-summary")
    check(status == 401, f"Anonymous users cannot view the HR summary (HTTP {status}).")

    student_a = login(EVALUATION_STUDENT_A_EMAIL, EVALUATION_STUDENT_A_PASSWORD, "ROLE_STUDENT")
    student_b = login(EVALUATION_STUDENT_B_EMAIL, EVALUATION_STUDENT_B_PASSWORD, "ROLE_STUDENT")
    mentor_a = login(EVALUATION_MENTOR_A_EMAIL, EVALUATION_MENTOR_A_PASSWORD, "ROLE_MENTOR")
    mentor_b = login(EVALUATION_MENTOR_B_EMAIL, EVALUATION_MENTOR_B_PASSWORD, "ROLE_MENTOR")
    hr_token = login(HR_EMAIL, HR_PASSWORD, "ROLE_HR")

    status, profile_a_response = request("GET", "/api/student/profile", token=student_a)
    profile_a = profile_a_response.get("data", {})
    check(status == 200 and isinstance(profile_a.get("id"), int) and profile_a.get("mentorId") is not None,
          "Student A profile belongs to an assigned mentor.")
    status, profile_b_response = request("GET", "/api/student/profile", token=student_b)
    profile_b = profile_b_response.get("data", {})
    check(status == 200 and isinstance(profile_b.get("id"), int) and profile_b.get("mentorId") is not None and
          profile_b.get("mentorId") != profile_a.get("mentorId"),
          "Student B is assigned to a different mentor for cross-owner checks.")

    status, mentor_a_students = request("GET", "/api/mentor/evaluations", token=mentor_a)
    mentor_a_items = mentor_a_students.get("data", [])
    student_a_item = next((item for item in mentor_a_items if item.get("studentId") == profile_a["id"]), None)
    check(status == 200 and student_a_item is not None,
          "Mentor A can list their assigned student for evaluation.")
    status, mentor_b_students = request("GET", "/api/mentor/evaluations", token=mentor_b)
    mentor_b_items = mentor_b_students.get("data", [])
    student_b_item = next((item for item in mentor_b_items if item.get("studentId") == profile_b["id"]), None)
    check(status == 200 and student_b_item is not None,
          "Mentor B can list their assigned student for evaluation.")
    check(student_a_item.get("evaluation") is None and student_b_item.get("evaluation") is None,
          "The isolated test students have no prior final evaluation.")

    status, _ = request("POST", f"/api/mentor/evaluations/students/{profile_a['id']}", evaluation_payload(), token=student_a)
    check(status == 403, f"Student role cannot create an evaluation (HTTP {status}).")
    status, _ = request("POST", f"/api/mentor/evaluations/students/{profile_a['id']}", evaluation_payload(skills=0), token=mentor_a)
    check(status == 400, f"Evaluation rejects a score below 1 (HTTP {status}).")
    status, _ = request("POST", f"/api/mentor/evaluations/students/{profile_a['id']}", evaluation_payload(attitude=11), token=mentor_a)
    check(status == 400, f"Evaluation rejects a score above 10 (HTTP {status}).")
    status, _ = request("POST", f"/api/mentor/evaluations/students/{profile_a['id']}", {"skillsScore": 8, "attitudeScore": 9}, token=mentor_a)
    check(status == 400, f"Evaluation requires a comment (HTTP {status}).")
    status, _ = raw_request("POST", f"/api/mentor/evaluations/students/{profile_a['id']}", b'{"skillsScore":', mentor_a)
    check(status == 400, f"Malformed evaluation JSON is rejected (HTTP {status}).")
    status, _ = request("POST", "/api/mentor/evaluations/students/2147483000", evaluation_payload(), token=mentor_a)
    check(status == 404, f"Evaluation rejects a non-existing student (HTTP {status}).")
    status, _ = request("POST", f"/api/mentor/evaluations/students/{profile_b['id']}", evaluation_payload(), token=mentor_a)
    check(status == 404, f"Mentor A cannot evaluate a student assigned to Mentor B (HTTP {status}).")

    status, created_a = request("POST", f"/api/mentor/evaluations/students/{profile_a['id']}", evaluation_payload(), token=mentor_a)
    check(status == 201 and isinstance(created_a.get("data", {}).get("id"), int),
          f"Mentor A can evaluate their assigned student (HTTP {status}).")
    evaluation_a_id = created_a["data"]["id"]
    status, _ = request("POST", f"/api/mentor/evaluations/students/{profile_a['id']}", evaluation_payload(), token=mentor_a)
    check(status == 409, f"Duplicate evaluation for a student is rejected (HTTP {status}).")
    status, updated_a = request("PUT", f"/api/mentor/evaluations/{evaluation_a_id}", evaluation_payload(9, 9, "Updated evaluation after review."), token=mentor_a)
    check(status == 200 and updated_a.get("data", {}).get("overallScore") == 9,
          f"The authoring mentor can update their evaluation (HTTP {status}).")
    status, _ = request("GET", "/api/mentor/evaluations/2147483000", token=mentor_a)
    check(status == 404, f"A non-existing evaluation returns not found (HTTP {status}).")
    status, _ = request("GET", f"/api/mentor/evaluations/{evaluation_a_id}", token=mentor_b)
    check(status == 404, f"Mentor B cannot view Mentor A's evaluation (HTTP {status}).")
    status, _ = request("PUT", f"/api/mentor/evaluations/{evaluation_a_id}", evaluation_payload(), token=mentor_b)
    check(status == 404, f"Mentor B cannot update Mentor A's evaluation (HTTP {status}).")
    status, _ = request("PUT", f"/api/mentor/evaluations/{evaluation_a_id}", evaluation_payload(), token=student_a)
    check(status == 403, f"Student cannot modify an evaluation (HTTP {status}).")
    status, own_evaluations = request("GET", "/api/student/evaluations", token=student_a)
    check(status == 200 and evaluation_a_id in {item.get("id") for item in own_evaluations.get("data", [])},
          "Student A can read their own evaluation.")
    status, _ = request("GET", f"/api/student/evaluations/{evaluation_a_id}", token=student_b)
    check(status == 404, f"Student B cannot view Student A's evaluation (HTTP {status}).")

    status, _ = request("GET", "/api/hr/internship-summary", token=mentor_a)
    check(status == 403, f"Mentor role cannot view the HR summary (HTTP {status}).")
    status, _ = request("GET", "/api/hr/internship-summary", token=student_a)
    check(status == 403, f"Student role cannot view the HR summary (HTTP {status}).")
    status, summary_response = request("GET", "/api/hr/internship-summary", token=hr_token)
    summary = summary_response.get("data", {})
    summary_item = next((item for item in summary.get("items", []) if item.get("studentId") == profile_a["id"]), None)
    check(status == 200 and summary_item is not None and summary_item.get("evaluationStatus") == "EVALUATED" and
          summary_item.get("overallScore") == 9,
          "HR can view an accurate summary including the saved evaluation.")
    if ADMIN_EMAIL and ADMIN_PASSWORD:
        admin_token = login(ADMIN_EMAIL, ADMIN_PASSWORD, "ROLE_ADMIN")
        status, _ = request("GET", "/api/hr/internship-summary", token=admin_token)
        check(status == 200, f"Admin can view the HR summary under the current role policy (HTTP {status}).")
    else:
        print("[SKIP] Admin-to-HR-summary check: set VALIDATION_ADMIN_EMAIL and VALIDATION_ADMIN_PASSWORD.")

    print("[PASS] US19/US20 authenticated evaluation, validation, ownership, and HR summary API checks passed.")


def run_tests():
    run_weekly_report_ownership_tests()
    run_part09_tests()
    if not ADMIN_EMAIL or not ADMIN_PASSWORD:
        print("[SKIP] API smoke checks are read-only and opt-in. Set VALIDATION_ADMIN_EMAIL and VALIDATION_ADMIN_PASSWORD.")
        run_student_schedule_tests()
        return

    parsed_url = urllib.parse.urlparse(BASE_URL)
    check(parsed_url.scheme in {"http", "https"} and bool(parsed_url.netloc), "API base URL is an absolute HTTP(S) URL.")

    admin_token = login(ADMIN_EMAIL, ADMIN_PASSWORD, "ROLE_ADMIN")
    run_student_schedule_tests()

    status, _ = request("POST", "/api/auth/login", {"email": ADMIN_EMAIL, "password": "invalid-test-password"})
    check(status == 401, f"Invalid password is rejected (HTTP {status}).")

    status, response = request("GET", "/api/admin/users")
    check(status == 401, f"Admin endpoint rejects a missing token (HTTP {status}).")

    status, response = request("GET", "/api/admin/users", token=admin_token)
    check(status == 200 and isinstance(response.get("data"), list), f"Admin can read users (HTTP {status}).")

    status, response = request(
        "POST",
        "/api/auth/login",
        {"email": "invalid-email", "password": "short"},
    )
    check(status == 400 and response.get("success") is False, f"Login request validation returns 400 (HTTP {status}).")

    if HR_EMAIL and HR_PASSWORD:
        hr_token = login(HR_EMAIL, HR_PASSWORD, "ROLE_HR")
        status, _ = request("GET", "/api/admin/users", token=hr_token)
        check(status == 403, f"HR is denied admin-only access (HTTP {status}).")
        status, _ = request("GET", "/api/student/schedule", token=hr_token)
        check(status == 403, f"HR is denied student-only schedule access (HTTP {status}).")
    else:
        print("[SKIP] HR-to-admin authorization check: set VALIDATION_HR_EMAIL and VALIDATION_HR_PASSWORD.")

    print("All configured read-only API checks passed.")


if __name__ == "__main__":
    try:
        run_tests()
    except (AssertionError, OSError, ValueError, urllib.error.URLError) as error:
        print(f"[FAIL] {error}", file=sys.stderr)
        sys.exit(1)
