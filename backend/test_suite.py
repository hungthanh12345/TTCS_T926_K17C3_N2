"""Read-only API smoke checks; credentials and target are provided via environment."""

import json
import os
import sys
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


def run_tests():
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
