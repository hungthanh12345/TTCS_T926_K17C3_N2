import json
import re
import subprocess
import sys
import urllib.request

sys.stdout.reconfigure(encoding='utf-8')

def test_requirements():
    print("=" * 60)
    print("VERIFYING ALL 4 TASK REQUIREMENTS")
    print("=" * 60)

    # ---------------------------------------------------------
    # 1. FIX VIETNAMESE ENCODING BUG & CONNECTION STRING
    # ---------------------------------------------------------
    print("\n--- REQUIREMENT 1: VIETNAMESE ENCODING & CONNECTION STRING ---")
    
    # Check appsettings.json
    with open("d:/backend/appsettings.json", "r", encoding="utf-8") as f:
        appsettings = json.load(f)
    conn_str = appsettings["ConnectionStrings"]["DefaultConnection"]
    has_charset = "CharSet=utf8mb4" in conn_str or "charset=utf8mb4" in conn_str
    print(f"[CHECK] appsettings.json contains CharSet=utf8mb4: {'PASS' if has_charset else 'FAIL'}")
    assert has_charset, "Connection string missing CharSet=utf8mb4"

    # Check MySQL DB and Table collations
    cmd = [
        r"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe",
        "--default-character-set=utf8mb4",
        "-u", "root",
        "-p123456",
        "-e",
        "SELECT default_character_set_name, default_collation_name FROM information_schema.SCHEMATA WHERE schema_name = 'internship_management';"
    ]
    proc = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8")
    db_utf8 = "utf8mb4" in proc.stdout and "utf8mb4_unicode_ci" in proc.stdout
    print(f"[CHECK] MySQL Database character set & collation: {'PASS (utf8mb4 / utf8mb4_unicode_ci)' if db_utf8 else 'FAIL'}")
    assert db_utf8, "Database not set to utf8mb4 / utf8mb4_unicode_ci"

    # ---------------------------------------------------------
    # 2. RESTRUCTURE MENTOR SEED DATA
    # ---------------------------------------------------------
    print("\n--- REQUIREMENT 2: RESTRUCTURE MENTOR SEED DATA ---")
    # Login as Admin to query API
    login_req = urllib.request.Request(
        "http://localhost:5000/api/auth/login",
        data=json.dumps({"email": "hung.nt.admin@gmail.com", "password": "Admin@123"}).encode("utf-8"),
        headers={"Content-Type": "application/json"}
    )
    res = urllib.request.urlopen(login_req)
    admin_token = json.loads(res.read().decode("utf-8"))["data"]["token"]

    # Query Mentors
    mentor_req = urllib.request.Request(
        "http://localhost:5000/api/hr/mentors",
        headers={"Authorization": f"Bearer {admin_token}"}
    )
    res_m = urllib.request.urlopen(mentor_req)
    mentor_data = json.loads(res_m.read().decode("utf-8"))["data"]
    mentors = mentor_data.get("items", mentor_data) if isinstance(mentor_data, dict) else mentor_data
    
    print(f"[CHECK] Total Mentors count: {len(mentors)} (Expected: exactly 1)")
    assert len(mentors) == 1, f"Expected 1 mentor, found {len(mentors)}"
    
    m0 = mentors[0]
    print(f"   Mentor Full Name: '{m0['fullName']}' (Expected: 'Nguyễn Khánh Tùng')")
    print(f"   Department: '{m0['department']}' (Expected: 'Kỹ thuật phần mềm')")
    print(f"   Specialization: '{m0['specialization']}' (Expected: 'Full-stack Web & Cloud Native')")
    assert m0["fullName"] == "Nguyễn Khánh Tùng", "Mentor name mismatch"
    assert m0["department"] == "Kỹ thuật phần mềm", "Mentor department mismatch"
    assert m0["specialization"] == "Full-stack Web & Cloud Native", "Mentor specialization mismatch"
    print("[PASS] Mentor seed data correctly restructured to strictly 1 mentor.")

    # Query Students
    student_req = urllib.request.Request(
        "http://localhost:5000/api/hr/students",
        headers={"Authorization": f"Bearer {admin_token}"}
    )
    res_s = urllib.request.urlopen(student_req)
    student_data = json.loads(res_s.read().decode("utf-8"))["data"]
    students = student_data.get("items", student_data) if isinstance(student_data, dict) else student_data
    
    print(f"[CHECK] Total Students count: {len(students)}")
    has_question_marks = False
    assigned_count = 0
    unassigned_count = 0

    for s in students:
        full_text = f"{s['fullName']} {s['university']} {s['major']}"
        if "?" in full_text:
            has_question_marks = True
            print(f"   [FAIL] Question mark detected in student: {full_text}")
        if s.get("mentor") and s["mentor"]["fullName"] == "Nguyễn Khánh Tùng":
            assigned_count += 1
        elif not s.get("mentor"):
            unassigned_count += 1

        assert s['university'] == 'Đại học Công nghệ Thông tin & Truyền thông — ĐHTN', f"University mismatch: {s['university']}"
        assert s['major'] == 'Kỹ thuật Phần mềm', f"Major mismatch: {s['major']}"

    print(f"[CHECK] Vietnamese diacritics integrity: {'PASS (No ? marks found)' if not has_question_marks else 'FAIL'}")
    assert not has_question_marks, "Detected question marks in student records"
    print(f"[CHECK] Students assigned to Nguyễn Khánh Tùng: {assigned_count} / {len(students)}")
    assert assigned_count == len(students), f"Expected all {len(students)} students assigned to Nguyễn Khánh Tùng, got {assigned_count}"

    # ---------------------------------------------------------
    # 3. FIX QUICK-LOGIN BEHAVIOR (NO AUTO-SUBMISSION)
    # ---------------------------------------------------------
    print("\n--- REQUIREMENT 3: QUICK-LOGIN BEHAVIOR IN FRONTEND ---")
    with open("d:/frontend/src/views/auth/LoginView.jsx", "r", encoding="utf-8") as f:
        login_view_code = f.read()

    # Verify handleQuickFill does NOT execute login
    quick_fill_match = re.search(r"const handleQuickFill = \(acc\)([\s\S]*?)\};", login_view_code)
    assert quick_fill_match, "handleQuickFill function not found in LoginView.jsx"
    quick_fill_body = quick_fill_match.group(1)
    
    has_execute_call = "executeLogin" in quick_fill_body or "login(" in quick_fill_body
    print(f"[CHECK] handleQuickFill does NOT invoke login API automatically: {'PASS' if not has_execute_call else 'FAIL'}")
    assert not has_execute_call, "handleQuickFill is still automatically submitting the login!"

    has_set_form_data = "setFormData" in quick_fill_body
    print(f"[CHECK] handleQuickFill sets formData email & password: {'PASS' if has_set_form_data else 'FAIL'}")
    assert has_set_form_data, "handleQuickFill does not set form data"

    # ---------------------------------------------------------
    # 4. PREVENT UNWANTED AUTO-LOGIN ON APP LAUNCH
    # ---------------------------------------------------------
    print("\n--- REQUIREMENT 4: PREVENT AUTO-LOGIN & CLEAN SESSION RESET ---")
    with open("d:/frontend/src/App.jsx", "r", encoding="utf-8") as f:
        app_code = f.read()

    # Verify root path navigates to /login
    has_root_navigate = '<Route path="/" element={<Navigate to="/login" replace />} />' in app_code
    print(f"[CHECK] App.jsx maps '/' to <Navigate to=\"/login\" replace />: {'PASS' if has_root_navigate else 'FAIL'}")
    assert has_root_navigate, "App.jsx does not direct root '/' directly to '/login'"

    # Verify LoginView performs clean session reset on mount
    has_reset_session = "resetSession?.();" in login_view_code and "useEffect" in login_view_code
    print(f"[CHECK] LoginView.jsx calls resetSession on mount: {'PASS' if has_reset_session else 'FAIL'}")
    assert has_reset_session, "LoginView does not reset session on mount"

    # Verify AuthContext provides resetSession
    with open("d:/frontend/src/context/AuthContext.jsx", "r", encoding="utf-8") as f:
        auth_context_code = f.read()
    has_reset_def = "const resetSession = () => {" in auth_context_code and "resetSession," in auth_context_code
    print(f"[CHECK] AuthContext.jsx implements and exports resetSession: {'PASS' if has_reset_def else 'FAIL'}")
    assert has_reset_def, "AuthContext does not provide resetSession"

    print("\n" + "=" * 60)
    print("ALL 4 REQUIREMENTS FULLY VERIFIED AND PASSING!")
    print("=" * 60)

if __name__ == "__main__":
    test_requirements()
