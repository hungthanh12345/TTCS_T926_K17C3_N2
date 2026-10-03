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

    app_code = (FRONTEND / "src/App.jsx").read_text(encoding="utf-8")
    check(
        '<Route path="/" element={<Navigate to="/login" replace />} />' in app_code,
        "The root route must navigate to the login page.",
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


def validate_mock_fallback_is_development_only():
    mock_mode = (FRONTEND / "src/services/mockMode.js").read_text(encoding="utf-8")
    check("import.meta.env.DEV" in mock_mode, "Mock mode must be restricted to development builds.")
    check("VITE_ENABLE_MOCK_DATA === 'true'" in mock_mode, "Mock mode must require explicit opt-in.")

    auth_service = (FRONTEND / "src/services/authService.js").read_text(encoding="utf-8")
    check("demo_jwt_token_" not in auth_service, "The login API must not fabricate a JWT when the response is incomplete.")
    check("|| 'ROLE_ADMIN'" not in auth_service, "Login must not default an unknown account to administrator.")
    check("error.isAxiosError && !error.response && isMockModeEnabled" in auth_service, "Mock login must only handle opted-in network failures.")

    for relative_path in ("src/services/userService.js", "src/services/mentorService.js", "src/services/studentService.js"):
        service = (FRONTEND / relative_path).read_text(encoding="utf-8")
        check("if (!error.response)" not in service, f"{relative_path} must not silently substitute mock data in production.")
        check("!error.response && isMockModeEnabled" in service, f"{relative_path} mock behavior must be opt-in.")
    print("[PASS] Client-side mock login and data fallbacks require explicit development-only opt-in.")


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
    check("request(\"PUT\"" not in test_suite and "request(\"DELETE\"" not in test_suite, "API smoke tests must not modify or delete production data.")
    check("request(\"POST\", \"/api/auth/login\"" in test_suite, "Mutating API tests must be excluded from the default smoke suite.")
    print("[PASS] API smoke tests are environment-driven and read-only.")


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
    validate_mock_fallback_is_development_only()
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
