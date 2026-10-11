import requests
import os
import sys

BASE_URL = os.getenv("VALIDATION_API_BASE_URL", "http://localhost:5000").rstrip("/")

def login(email, password):
    url = f"{BASE_URL}/api/Auth/login"
    res = requests.post(url, json={"email": email, "password": password})
    if res.status_code == 200:
        return res.json()["data"]["token"]
    return None

def run_tests():
    print("Testing Attendance Reports API (US22)...")
    
    hr_email = "customer.hr@company.com"
    hr_password = "password123" # Giả định pass môi trường dev
    token = login(hr_email, hr_password)
    
    if not token:
        print("[-] Lỗi đăng nhập HR. Test bị hủy.")
        return

    headers = {"Authorization": f"Bearer {token}"}
    
    print("[*] Gọi API lấy báo cáo chấm công (không filter)...")
    res = requests.get(f"{BASE_URL}/api/AttendanceReports", headers=headers)
    if res.status_code == 200:
        data = res.json()["data"]["items"]
        print(f"[+] Lấy thành công báo cáo chấm công (Số lượng: {len(data)})")
    else:
        print(f"[-] Lỗi gọi API báo cáo: {res.text}")
        sys.exit(1)

    print("[*] Gọi API lấy báo cáo chấm công (khoảng thời gian)...")
    res = requests.get(f"{BASE_URL}/api/AttendanceReports?startDate=2026-10-01&endDate=2026-10-15", headers=headers)
    if res.status_code == 200:
        data = res.json()["data"]["items"]
        print(f"[+] Lọc ngày thành công (Số lượng: {len(data)})")
    else:
        print(f"[-] Lỗi gọi API lọc ngày: {res.text}")
        sys.exit(1)

    print("[*] Gọi API lỗi ngày kết thúc < ngày bắt đầu...")
    res = requests.get(f"{BASE_URL}/api/AttendanceReports?startDate=2026-10-15&endDate=2026-10-01", headers=headers)
    if res.status_code == 400:
        print("[+] Bắt lỗi ngày hợp lệ thành công (400 Bad Request)")
    else:
        print(f"[-] Lỗi: Không bắt được invalid date range, Status code: {res.status_code}")
        sys.exit(1)
        
    print("Toàn bộ test báo cáo chấm công đều pass!")

if __name__ == "__main__":
    run_tests()
