import urllib.request
import urllib.error
import json
import sys

BASE_URL = "http://localhost:5000"

def request(method, path, body=None, token=None):
    url = f"{BASE_URL}{path}"
    headers = {
        "Content-Type": "application/json",
        "Accept": "application/json"
    }
    if token:
        headers["Authorization"] = f"Bearer {token}"
        
    data = json.dumps(body).encode("utf-8") if body is not None else None
    req = urllib.request.Request(url, data=data, headers=headers, method=method)
    
    try:
        with urllib.request.urlopen(req) as resp:
            content = resp.read().decode("utf-8")
            return resp.status, json.loads(content) if content else {}
    except urllib.error.HTTPError as e:
        content = e.read().decode("utf-8")
        try:
            parsed = json.loads(content)
        except Exception:
            parsed = {"raw": content}
        return e.code, parsed

def run_tests():
    passed = 0
    failed = 0

    def assert_eq(test_name, actual, expected):
        nonlocal passed, failed
        if actual == expected:
            print(f"[PASS] {test_name} (Status: {actual})")
            passed += 1
        else:
            print(f"[FAIL] {test_name}: expected {expected}, got {actual}")
            failed += 1

    print("==================================================")
    print("STARTING FULL END-TO-END SUITE FOR BACKEND API")
    print("==================================================")

    # 1. AUTHENTICATION TESTS
    # 1.1 Login with wrong password
    status, res = request("POST", "/api/auth/login", {"email": "admin@gmail.com", "password": "WrongPassword"})
    assert_eq("1.1 Login with invalid credentials", status, 401)

    # 1.2 Login with valid admin credentials
    status, res = request("POST", "/api/auth/login", {"email": "admin@gmail.com", "password": "Admin@123"})
    assert_eq("1.2 Login with valid Admin credentials", status, 200)
    admin_token = res["data"]["token"]
    assert res["data"]["user"]["role"] == "ROLE_ADMIN", "Admin role mismatch"

    # 2. AUTHORIZATION TESTS
    # 2.1 Access /api/admin/users without token
    status, res = request("GET", "/api/admin/users")
    assert_eq("2.1 Access admin endpoint without token", status, 401)

    # 2.2 Access /api/admin/users with Admin token
    status, res = request("GET", "/api/admin/users", token=admin_token)
    assert_eq("2.2 Access admin endpoint with Admin token", status, 200)
    print(f"    -> Found {len(res['data'])} existing users")

    # 3. USER MANAGEMENT (ADMIN)
    # 3.1 Create new HR user
    hr_email = "alex.hr@system.local"
    status, res = request("POST", "/api/admin/users", {
        "email": hr_email,
        "password": "Password@123",
        "roleName": "ROLE_HR",
        "status": 0 # ACTIVE
    }, token=admin_token)
    if status == 409: # Already exists from previous run
        print(f"    (User {hr_email} already exists, continuing)")
    else:
        assert_eq("3.1 Create HR user account", status, 201)

    # 3.2 Create new Mentor user
    mentor_email = "elena.mentor@system.local"
    status, res = request("POST", "/api/admin/users", {
        "email": mentor_email,
        "password": "Password@123",
        "roleName": "ROLE_MENTOR"
    }, token=admin_token)
    if status == 409:
        print(f"    (User {mentor_email} already exists, continuing)")
        # Look up existing id
        _, users_res = request("GET", "/api/admin/users", token=admin_token)
        mentor_user_id = next(u["id"] for u in users_res["data"] if u["email"] == mentor_email)
    else:
        assert_eq("3.2 Create Mentor user account", status, 201)
        mentor_user_id = res["data"]["id"]

    # 3.3 Create new Student user
    student_email = "michael.student@system.local"
    status, res = request("POST", "/api/admin/users", {
        "email": student_email,
        "password": "Password@123",
        "roleName": "ROLE_STUDENT"
    }, token=admin_token)
    if status == 409:
        print(f"    (User {student_email} already exists, continuing)")
        _, users_res = request("GET", "/api/admin/users", token=admin_token)
        student_user_id = next(u["id"] for u in users_res["data"] if u["email"] == student_email)
    else:
        assert_eq("3.3 Create Student user account", status, 201)
        student_user_id = res["data"]["id"]

    # 4. HR LOGIN & RBAC FORBIDDEN TEST
    # 4.1 Login as HR user
    status, res = request("POST", "/api/auth/login", {"email": hr_email, "password": "Password@123"})
    assert_eq("4.1 Login as new HR user", status, 200)
    hr_token = res["data"]["token"]
    assert res["data"]["user"]["role"] == "ROLE_HR", "Expected ROLE_HR"

    # 4.2 HR user attempts to access /api/admin/users -> 403 Forbidden
    status, res = request("GET", "/api/admin/users", token=hr_token)
    assert_eq("4.2 RBAC: HR access to Admin endpoint", status, 403)

    # 5. MENTOR MANAGEMENT (HR)
    # 5.1 Create Mentor profile
    status, res = request("POST", "/api/hr/mentors", {
        "userId": mentor_user_id,
        "fullName": "Elena Rostova",
        "phoneNumber": "0988776655",
        "department": "AI & Cloud Engineering",
        "specialization": "Distributed Systems & Machine Learning"
    }, token=hr_token)
    if status == 409:
        print(f"    (Mentor profile for user {mentor_user_id} already exists, fetching)")
        _, mentors_res = request("GET", "/api/hr/mentors", token=hr_token)
        mentor_id = next(m["id"] for m in mentors_res["data"] if m["userId"] == mentor_user_id)
    else:
        assert_eq("5.1 Create Mentor profile", status, 201)
        mentor_id = res["data"]["id"]

    # 5.2 Retrieve all Mentors
    status, res = request("GET", "/api/hr/mentors", token=hr_token)
    assert_eq("5.2 Retrieve all Mentors", status, 200)
    print(f"    -> Retrieved {len(res['data'])} mentors")

    # 6. STUDENT MANAGEMENT (HR)
    # 6.1 Create Student Profile
    student_code = "STU2026888"
    status, res = request("POST", "/api/hr/students", {
        "studentCode": student_code,
        "fullName": "Michael Scott",
        "phoneNumber": "0911223344",
        "university": "Hanoi University of Science and Technology",
        "major": "Computer Science",
        "userId": student_user_id
    }, token=hr_token)
    if status == 409:
        print(f"    (Student profile {student_code} already exists, searching)")
        _, s_res = request("GET", f"/api/hr/students/search?keyword={student_code}", token=hr_token)
        student_id = s_res["data"]["items"][0]["id"]
    else:
        assert_eq("6.1 Create Student profile", status, 201)
        student_id = res["data"]["id"]

    # 6.2 Get Student by ID
    status, res = request("GET", f"/api/hr/students/{student_id}", token=hr_token)
    assert_eq("6.2 Get Student details by ID", status, 200)
    assert res["data"]["studentCode"] == student_code

    # 6.3 Update Student profile
    status, res = request("PUT", f"/api/hr/students/{student_id}", {
        "fullName": "Michael Gary Scott",
        "phoneNumber": "0911223399",
        "university": "Hanoi University of Science and Technology",
        "major": "Software Architecture",
        "studentCode": student_code,
        "userId": student_user_id
    }, token=hr_token)
    assert_eq("6.3 Update Student profile", status, 200)
    assert res["data"]["fullName"] == "Michael Gary Scott"

    # 6.4 Assign Mentor to Student
    status, res = request("PUT", f"/api/hr/students/{student_id}/assign-mentor", {
        "mentorId": mentor_id
    }, token=hr_token)
    assert_eq("6.4 Assign Mentor to Student", status, 200)
    assert res["data"]["mentorId"] == mentor_id, "MentorId should match assigned mentor"
    assert res["data"]["mentor"]["fullName"] == "Elena Rostova", "Mentor fullName should match"

    # 6.5 Search Students with filters & pagination
    status, res = request("GET", "/api/hr/students/search?university=Hanoi&page=1&pageSize=5", token=hr_token)
    assert_eq("6.5 Search Students with filters & pagination", status, 200)
    assert res["data"]["totalItems"] >= 1, "Should find at least 1 student"
    print(f"    -> Search found {res['data']['totalItems']} total items, Page {res['data']['pageNumber']}/{res['data']['totalPages']}")

    # 7. VALIDATION & ERROR HANDLING
    # 7.1 Input validation error (empty email and short password)
    status, res = request("POST", "/api/auth/login", {"email": "invalid-email", "password": "123"})
    assert_eq("7.1 Validation failure response (400 Bad Request)", status, 400)
    assert res["success"] is False

    # 7.2 Non-existent resource (404 Not Found)
    status, res = request("GET", "/api/hr/students/999999", token=hr_token)
    assert_eq("7.2 Non-existent student response (404 Not Found)", status, 404)
    assert res["success"] is False

    print("==================================================")
    print(f"RESULTS: {passed} PASSED, {failed} FAILED")
    print("==================================================")
    if failed > 0:
        sys.exit(1)

if __name__ == "__main__":
    run_tests()
