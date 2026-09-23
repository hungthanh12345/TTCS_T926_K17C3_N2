# Hệ Thống Quản Lý Thực Tập Sinh (Intern Management System) - Nhóm 2

Dự án cơ sở dữ liệu MySQL phục vụ Sprint 1 cho học phần Thực tập cơ sở (TTCS).

---

## 📌 1. Thông Tin Nhóm & Phân Vai
* **Nhóm**: Nhóm 2 - Lớp K17C3
* **Product Owner / Mentor**: Nguyễn Khánh Tùng (`tung.nk@gmail.com`)
* **Admin hệ thống**: Nguyễn Thành Hưng (`hung.nt.admin@gmail.com`)
* **HR Doanh nghiệp**: Khách hàng đại diện (`customer.hr@company.com`)
* **Thành viên Thực tập sinh (ICTU)**:
  1. Dương Minh Hưng (`hung.dm@gmail.com` - `SV2026002`)
  2. Hoàng Thanh Hùng (`hung.ht@gmail.com` - `SV2026003`)
  3. Trương Đình Giang (`giang.td@gmail.com` - `SV2026004`)
  4. Dương Hải Dương (`duong.dh@gmail.com` - `SV2026005`)
  5. Diệp Đình Hân (`han.dd@gmail.com` - `SV2026006`)
  6. Bùi Ngọc Huân (`huan.bn@gmail.com` - `SV2026007`)
  7. Nguyễn Hồng Hải (`hai.nh@gmail.com` - `SV2026008`)
  8. Phạm Hải Hướng (`huong.ph@gmail.com` - `SV2026009`)
  9. Nguyễn Thị Giang (`giang.nt@gmail.com` - `SV2026010`)

---

## 🎯 2. Phạm Vi & User Stories Sprint 1
* **US 39**: Admin tạo tài khoản cho HR, Mentors, và Students.
* **US 40**: Xác thực, Đăng nhập, Sinh JWT Token và Phân quyền RBAC.
* **US 1**: HR tạo mới hồ sơ thực tập sinh (Intern Profile).
* **US 2**: HR cập nhật thông tin hồ sơ thực tập sinh.
* **US 3**: HR tìm kiếm và lọc danh sách thực tập sinh theo Trường (University) và Ngành học (Major).
* **US 29**: HR tạo hồ sơ Mentor.
* **US 30**: HR phân công Mentor phụ trách thực tập sinh.

---

## 🗄️ 3. Cấu Trúc Cơ Sở Dữ Liệu
* **Database Name**: `internship_management`
* **Character Set / Collation**: `utf8mb4` / `utf8mb4_unicode_ci`
* **Storage Engine**: `InnoDB`

### Danh sách các bảng:
1. `roles`: Lưu danh sách các vai trò quyền hạn (`ROLE_ADMIN`, `ROLE_HR`, `ROLE_MENTOR`, `ROLE_STUDENT`).
2. `users`: Lưu thông tin đăng nhập, mật khẩu mã hóa BCrypt, khóa ngoại liên kết tới `roles`, trạng thái tài khoản.
3. `mentors`: Lưu thông tin hồ sơ Mentor, liên kết 1-1 với `users`, phòng ban, chuyên môn.
4. `students`: Lưu thông tin sinh viên thực tập, mã SV, trường ĐH, ngành học, khóa ngoại liên kết tới `users` và `mentors` (người hướng dẫn).

---

## 🚀 4. Hướng Dẫn Cài Đặt Cho Thành Viên (MySQL Workbench)

### Bước 1: Clone dự án về máy
```bash
git clone https://github.com/hungthanh12345/TTCS_T926_K17C3_N2.git
cd TTCS_T926_K17C3_N2
```

### Bước 2: Chạy khởi tạo CSDL
1. Mở **MySQL Workbench**, kết nối tới MySQL Server Local (`localhost:3306`).
2. Mở file `schema.sql` và nhấn biểu tượng ⚡ (**Execute**) để tạo Database `internship_management` và 4 bảng cùng các Index hiệu năng.
3. Mở file `seed_data.sql` và nhấn biểu tượng ⚡ (**Execute**) để nạp dữ liệu mẫu ban đầu (Roles, Users, Mentor, Students).

### Bước 3: Thông tin tài khoản kiểm thử mặc định
* **Mật khẩu chung cho tất cả tài khoản mẫu**: `Admin@123`
* **Admin**: `hung.nt.admin@gmail.com`
* **HR**: `customer.hr@company.com`
* **Mentor**: `tung.nk@gmail.com`
* **Students**: Các email `@gmail.com` theo danh sách trên.