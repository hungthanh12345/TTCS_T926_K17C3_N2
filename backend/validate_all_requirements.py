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
        "const resetSession = () => {" in auth_context and "resetSession," in auth_context,
        "AuthContext must expose resetSession.",
    )

    print("[PASS] Frontend login validation, root route, and session-reset checks passed.")


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
    print("[PASS] JWT configuration is shared between signing and validation, and local credentials are externalized.")


def validate_exception_sanitization():
    middleware = (BACKEND / "Middleware/GlobalExceptionMiddleware.cs").read_text(encoding="utf-8")
    check(
        "errors = exception.Message" not in middleware,
        "Unhandled exception details must not be returned in API error responses.",
    )
    check("_logger.LogError(ex," in middleware, "Unhandled exceptions must still be logged server-side.")
    print("[PASS] Generic server exceptions are logged but not exposed to API clients.")


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
    print("[PASS] Password verification has no seed-password bypass; seed fixtures use BCrypt hashes and are marked development-only.")


def main():
    print("Validating repository requirements and selected regressions...")
    validate_connection_charset()
    validate_production_secret_configuration()
    validate_exception_sanitization()
    validate_password_hashing_and_seed_fixtures()
    validate_frontend_contracts()
    validate_database_collation()
    validate_seed_api()
    print("Validation complete. Optional external checks are marked SKIP when credentials are not configured.")


if __name__ == "__main__":
    try:
        main()
    except (AssertionError, OSError, ValueError, subprocess.SubprocessError, urllib.error.URLError) as error:
        print(f"[FAIL] {error}", file=sys.stderr)
        sys.exit(1)
