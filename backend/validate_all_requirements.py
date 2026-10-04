"""Portable checks for project requirements and selected Sprint 1 regressions.

Database and API checks are opt-in so the script never assumes local credentials
or mutates a developer's database.
"""

import json
import os
import re
import shutil
import subprocess
import sys
import urllib.error
import urllib.request
from pathlib import Path

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

ROOT = Path(__file__).resolve().parents[1]
BACKEND = ROOT / "backend"
FRONTEND = ROOT / "frontend"
API_BASE_URL = os.getenv("VALIDATION_API_BASE_URL", "http://localhost:5000").rstrip("/")


def check(condition, message):
    if not condition:
        raise AssertionError(message)


def request(method, path, body=None, token=None):
    headers = {"Content-Type": "application/json", "Accept": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"

    data = json.dumps(body).encode("utf-8") if body is not None else None
    req = urllib.request.Request(
        f"{API_BASE_URL}{path}", data=data, headers=headers, method=method
    )

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


def get_items(data):
    if isinstance(data, dict):
        return data.get("items", [])
    return data if isinstance(data, list) else []


def validate_connection_charset():
    configured = os.getenv("ConnectionStrings__DefaultConnection") or os.getenv("MYSQL_URL")
    if not configured:
        print("[SKIP] Database connection charset check: no connection string is configured in the environment.")
        return

    if configured.lower().startswith(("mysql://", "mysqls://")):
        resolver = (BACKEND / "Program.cs").read_text(encoding="utf-8")
        check('CharSet=utf8mb4' in resolver, "MySQL URI connections must be normalized to utf8mb4.")
    else:
        check(
            "charset=utf8mb4" in configured.lower() or "char set=utf8mb4" in configured.lower(),
            "Development/database connection configuration must specify utf8mb4.",
        )
    print("[PASS] Configured database connection uses utf8mb4.")


def validate_database_collation():
    mysql_cli = os.getenv("MYSQL_CLI") or shutil.which("mysql")
    defaults_file = os.getenv("MYSQL_DEFAULTS_FILE")
    if not mysql_cli or not defaults_file:
        print("[SKIP] Database collation check: set MYSQL_CLI and MYSQL_DEFAULTS_FILE to opt in.")
        return

    defaults_path = Path(defaults_file).expanduser().resolve()
    check(defaults_path.is_file(), "MYSQL_DEFAULTS_FILE must point to a MySQL client option file.")
    command = [
        mysql_cli,
        f"--defaults-extra-file={defaults_path}",
        "--default-character-set=utf8mb4",
        "--batch",
        "--skip-column-names",
        "-e",
        "SELECT default_character_set_name, default_collation_name "
        "FROM information_schema.SCHEMATA "
        "WHERE schema_name = 'internship_management';",
    ]
    result = subprocess.run(command, capture_output=True, text=True, encoding="utf-8", timeout=15)
    check(result.returncode == 0, "Could not read the configured MySQL schema metadata.")
    check(
        "utf8mb4" in result.stdout and "utf8mb4_unicode_ci" in result.stdout,
        "Database must use utf8mb4 / utf8mb4_unicode_ci.",
    )
    print("[PASS] MySQL database uses utf8mb4 / utf8mb4_unicode_ci.")


def validate_seed_api():
    email = os.getenv("VALIDATION_ADMIN_EMAIL")
    password = os.getenv("VALIDATION_ADMIN_PASSWORD")
    if not email or not password:
        print("[SKIP] API seed checks: set VALIDATION_ADMIN_EMAIL and VALIDATION_ADMIN_PASSWORD to opt in.")
        return

    status, response = request(
        "POST", "/api/auth/login", {"email": email, "password": password}
    )
    check(status == 200, f"Admin login failed with HTTP {status}.")
    admin_token = response["data"]["token"]

    status, response = request("GET", "/api/hr/mentors", token=admin_token)
    check(status == 200, f"Could not retrieve mentors (HTTP {status}).")
    mentors = get_items(response.get("data"))
    check(len(mentors) == 1, f"Expected exactly 1 seeded mentor; found {len(mentors)}.")
    mentor = mentors[0]
    check(mentor.get("fullName") == "Nguyễn Khánh Tùng", "Seed mentor name mismatch.")
    check(mentor.get("department") == "Kỹ thuật phần mềm", "Seed mentor department mismatch.")
    check(
        mentor.get("specialization") == "Full-stack Web và Cloud Native",
        "Seed mentor specialization mismatch.",
    )

    status, response = request("GET", "/api/hr/students", token=admin_token)
    check(status == 200, f"Could not retrieve students (HTTP {status}).")
    students = get_items(response.get("data"))
    check(bool(students), "Expected seeded students in the database.")
    for student in students:
        check(
            student.get("university") == "Đại học Công nghệ Thông tin và Truyền thông — ĐHTN",
            "Seed student university mismatch.",
        )
        check(student.get("major") == "Kỹ thuật Phần mềm", "Seed student major mismatch.")
        visible_text = f"{student.get('fullName', '')} {student.get('university', '')} {student.get('major', '')}"
        check("?" not in visible_text, "Found corrupted Vietnamese text in student data.")
        check(
            (student.get("mentor") or {}).get("fullName") == mentor["fullName"],
            "A seeded student is not assigned to the expected mentor.",
        )

    print(f"[PASS] API seed checks passed for {len(students)} students and 1 mentor.")


def validate_frontend_contracts():
    login_view = (FRONTEND / "src/views/auth/LoginView.jsx").read_text(encoding="utf-8")
    submit_match = re.search(r"const handleSubmit = async \(e\) => \{([\s\S]*?)\n  \};", login_view)
    check(submit_match is not None, "Login form submit handler was not found in LoginView.jsx.")
    submit_body = submit_match.group(1)
    check(
        "if (!validate()) return;" in submit_body and "executeLogin(formData)" in submit_body,
        "Login submission must validate fields before calling the login API.",
    )
    check('<form onSubmit={handleSubmit}' in login_view, "Login form must use its validated submit handler.")
    check('type="email"' in login_view, "Login form must expose an email input.")
    check('type={showPassword ?' in login_view, "Login form must preserve the password visibility control.")
    check("Tài Khoản Demo Nhanh" in login_view, "Login page must render the quick demo-login section.")
    check("const DEMO_PASSWORD = import.meta.env.DEV ? 'Admin@123' : null;" in login_view,
          "The shared demo password must only be present in development mode.")
    check("password: DEMO_PASSWORD" in login_view,
          "Selecting a demo account must fill the shared password into the password field.")
    quick_login_match = re.search(
        r'<section[^>]+aria-labelledby="quick-login-title"[\s\S]*?</section>', login_view
    )
    check(quick_login_match is not None, "Quick demo accounts must be grouped in their own section.")
    if quick_login_match is not None:
        quick_login_section = quick_login_match.group(0)
        check('grid-cols-1' in quick_login_section and 'min-[560px]:grid-cols-2' in quick_login_section,
              "Quick demo account cards must stay responsive on mobile and desktop.")
        check('min-h-[62px]' in quick_login_section,
              "Quick demo account cards must use the compact layout.")
        check('type="button"' in quick_login_section and "executeLogin(" not in quick_login_section and
              "navigate(" not in quick_login_section,
              "Selecting a demo account must not submit login or navigate away.")
    for demo_email in (
        "admin@gmail.com",
        "customer.hr@company.com",
        "tung.nk@gmail.com",
        "hung.nt@gmail.com",
    ):
        check(demo_email in login_view, f"Login demo account {demo_email} must be available as a quick-fill option.")
    for demo_role in ("ROLE_ADMIN", "ROLE_HR", "ROLE_MENTOR", "ROLE_STUDENT"):
        check(demo_role in login_view, f"Login demo role {demo_role} must be visible in its quick-fill option.")
    check("document.getElementById('login-password')?.focus()" in login_view,
          "Selecting a demo account must focus the password input.")
    check("import.meta.env.DEV && (" in login_view,
          "The quick demo-login section must only be available in development mode.")
    check(login_view.count("Admin@123") == 1,
          "The demo password may only appear in the development-only form-fill constant.")

    app_code = (FRONTEND / "src/App.jsx").read_text(encoding="utf-8")
    check(
        '<Route path="/" element={<LandingView />} />' in app_code and
        (FRONTEND / "src/views/common/LandingView.jsx").is_file(),
        "The public landing page must remain available at the root route.",
    )

    auth_context = (FRONTEND / "src/context/AuthContext.jsx").read_text(encoding="utf-8")
    check(
        "const resetSession = useCallback(() => {" in auth_context and "resetSession," in auth_context,
        "AuthContext must expose a stable resetSession callback.",
    )
    check("useState(readInitialSession)" in auth_context, "AuthContext must load stored auth state before the first protected-route render.")
    check("Initialize auth state from this tab's session storage" not in auth_context, "Auth initialization must not require an effect-driven second render.")

    profile_controller = (BACKEND / "Controllers/StudentProfileController.cs").read_text(encoding="utf-8")
    student_repository = (BACKEND / "Repositories/StudentRepository.cs").read_text(encoding="utf-8")
    dashboard = (FRONTEND / "src/views/student/StudentDashboardView.jsx").read_text(encoding="utf-8")
    check('[Route("api/student/profile")]' in profile_controller and '[Authorize(Roles = "ROLE_STUDENT")]' in profile_controller,
          "Student profile API must be restricted to student accounts.")
    check("GetStudentByUserIdAsync(userId)" in profile_controller and "student.UserId == userId" in student_repository,
          "Student profile API must scope the record to the authenticated user ID.")
    check("studentService.getMyProfile()" in dashboard and "allStudents[0]" not in dashboard,
          "Student dashboard must use its own profile endpoint without a cross-account fallback.")

    print("[PASS] Frontend login validation, root route, and session-reset checks passed.")


def validate_dashboard_roles_match_api_policies():
    app_code = (FRONTEND / "src/App.jsx").read_text(encoding="utf-8")
    dashboard_routes = {
        "/mentor/students": ("ROLE_MENTOR", BACKEND / "Controllers/MentorTasksController.cs"),
        "/student/profile": ("ROLE_STUDENT", BACKEND / "Controllers/StudentProfileController.cs"),
    }
    for route_path, (required_role, controller_path) in dashboard_routes.items():
        route_match = re.search(
            rf'path="{re.escape(route_path)}"([\s\S]*?)/>', app_code
        )
        check(route_match is not None, f"Frontend route {route_path} must exist.")
        roles_match = re.search(r"allowedRoles=\{\[([^\]]*)\]\}", route_match.group(1))
        check(roles_match is not None, f"Frontend route {route_path} must declare allowed roles.")
        frontend_roles = set(re.findall(r"'([^']+)'", roles_match.group(1)))
        check(frontend_roles == {required_role},
              f"Frontend route {route_path} must allow only the role authorized by its API.")

        controller = controller_path.read_text(encoding="utf-8")
        check(f'[Authorize(Roles = "{required_role}")]' in controller,
              f"The API for {route_path} must require {required_role}.")
    print("[PASS] Mentor and student dashboard routes match their API role policies.")


def validate_sidebar_roles_match_routes():
    app_code = (FRONTEND / "src/App.jsx").read_text(encoding="utf-8")
    sidebar = (FRONTEND / "src/components/layout/Sidebar.jsx").read_text(encoding="utf-8")
    routes = {}
    for match in re.finditer(r'<Route\s+path="([^"]+)"([\s\S]*?)/>', app_code):
        roles_match = re.search(r"allowedRoles=\{\[([^\]]*)\]\}", match.group(2))
        if roles_match:
            routes[match.group(1)] = set(re.findall(r"'([^']+)'", roles_match.group(1)))

    nav_items = {}
    for match in re.finditer(r"\{\s*title:[\s\S]*?path:\s*'([^']+)'([\s\S]*?)\n\s*\},", sidebar):
        roles_match = re.search(r"roles:\s*\[([^\]]*)\]", match.group(2))
        if roles_match:
            nav_items[match.group(1)] = set(re.findall(r"'([^']+)'", roles_match.group(1)))

    for path, roles in nav_items.items():
        if path in routes:
            check(roles == routes[path],
                  f"Sidebar roles for {path} must match its protected route roles.")
    print("[PASS] Sidebar navigation exposes protected routes only to roles accepted by each route.")


def validate_student_schedule_contract():
    controller = (BACKEND / "Controllers/StudentScheduleController.cs").read_text(encoding="utf-8")
    service = (BACKEND / "Services/StudentScheduleService.cs").read_text(encoding="utf-8")
    app_code = (FRONTEND / "src/App.jsx").read_text(encoding="utf-8")
    sidebar = (FRONTEND / "src/components/layout/Sidebar.jsx").read_text(encoding="utf-8")
    schedule_service = (FRONTEND / "src/services/studentScheduleService.js").read_text(encoding="utf-8")
    page = (FRONTEND / "src/views/student/StudentScheduleView.jsx").read_text(encoding="utf-8")

    check('[Route("api/student/schedule")]' in controller, "US14 must expose its personal schedule API.")
    check('[Authorize(Roles = "ROLE_STUDENT")]' in controller, "US14 API must be limited to student accounts.")
    check("ClaimTypes.NameIdentifier" in controller and 'FindFirstValue("userId")' in controller,
          "US14 must resolve the owner from the authenticated identity.")
    check("task.Student.UserId == userId" in service,
          "US14 schedule queries must be scoped to the authenticated student's user id.")
    check("task.DueDate.HasValue" in service and "OrderBy(task => task.DueDate)" in service,
          "US14 must list dated assignments in chronological order.")
    check("StudentId" not in controller and "studentId" not in controller,
          "US14 API must not accept a client-supplied student id.")

    route_match = re.search(r'path="/student/schedule"([\s\S]*?)/>', app_code)
    check(route_match is not None, "US14 student schedule route must exist.")
    roles_match = re.search(r"allowedRoles=\{\[([^\]]*)\]\}", route_match.group(1))
    check(roles_match is not None and set(re.findall(r"'([^']+)'", roles_match.group(1))) == {"ROLE_STUDENT"},
          "US14 frontend route must allow only ROLE_STUDENT.")
    check("path: '/student/schedule'" in sidebar and "roles: ['ROLE_STUDENT']" in sidebar,
          "US14 navigation must be visible only to students.")
    check("api.get('/student/schedule/overview')" in schedule_service and
          "program" in schedule_service and "events" in schedule_service,
          "US14 frontend must call the real schedule API.")
    check("role=\"status\"" in page and "role=\"alert\"" in page and "events.length === 0" in page,
          "US14 page must render loading, error, and empty states.")
    check("api.post(" not in schedule_service and "api.put(" not in schedule_service and "api.delete(" not in schedule_service,
          "US14 read-only schedule service must not add task mutation actions.")
    print("[PASS] US14 API ownership, student-only access, frontend route, and read-only states are wired.")


def validate_weekly_report_contract():
    student_controller = (BACKEND / "Controllers/StudentWeeklyReportsController.cs").read_text(encoding="utf-8")
    mentor_controller = (BACKEND / "Controllers/MentorWeeklyReportsController.cs").read_text(encoding="utf-8")
    report_service = (BACKEND / "Services/WeeklyReportService.cs").read_text(encoding="utf-8")
    feedback_service = (BACKEND / "Services/MentorFeedbackService.cs").read_text(encoding="utf-8")
    dto = (BACKEND / "DTOs/WeeklyReportDtos.cs").read_text(encoding="utf-8")
    db_context = (BACKEND / "Data/AppDbContext.cs").read_text(encoding="utf-8")
    migration = (BACKEND / "migrations/20261004_part08_weekly_reports_and_mentor_feedback.sql").read_text(encoding="utf-8")
    student_api = (FRONTEND / "src/services/weeklyReportService.js").read_text(encoding="utf-8")
    mentor_api = (FRONTEND / "src/services/mentorWeeklyReportService.js").read_text(encoding="utf-8")
    student_panel = (FRONTEND / "src/components/student/StudentWeeklyReportsPanel.jsx").read_text(encoding="utf-8")
    mentor_panel = (FRONTEND / "src/components/mentor/MentorWeeklyReportsPanel.jsx").read_text(encoding="utf-8")

    check('[Route("api/student/weekly-reports")]' in student_controller and
          '[Authorize(Roles = "ROLE_STUDENT")]' in student_controller,
          "US17 routes must be restricted to authenticated students.")
    check('[Route("api/mentor/weekly-reports")]' in mentor_controller and
          '[Authorize(Roles = "ROLE_MENTOR")]' in mentor_controller,
          "US18 routes must be restricted to authenticated mentors.")
    check("ClaimTypes.NameIdentifier" in student_controller and 'FindFirstValue("userId")' in student_controller and
          "report.StudentId == student.Id" in report_service,
          "Student report reads and writes must derive ownership from the authenticated user.")
    check("report.Student.MentorId == mentor.Id" in report_service and
          "item.Student.MentorId == mentor.Id" in feedback_service and
          "feedback.MentorId != mentor.Id" in feedback_service,
          "Mentor report access and feedback updates must be limited to assigned students and the author.")
    check("WeekStartDate" in dto and "WorkSummary" in dto and "NextWeekPlan" in dto and "StringLength" in dto,
          "Weekly report DTOs must expose the report fields with bounded input validation.")
    check("IsLate = IsLate(report)" in report_service and "AddHours(-7)" in report_service and
          "report.isLate" in student_panel and "report.isLate" in mentor_panel,
          "US17 must expose and render on-time/late submission from the persisted submission timestamp.")
    check("WeeklyReports" in db_context and "MentorFeedbacks" in db_context and
          "IsUnique()" in db_context and "fk_weekly_reports_student" in migration and
          "fk_mentor_feedback_weekly_report" in migration and "fk_mentor_feedback_mentor" in migration and
          "DROP TABLE" not in migration.upper(),
          "Part 08 persistence must enforce unique weeks and additive foreign-key constraints.")
    check("api.get('/student/weekly-reports')" in student_api and "api.post('/student/weekly-reports'" in student_api and
          "api.get('/mentor/weekly-reports')" in mentor_api and "api.post(`/mentor/weekly-reports/${reportId}/feedback`" in mentor_api,
          "US17 and US18 frontend services must call the real backend APIs.")
    student_dashboard = (FRONTEND / "src/views/student/StudentDashboardView.jsx").read_text(encoding="utf-8")
    mentor_dashboard = (FRONTEND / "src/views/mentor/MentorDashboardView.jsx").read_text(encoding="utf-8")
    check('role="status"' in student_panel and 'role="alert"' in student_panel and
          'role="status"' in mentor_panel and 'role="alert"' in mentor_panel and
          "StudentWeeklyReportsPanel" in student_dashboard and "MentorWeeklyReportsPanel" in mentor_dashboard,
          "Student and mentor report panels must be dashboard-integrated with loading, empty, and error states.")
    check("VALIDATION_WEEKLY_REPORT_TESTS" in (BACKEND / "test_suite.py").read_text(encoding="utf-8"),
          "Part 08 authenticated ownership integration checks must be available as an opt-in test.")
    print("[PASS] US17/US18 API roles, ownership, validation, persistence, and frontend integration are wired.")


def validate_part09_contract():
    mentor_controller = (BACKEND / "Controllers/MentorEvaluationsController.cs").read_text(encoding="utf-8")
    student_controller = (BACKEND / "Controllers/StudentEvaluationsController.cs").read_text(encoding="utf-8")
    summary_controller = (BACKEND / "Controllers/HrInternshipSummaryController.cs").read_text(encoding="utf-8")
    evaluation_service = (BACKEND / "Services/InternshipEvaluationService.cs").read_text(encoding="utf-8")
    summary_service = (BACKEND / "Services/HrInternshipSummaryService.cs").read_text(encoding="utf-8")
    evaluation_dto = (BACKEND / "DTOs/InternshipEvaluationDtos.cs").read_text(encoding="utf-8")
    db_context = (BACKEND / "Data/AppDbContext.cs").read_text(encoding="utf-8")
    migration = (BACKEND / "migrations/20261004_part09_internship_evaluations.sql").read_text(encoding="utf-8")
    mentor_api = (FRONTEND / "src/services/internshipEvaluationService.js").read_text(encoding="utf-8")
    hr_api = (FRONTEND / "src/services/hrInternshipSummaryService.js").read_text(encoding="utf-8")
    mentor_panel = (FRONTEND / "src/components/mentor/MentorEvaluationPanel.jsx").read_text(encoding="utf-8")
    student_panel = (FRONTEND / "src/components/student/StudentEvaluationPanel.jsx").read_text(encoding="utf-8")
    hr_view = (FRONTEND / "src/views/hr/HrInternshipSummaryView.jsx").read_text(encoding="utf-8")

    check('[Route("api/mentor/evaluations")]' in mentor_controller and
          '[Authorize(Roles = "ROLE_MENTOR")]' in mentor_controller,
          "US19 write/read APIs must be restricted to Mentor accounts.")
    check('[Route("api/student/evaluations")]' in student_controller and
          '[Authorize(Roles = "ROLE_STUDENT")]' in student_controller and "GetOwnByIdAsync" in student_controller,
          "Students must have read-only access to their own evaluations.")
    check('[Route("api/hr/internship-summary")]' in summary_controller and
          '[Authorize(Roles = "ROLE_HR,ROLE_ADMIN")]' in summary_controller,
          "US20 summary API must use the existing HR/Admin role policy.")
    check("ClaimTypes.NameIdentifier" in mentor_controller and "ClaimTypes.NameIdentifier" in student_controller and
          "evaluation.Student.MentorId == mentorId" in evaluation_service and
          "evaluation.StudentId == student.Id" in evaluation_service and
          "evaluation.MentorId != mentor.Id" in evaluation_service,
          "Evaluation ownership must be derived from authenticated accounts, assigned students, and evaluation authorship.")
    check("Range(1, 10)" in evaluation_dto and "StringLength(4000, MinimumLength = 2)" in evaluation_dto and
          "ValidateRequest" in evaluation_service and "ConflictException" in evaluation_service,
          "Evaluation requests must validate 1–10 scores, comments, and duplicate submissions.")
    check("InternshipEvaluations" in db_context and "IsUnique()" in db_context and
          "HasCheckConstraint" in db_context and "fk_internship_evaluations_student" in migration and
          "fk_internship_evaluations_mentor" in migration and "uk_internship_evaluations_student" in migration and
          "CHECK" in migration and "DROP TABLE" not in migration.upper(),
          "Evaluation persistence must be additive with score, uniqueness, and foreign-key constraints.")
    student_model = (BACKEND / "Data/Entities/Student.cs").read_text(encoding="utf-8")
    program_link = (BACKEND / "migrations/20261004_part11_student_program_link.sql").read_text(encoding="utf-8")
    check("ProgramId" in student_model and "fk_students_program" in program_link and
          "ADD COLUMN `program_id` INT NULL" in program_link and "DROP TABLE" not in program_link.upper(),
          "Student-program integration must be additive and preserve existing unassigned student profiles.")
    check("GroupBy(report => report.StudentId)" in summary_service and "MentorFeedbacks" in summary_service and
          "InternshipEvaluations" in summary_service and "WeeklyReportCount" in summary_service,
          "HR summary must aggregate existing reports, feedback, and evaluations without persisting duplicate summary data.")
    check("GetSummaryAsync(int? programId" in summary_service and "student.ProgramId == programId.Value" in summary_service,
          "US20 summary must be filterable by the live student-program relationship.")
    check("api.get('/mentor/evaluations')" in mentor_api and "api.post(`/mentor/evaluations/students/${studentId}`" in mentor_api and
          "api.get('/student/evaluations')" in mentor_api and "'/hr/internship-summary'" in hr_api,
          "US19/US20 frontend services must call the live backend APIs.")
    check('role="status"' in mentor_panel and 'role="alert"' in mentor_panel and
          'role="status"' in student_panel and 'role="alert"' in student_panel and
          'role="status"' in hr_view and 'role="alert"' in hr_view,
          "Mentor, student, and HR evaluation interfaces must render loading, empty, and error states.")
    check("VALIDATION_PART09_TESTS" in (BACKEND / "test_suite.py").read_text(encoding="utf-8"),
          "Part 09 authorization and validation integration checks must be available as opt-in tests.")
    app_code = (FRONTEND / "src/App.jsx").read_text(encoding="utf-8")
    sidebar = (FRONTEND / "src/components/layout/Sidebar.jsx").read_text(encoding="utf-8")
    route_match = re.search(r'path="/hr/internship-summary"([\s\S]*?)/>', app_code)
    check(route_match is not None and "ROLE_HR" in route_match.group(1) and "ROLE_ADMIN" in route_match.group(1) and
          "path: '/hr/internship-summary'" in sidebar and "ROLE_HR" in sidebar and "ROLE_ADMIN" in sidebar,
          "HR summary route and navigation must be available only to HR/Admin roles.")
    print("[PASS] US19/US20 roles, ownership, score validation, program-scoped data aggregation, and UI wiring are present.")


def validate_student_application_integration():
    auth_controller = (BACKEND / "Controllers/AuthController.cs").read_text(encoding="utf-8")
    registration = (BACKEND / "Services/StudentRegistrationService.cs").read_text(encoding="utf-8")
    programs_controller = (BACKEND / "Controllers/PublicInternshipProgramsController.cs").read_text(encoding="utf-8")
    hr_documents = (BACKEND / "Controllers/HrStudentDocumentsController.cs").read_text(encoding="utf-8")
    registration_view = (FRONTEND / "src/views/auth/StudentRegistrationView.jsx").read_text(encoding="utf-8")
    registration_api = (FRONTEND / "src/services/studentRegistrationService.js").read_text(encoding="utf-8")
    approval_view = (FRONTEND / "src/views/hr/StudentRegistrationApprovalView.jsx").read_text(encoding="utf-8")
    db_context = (BACKEND / "Data/AppDbContext.cs").read_text(encoding="utf-8")
    schedule_service = (BACKEND / "Services/StudentScheduleService.cs").read_text(encoding="utf-8")

    check('[HttpPost("register")]' in auth_controller and '[HttpPost("register-application")]' in auth_controller and
          '[Consumes("application/json")]' in auth_controller and
          '[Consumes("multipart/form-data")]' in auth_controller,
          "US06 must preserve the JSON registration API and provide a separate multipart application flow.")
    check("Status = UserStatus.PENDING_APPROVAL" in registration and "_passwordHasher.Hash(request.Password)" in registration and
          "ProgramId = program.Id" in registration and "BeginTransactionAsync" in registration and
          '"CV"' in registration and '"INTERNSHIP_LETTER"' in registration,
          "Registration must atomically create a pending Sprint 1 user/profile linked to a program and both documents.")
    check('[Route("api/programs")]' in programs_controller and "[AllowAnonymous]" in programs_controller,
          "Guest registration must load real internship programs through a public read-only API.")
    check('[Authorize(Roles = "ROLE_HR,ROLE_ADMIN")]' in hr_documents and "item.StudentId == studentId" in hr_documents,
          "HR document access must be role protected and scoped to the selected student profile.")
    check("getPrograms()" in registration_view and "internshipLetter" in registration_view and
          "api.post('/auth/register-application', form" in registration_api and "getRegistrationDocuments" in approval_view,
          "Frontend application must select a program, upload both documents, and expose them in HR review.")
    check("e => e.ProgramId" in db_context and "GetMyScheduleOverviewAsync" in schedule_service,
          "Student-program linkage must flow into the authenticated personal schedule.")
    print("[PASS] US04/US06 application, documents, program linkage, HR review access, and schedule integration are wired.")


def validate_program_and_task_flows():
    program_controller = (BACKEND / "Controllers/InternshipProgramsController.cs").read_text(encoding="utf-8")
    dates_dto = (BACKEND / "DTOs/Sprint2Part02Dtos.cs").read_text(encoding="utf-8")
    dates_view = (FRONTEND / "src/views/hr/ProgramDatesEditor.jsx").read_text(encoding="utf-8")
    mentor_controller = (BACKEND / "Controllers/MentorTasksController.cs").read_text(encoding="utf-8")
    student_controller = (BACKEND / "Controllers/StudentTasksController.cs").read_text(encoding="utf-8")
    task_service = (BACKEND / "Services/MentorTaskService.cs").read_text(encoding="utf-8")
    mentor_ui = (FRONTEND / "src/components/mentor/MentorTaskManagement.jsx").read_text(encoding="utf-8")
    student_ui = (FRONTEND / "src/components/student/StudentTasksPanel.jsx").read_text(encoding="utf-8")
    task_api = (FRONTEND / "src/services/mentorTaskService.js").read_text(encoding="utf-8")

    check('[Authorize(Roles = "ROLE_HR,ROLE_ADMIN")]' in program_controller and
          '[HttpPost("programs")]' in program_controller and '[HttpGet("programs")]' in program_controller and
          '[HttpPut("programs/{programId:int}/dates")]' in program_controller,
          "US11/US13 program creation, listing, and date updates must remain HR/Admin protected.")
    check("EndDate.Value < StartDate.Value" in dates_dto and "endDate >= startDate" in dates_view,
          "US13 must allow equal start/end dates and reject dates ending before the start.")
    check('[Authorize(Roles = "ROLE_MENTOR")]' in mentor_controller and
          '[Authorize(Roles = "ROLE_STUDENT")]' in student_controller and
          "item.Id == request.StudentId && item.MentorId == mentor.Id" in task_service and
          "task.Student.UserId != userId" in task_service,
          "US15/US16 must enforce mentor assignment and student ownership in the backend.")
    check('"TO_DO"' in task_service and '"IN_PROGRESS"' in task_service and '"DONE"' in task_service and
          "mentorTaskService.createTask" in mentor_ui and "updateMyTaskProgress" in student_ui and
          "api.put(`/student/tasks/${taskId}/progress`" in task_api,
          "US15/US16 task creation and progress choices must be connected to the real task API.")
    print("[PASS] US11/US13 role policies, date validation, and US15/US16 ownership-backed task flows are wired.")


def validate_mentor_delete_preserves_work_history():
    mentor_service = (BACKEND / "Services/MentorService.cs").read_text(encoding="utf-8")
    mentor_repository = (BACKEND / "Repositories/MentorRepository.cs").read_text(encoding="utf-8")
    hr_controller = (BACKEND / "Controllers/HrController.cs").read_text(encoding="utf-8")
    check("HasWorkReferencesAsync" in mentor_service and "ConflictException" in mentor_service and
          "HasWorkReferencesAsync" in mentor_repository and "_context.Tasks.AnyAsync" in mentor_repository and
          "_context.MentorFeedbacks.AnyAsync" in mentor_repository and
          "StatusCodes.Status409Conflict" in hr_controller,
          "Mentor deletion must preserve assigned tasks and feedback history with a 409 conflict response.")
    print("[PASS] Mentor deletion guards assigned tasks and feedback history.")


def validate_business_data_uses_api_only():
    auth_service = (FRONTEND / "src/services/authService.js").read_text(encoding="utf-8")
    check("api.post('/auth/login', { email, password })" in auth_service,
          "Login must authenticate through the existing API.")
    check("demo_jwt_token_" not in auth_service, "The login API must not fabricate a JWT when the response is incomplete.")
    check("mock_jwt_token_" not in auth_service and "getStoredUsers" not in auth_service and
          "isMockModeEnabled" not in auth_service,
          "Login must not bypass API authentication with a local mock token.")
    check("|| 'ROLE_ADMIN'" not in auth_service, "Login must not default an unknown account to administrator.")

    for relative_path in ("src/services/userService.js", "src/services/mentorService.js", "src/services/studentService.js"):
        service = (FRONTEND / relative_path).read_text(encoding="utf-8")
        check("mockData" not in service and "localStorage" not in service,
              f"{relative_path} must use the backend for business data without local fake-data fallbacks.")
    check(not (FRONTEND / "src/services/mockData.js").exists(), "Business mock data must not remain in the frontend service bundle.")
    check("VITE_ENABLE_MOCK_DATA" not in (FRONTEND / ".env.example").read_text(encoding="utf-8"),
          "The frontend must not expose an offline business-mock switch.")
    print("[PASS] Login and account, mentor, and student business data use backend APIs without mock fallbacks.")


def validate_student_accounts_link_to_existing_profiles():
    admin_view = (FRONTEND / "src/views/admin/UserManagementView.jsx").read_text(encoding="utf-8")
    check("CreateUserModal" not in admin_view and "Tạo Người Dùng Mới" not in admin_view,
          "Admin user management must not expose account creation.")

    student_view = (FRONTEND / "src/views/hr/StudentManagementView.jsx").read_text(encoding="utf-8")
    add_student_modal = (FRONTEND / "src/components/modals/AddStudentModal.jsx").read_text(encoding="utf-8")
    student_service = (BACKEND / "Services/StudentService.cs").read_text(encoding="utf-8")
    student_dto = (BACKEND / "DTOs/Student/CreateStudentRequestDto.cs").read_text(encoding="utf-8")
    repository = (BACKEND / "Repositories/StudentRepository.cs").read_text(encoding="utf-8")
    hr_controller = (BACKEND / "Controllers/HrController.cs").read_text(encoding="utf-8")
    user_service = (BACKEND / "Services/UserService.cs").read_text(encoding="utf-8")
    registration = (BACKEND / "Services/StudentRegistrationService.cs").read_text(encoding="utf-8")

    check("Liên Kết Tài Khoản Sinh Viên" in student_view and "Tạo Sinh Viên" not in student_view,
          "HR student management must describe the workflow as linking an existing account.")
    check("getStudentAccountLinks" in add_student_modal and "hasStudentProfile" in add_student_modal and
          "userId" in add_student_modal,
          "The student profile form must load existing accounts and reject already-linked accounts.")
    check("[HttpGet(\"student-accounts\")]" in hr_controller and
          "GetStudentAccountLinksAsync" in hr_controller,
          "HR/Admin must have a protected endpoint for live student account-link data.")
    check("HasStudentProfile = _context.Students.Any" in repository and
          "UserId = user.Id" in repository and "UnlinkedProfileCount" in repository,
          "Account/profile counts must come from users and students in the database.")
    check("[Required(ErrorMessage = \"An existing student account is required.\")]" in student_dto and
          "A student profile must be linked to an existing ROLE_STUDENT account." in student_service and
          "Tài khoản này đã có hồ sơ sinh viên." in student_service,
          "The API must require a student account and reject duplicate profile links.")
    check("Student accounts must be created through student registration" in user_service,
          "Admin account creation must not create ROLE_STUDENT accounts outside registration.")
    check("Student = new Student" in registration and "ProgramId = program.Id" in registration,
          "Registration must atomically create the student account/profile link and retain its program link.")
    print("[PASS] Admin cannot create student accounts from user management; profiles link to existing ROLE_STUDENT accounts.")


def validate_notifications_use_live_work_data():
    controller = (BACKEND / "Controllers/NotificationsController.cs").read_text(encoding="utf-8")
    service = (BACKEND / "Services/NotificationService.cs").read_text(encoding="utf-8")
    header = (FRONTEND / "src/components/layout/Header.jsx").read_text(encoding="utf-8")
    notification_service = (FRONTEND / "src/services/notificationService.js").read_text(encoding="utf-8")
    check('[Route("api/notifications")]' in controller and "[Authorize]" in controller,
          "The notification API must be authenticated.")
    for entity_query in ("_db.Users", "_db.Students", "_db.Tasks", "_db.WeeklyReports",
                         "_db.MentorFeedbacks", "_db.InternshipEvaluations"):
        check(entity_query in service, f"Notifications must be derived from live data in {entity_query}.")
    check("notificationService.getMine()" in header and "api.get('/notifications')" in notification_service,
          "The header bell must load notifications from the backend API.")
    print("[PASS] Header notifications are computed from authenticated, live internship records without a mock table.")


def validate_vite_host_check_is_enabled():
    vite_config = (FRONTEND / "vite.config.js").read_text(encoding="utf-8")
    check("allowedHosts: true" not in vite_config,
          "The Vite development server must keep host validation enabled to mitigate DNS rebinding.")
    print("[PASS] Vite keeps development-server host validation enabled.")


def validate_cors_origins_are_explicit():
    program = (BACKEND / "Program.cs").read_text(encoding="utf-8")
    deployment = (BACKEND / "render.yaml").read_text(encoding="utf-8")
    check(".EndsWith(\".vercel.app\"" not in program, "CORS must not trust all Vercel subdomains.")
    check(".AllowCredentials()" not in program, "Bearer-token CORS must not enable cookie credentials.")
    check("uri.AbsolutePath != \"/\"" in program, "CORS entries must be validated as origins without a path.")
    check("origin.Contains('*', StringComparison.Ordinal)" in program, "CORS entries must reject wildcard host patterns.")
    check("!isDevelopmentEnvironment && uri.Scheme != Uri.UriSchemeHttps" in program, "Production CORS origins must use HTTPS.")
    check("https://*.vercel.app" not in deployment, "Deployment configuration must use explicit CORS origins.")
    print("[PASS] CORS is restricted to explicitly configured origins and excludes cookie credentials.")


def validate_safe_database_setup():
    baseline = (BACKEND / "migrations/000_initial_core_schema.sql").read_text(encoding="utf-8")
    check("CREATE TABLE IF NOT EXISTS `roles`" in baseline, "The initial migration must create the core role table additively.")
    check("CREATE TABLE IF NOT EXISTS `users`" in baseline, "The initial migration must create the core user table additively.")
    executable_sql = re.sub(r"(?m)^\s*--.*$", "", baseline)
    check("DROP TABLE" not in executable_sql.upper(), "The initial migration must not drop existing tables.")
    deployment = (BACKEND / "DEPLOYMENT.md").read_text(encoding="utf-8")
    check("Không chạy `schema.sql` hoặc `seed_data.sql` trên cloud" in deployment, "Cloud setup must warn against destructive development scripts.")
    print("[PASS] Database deployment starts with an additive baseline and avoids destructive local scripts.")


def validate_api_test_safety():
    test_suite = (BACKEND / "test_suite.py").read_text(encoding="utf-8")
    check("VALIDATION_ADMIN_EMAIL" in test_suite and "VALIDATION_ADMIN_PASSWORD" in test_suite, "API tests must receive credentials from the environment.")
    check('"Admin@123"' not in test_suite, "API tests must not commit a default account password.")
    check("request(\"DELETE\"" not in test_suite, "API integration tests must not delete test or production records.")
    check("request(\"POST\", \"/api/auth/login\"" in test_suite, "API checks must authenticate through the existing login endpoint.")
    check('WEEKLY_REPORT_TESTS = os.getenv("VALIDATION_WEEKLY_REPORT_TESTS") == "1"' in test_suite and
          'WEEKLY_REPORT_TEST_DATABASE = os.getenv("VALIDATION_TEST_DATABASE") == "1"' in test_suite and
          'PART09_TESTS = os.getenv("VALIDATION_PART09_TESTS") == "1"' in test_suite and
          'parsed_url.hostname not in {"localhost", "127.0.0.1", "::1"}' in test_suite,
          "Mutating API tests must require explicit opt-in, a disposable database declaration, and a loopback API.")
    check("if not WEEKLY_REPORT_TESTS:" in test_suite and "if not PART09_TESTS:" in test_suite and
          test_suite.count("if not WEEKLY_REPORT_TEST_DATABASE:") >= 2,
          "Weekly-report and evaluation writes must stay outside the default API smoke suite and require a test database.")
    print("[PASS] API checks use environment credentials; mutating writes require opt-in, a disposable DB, and loopback.")


def validate_production_secret_configuration():
    settings = json.loads((BACKEND / "appsettings.json").read_text(encoding="utf-8"))
    check("Key" not in settings.get("Jwt", {}), "A JWT signing key must not be committed in appsettings.json.")
    check(
        "DefaultConnection" not in settings.get("ConnectionStrings", {}),
        "A local database credential must not be committed in appsettings.json.",
    )
    program = (BACKEND / "Program.cs").read_text(encoding="utf-8")
    token_service = (BACKEND / "Services/JwtTokenService.cs").read_text(encoding="utf-8")
    development_settings = json.loads((BACKEND / "appsettings.Development.json").read_text(encoding="utf-8"))
    check("Development_Only_JWT_Key" not in program, "A built-in development JWT key must not be committed.")
    check('builder.Configuration["Jwt:Key"] = jwtKey;' in program, "Token signing and validation must use the same resolved JWT key.")
    check('builder.Configuration["Jwt:Issuer"] = jwtIssuer;' in program, "Token signing and validation must use the same resolved JWT issuer.")
    check('builder.Configuration["Jwt:Audience"] = jwtAudience;' in program, "Token signing and validation must use the same resolved JWT audience.")
    check('?? "YourSuperSecretKey' not in token_service, "JWT token creation must not use a built-in signing key.")
    check("ConnectionStrings" not in development_settings, "A local database credential must not be committed in development settings.")
    check(
        "A JWT signing key must be configured" in program,
        "The API must fail closed when no JWT signing key is configured.",
    )
    check("double.IsFinite(parsed)" in token_service and "parsed > 0" in token_service,
          "JWT expiration must reject non-finite and non-positive durations.")
    check("CultureInfo.InvariantCulture" in token_service,
          "JWT expiration parsing must not depend on the host locale.")
    print("[PASS] JWT configuration is shared between signing and validation, and local credentials are externalized.")


def validate_exception_sanitization():
    middleware = (BACKEND / "Middleware/GlobalExceptionMiddleware.cs").read_text(encoding="utf-8")
    check(
        "errors = exception.Message" not in middleware,
        "Unhandled exception details must not be returned in API error responses.",
    )
    check("_logger.LogError(ex," in middleware, "Unhandled exceptions must still be logged server-side.")
    print("[PASS] Generic server exceptions are logged but not exposed to API clients.")


def validate_database_readiness_health_check():
    program = (BACKEND / "Program.cs").read_text(encoding="utf-8")
    check("db.Database.CanConnectAsync(cancellationToken)" in program,
          "The health endpoint must verify that the configured database is reachable.")
    check("StatusCodes.Status503ServiceUnavailable" in program,
          "The health endpoint must report an unavailable database with HTTP 503.")
    check('app.Logger.LogWarning(exception, "Database readiness check failed.")' in program,
          "Database health failures must be logged without exposing details in the response.")
    print("[PASS] Health endpoint reports database readiness without leaking connection details.")


def validate_production_mysql_tls():
    program = (BACKEND / "Program.cs").read_text(encoding="utf-8")
    deployment = (BACKEND / "DEPLOYMENT.md").read_text(encoding="utf-8")
    check("builder.Environment.IsProduction()" in program,
          "Database TLS policy must be selected from the ASP.NET environment.")
    check("if (requireTls)" in program and "connBuilder.SslMode = MySqlSslMode.Required;" in program,
          "Production MySQL connections must require TLS even if the supplied connection string prefers it.")
    check("SslMode=Required" in deployment,
          "Production deployment documentation must require encrypted MySQL connections.")
    print("[PASS] Production MySQL connections require TLS.")


def validate_password_hashing_and_seed_fixtures():
    hasher = (BACKEND / "Services/BcryptPasswordHasher.cs").read_text(encoding="utf-8")
    check("BCrypt.Net.BCrypt.Verify(password, passwordHash)" in hasher, "Passwords must be checked using BCrypt.")
    check("password == \"Admin@123\"" not in hasher, "Password verification must not include a seed-password bypass.")

    fake_hash = "$2a$11$eA8tVvKjF4B3mH1eZ1pXhe7Yn6o7E7v1r3f7e6o5a4b3c2d1e0f9a"
    for relative_path in ("backend/seed_data.sql", "MySQL-Nhom2/seed_data.sql"):
        seed = (ROOT / relative_path).read_text(encoding="utf-8")
        check(fake_hash not in seed, f"{relative_path} must not rely on the non-BCrypt sentinel hash.")
        check(
            re.search(r"\$2[aby]\$11\$[./A-Za-z0-9]{53}", seed) is not None,
            f"{relative_path} must use a syntactically valid BCrypt work-factor-11 seed hash.",
        )
        check("Never run this seed script in production" in seed, f"{relative_path} must be labeled as development-only.")
    mentor_service = (BACKEND / "Services/MentorService.cs").read_text(encoding="utf-8")
    mentor_request = (BACKEND / "DTOs/Mentor/CreateMentorRequestDto.cs").read_text(encoding="utf-8")
    check('Hash("Admin@123")' not in mentor_service, "New mentor accounts must not receive a shared default password.")
    check("_passwordHasher.Hash(request.Password)" in mentor_service, "Mentor account passwords must be hashed from the submitted initial credential.")
    check("Initial password must contain at least 12 characters" in mentor_request, "Mentor account creation must validate initial password length.")
    print("[PASS] Password verification has no seed-password bypass; seed fixtures use BCrypt hashes and are marked development-only.")


def validate_bcrypt_password_byte_limit():
    hasher = (BACKEND / "Services/BcryptPasswordHasher.cs").read_text(encoding="utf-8")
    attribute = (BACKEND / "Common/MaxUtf8ByteLengthAttribute.cs").read_text(encoding="utf-8")
    check("Encoding.UTF8.GetByteCount(text) <= MaximumBytes" in attribute,
          "Password length validation must count UTF-8 bytes, not characters.")
    check("MaximumUtf8PasswordBytes = 72" in hasher and "Encoding.UTF8.GetByteCount(password) > MaximumUtf8PasswordBytes" in hasher,
          "BCrypt hashing and verification must reject inputs beyond its 72-byte limit.")

    password_dtos = (
        "DTOs/Auth/LoginRequestDto.cs",
        "DTOs/Admin/CreateUserRequestDto.cs",
        "DTOs/StudentRegistrationDtos.cs",
        "DTOs/Mentor/CreateMentorRequestDto.cs",
    )
    for relative_path in password_dtos:
        dto = (BACKEND / relative_path).read_text(encoding="utf-8")
        check("MaxUtf8ByteLength(72)" in dto,
              f"{relative_path} must reject over-limit passwords during request validation.")
    print("[PASS] Password DTOs and BCrypt reject passwords longer than 72 UTF-8 bytes.")


def validate_account_status_enum():
    create_user_dto = (BACKEND / "DTOs/Admin/CreateUserRequestDto.cs").read_text(encoding="utf-8")
    check("[EnumDataType(typeof(UserStatus)" in create_user_dto,
          "Account creation must reject numeric user status values not defined by UserStatus.")
    check("public UserStatus Status { get; set; } = UserStatus.ACTIVE;" in create_user_dto,
          "Account creation must preserve ACTIVE as the default status.")
    print("[PASS] Account creation validates status values and retains its ACTIVE default.")


def main():
    print("Validating repository requirements and selected regressions...")
    validate_connection_charset()
    validate_production_secret_configuration()
    validate_exception_sanitization()
    validate_database_readiness_health_check()
    validate_production_mysql_tls()
    validate_password_hashing_and_seed_fixtures()
    validate_bcrypt_password_byte_limit()
    validate_account_status_enum()
    validate_frontend_contracts()
    validate_dashboard_roles_match_api_policies()
    validate_sidebar_roles_match_routes()
    validate_student_schedule_contract()
    validate_weekly_report_contract()
    validate_part09_contract()
    validate_student_application_integration()
    validate_program_and_task_flows()
    validate_mentor_delete_preserves_work_history()
    validate_business_data_uses_api_only()
    validate_student_accounts_link_to_existing_profiles()
    validate_notifications_use_live_work_data()
    validate_vite_host_check_is_enabled()
    validate_cors_origins_are_explicit()
    validate_safe_database_setup()
    validate_api_test_safety()
    validate_database_collation()
    validate_seed_api()
    print("Validation complete. Optional external checks are marked SKIP when credentials are not configured.")


if __name__ == "__main__":
    try:
        main()
    except (AssertionError, OSError, ValueError, subprocess.SubprocessError, urllib.error.URLError) as error:
        print(f"[FAIL] {error}", file=sys.stderr)
        sys.exit(1)
