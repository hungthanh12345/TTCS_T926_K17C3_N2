import requests
import json
import sys

sys.stdout.reconfigure(encoding='utf-8')

BASE_URL = "http://localhost:5000/api"

def run_tests():
    print("=== TEST SUITE: US21 ATTENDANCE CHECK-IN / CHECK-OUT ===")
    
    # 1. Login as student
    print("\n[Step 1] Logging in as student (hung.nt@gmail.com)...")
    login_res = requests.post(f"{BASE_URL}/auth/login", json={
        "email": "hung.nt@gmail.com",
        "password": "Admin@123"
    })
    assert login_res.status_code == 200, f"Login failed: {login_res.text}"
    token = login_res.json()["data"]["token"]
    headers = {
        "Authorization": f"Bearer {token}",
        "Content-Type": "application/json"
    }
    print("  -> Login success! JWT acquired.")

    # 2. Case A: Checkout before checkin
    # First, let's check current status
    status_res = requests.get(f"{BASE_URL}/student/attendance/today", headers=headers)
    assert status_res.status_code == 200, f"Get today status failed: {status_res.text}"
    today_status = status_res.json()["data"]
    print(f"  -> Current today status: isCheckedIn={today_status['IsCheckedIn'] if 'IsCheckedIn' in today_status else today_status.get('isCheckedIn')}")

    # If already checked in from a previous run today, let's test checkout duplicate
    is_in = today_status.get("isCheckedIn", today_status.get("IsCheckedIn", False))
    is_out = today_status.get("isCheckedOut", today_status.get("IsCheckedOut", False))

    if not is_in:
        print("\n[Step 2 - Case A] Testing Check-out BEFORE Check-in...")
        checkout_early = requests.post(f"{BASE_URL}/student/attendance/check-out", headers=headers, json={"notes": "Too early"})
        print(f"  -> Response status: {checkout_early.status_code}")
        print(f"  -> Response body: {checkout_early.text}")
        assert checkout_early.status_code == 400, f"Expected 400 Bad Request, got {checkout_early.status_code}"
        print("  [PASS] Correctly rejected checkout before checkin!")

        print("\n[Step 3 - Case B] Testing first valid Check-in...")
        checkin_res = requests.post(f"{BASE_URL}/student/attendance/check-in", headers=headers, json={"notes": "Bắt đầu ca sáng"})
        print(f"  -> Response status: {checkin_res.status_code}")
        print(f"  -> Response body: {checkin_res.text}")
        assert checkin_res.status_code == 201, f"Expected 201 Created, got {checkin_res.status_code}"
        checkin_data = checkin_res.json()["data"]
        assert checkin_data["status"] == "CHECKED_IN"
        print("  [PASS] Successfully checked in!")

    print("\n[Step 4 - Case C] Testing duplicate Check-in on the same day...")
    dup_checkin = requests.post(f"{BASE_URL}/student/attendance/check-in", headers=headers, json={"notes": "Check-in again"})
    print(f"  -> Response status: {dup_checkin.status_code}")
    print(f"  -> Response body: {dup_checkin.text}")
    assert dup_checkin.status_code in (400, 409), f"Expected 400 or 409, got {dup_checkin.status_code}"
    print("  [PASS] Correctly rejected duplicate check-in!")

    # Check status today
    status_mid = requests.get(f"{BASE_URL}/student/attendance/today", headers=headers).json()["data"]
    is_out_now = status_mid.get("isCheckedOut", status_mid.get("IsCheckedOut", False))

    if not is_out_now:
        print("\n[Step 5 - Case E] Testing valid Check-out...")
        checkout_res = requests.post(f"{BASE_URL}/student/attendance/check-out", headers=headers, json={"notes": "Kết thúc ca làm việc"})
        print(f"  -> Response status: {checkout_res.status_code}")
        print(f"  -> Response body: {checkout_res.text}")
        assert checkout_res.status_code == 200, f"Expected 200 OK, got {checkout_res.status_code}"
        checkout_data = checkout_res.json()["data"]
        assert checkout_data["status"] == "COMPLETED"
        assert checkout_data["checkOutTime"] is not None
        print(f"  -> Duration: {checkout_data['durationFormatted']} ({checkout_data['durationMinutes']} mins)")
        print("  [PASS] Successfully checked out and recorded duration!")

    print("\n[Step 6 - Case F] Testing duplicate Check-out on the same day...")
    dup_checkout = requests.post(f"{BASE_URL}/student/attendance/check-out", headers=headers, json={"notes": "Check-out again"})
    print(f"  -> Response status: {dup_checkout.status_code}")
    print(f"  -> Response body: {dup_checkout.text}")
    assert dup_checkout.status_code in (400, 409), f"Expected 400 or 409, got {dup_checkout.status_code}"
    print("  [PASS] Correctly rejected duplicate check-out!")

    print("\n[Step 7 - Case G] Testing Attendance History retrieval...")
    history_res = requests.get(f"{BASE_URL}/student/attendance/history", headers=headers)
    assert history_res.status_code == 200, f"Get history failed: {history_res.text}"
    history_data = history_res.json()["data"]
    print(f"  -> Total days recorded: {history_data['totalDays']}")
    print(f"  -> Total hours formatted: {history_data['totalHoursFormatted']}")
    print(f"  -> Completed days: {history_data['completedDays']}")
    print(f"  -> Number of records returned: {len(history_data['records'])}")
    assert len(history_data["records"]) > 0, "Expected at least 1 record in history"
    print("  [PASS] Attendance history verified successfully!")

    print("\n=======================================================")
    print("🎉 ALL US21 ATTENDANCE API & VALIDATION TESTS PASSED! 🎉")
    print("=======================================================")

if __name__ == "__main__":
    run_tests()
