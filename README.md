# Hệ Thống Quản Lý Thực Tập Sinh (Internship Management System) - Nhóm 2

Backend RESTful API hoàn chỉnh phục vụ cho học phần Thực tập cơ sở (TTCS) - Sprint 1. Xây dựng bằng **ASP.NET Core Web API 8.0 (LTS)**, **Entity Framework Core (Pomelo MySQL Provider)**, **BCrypt password hashing**, và **JWT Authentication with Role-Based Access Control (RBAC)**.

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

## 🎯 2. Phạm Vi & User Stories Đã Hoàn Thành (Sprint 1)
* **US 39 (Admin)**: `POST /api/admin/users`, `GET /api/admin/users` - Admin tạo và quản lý tài khoản cho HR, Mentors, và Students với mã hóa BCrypt.
* **US 40 (All Roles)**: `POST /api/auth/login` - Xác thực email & password, sinh JWT Token chứa claim `userId`, `email`, `role`, middleware phân quyền RBAC (`ROLE_ADMIN`, `ROLE_HR`, `ROLE_MENTOR`, `ROLE_STUDENT`).
* **US 1 (HR)**: `POST /api/hr/students` - HR tạo mới hồ sơ thực tập sinh (Intern Profile) kèm kiểm tra mã sinh viên và ràng buộc.
* **US 2 (HR)**: `GET /api/hr/students/{id}`, `PUT /api/hr/students/{id}` - HR xem chi tiết và cập nhật thông tin hồ sơ thực tập sinh.
* **US 3 (HR)**: `GET /api/hr/students/search` - HR tìm kiếm và lọc danh sách thực tập sinh theo Trường (University), Ngành học (Major), Họ tên, có phân trang (pagination).
* **US 29 (HR)**: `POST /api/hr/mentors`, `GET /api/hr/mentors` - HR tạo hồ sơ Mentor liên kết với tài khoản user và xem danh sách Mentor kèm số lượng thực tập sinh hướng dẫn.
* **US 30 (HR)**: `PUT /api/hr/students/{studentId}/assign-mentor` - HR phân công hoặc đổi Mentor phụ trách thực tập sinh.

---

## ⚙️ 3. Technical Stack & Architecture

- **Platform / Framework:** ASP.NET Core Web API 8.0 (.NET 8 LTS)
- **Database:** MySQL 8.0 (`internship_management`)
- **ORM / Data Access:** Entity Framework Core 8.0 with `Pomelo.EntityFrameworkCore.MySql`
- **Security & Cryptography:** 
  - JWT (JSON Web Token) with custom claims (`userId`, `email`, `role`, `status`)
  - Password Hashing: `BCrypt.Net-Next` (adaptive work factor 11)
- **API Documentation & Testing:** Swagger / OpenAPI 3.0 with interactive Bearer token authorization
- **Design Pattern:** Layered Enterprise Architecture:
  - **Controllers:** Request routing, input validation binding, HTTP status code management.
  - **Services:** Business logic, security enforcement, authorization checks, and transaction coordination.
  - **Repositories:** Data access abstraction, EF Core queries, filtering, search, and pagination.
  - **Middleware:** Centralized exception handling (`GlobalExceptionMiddleware`) and custom JWT challenge/forbidden event formatting.
  - **Data / Entities:** Clean ORM mapping to existing MySQL tables with relationship constraints.
  - **DTOs:** Typed Request/Response contracts with standard Data Annotations validation.

---

## 2. Database Schema & Tables

The system directly maps and enforces constraints on the 4 primary MySQL tables in `internship_management`:

### `roles`
| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | INT | PK, AUTO_INCREMENT | Unique Role ID |
| `name` | VARCHAR(50) | UNIQUE, NOT NULL | `ROLE_ADMIN`, `ROLE_HR`, `ROLE_MENTOR`, `ROLE_STUDENT` |
| `description` | VARCHAR(255) | NULLABLE | Description of role responsibilities |
| `created_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Record creation timestamp |
| `updated_at` | TIMESTAMP | ON UPDATE CURRENT_TIMESTAMP | Record update timestamp |

### `users`
| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | INT | PK, AUTO_INCREMENT | Unique User ID |
| `email` | VARCHAR(150) | UNIQUE, NOT NULL | User login email |
| `password_hash` | VARCHAR(255) | NOT NULL | BCrypt hashed password |
| `role_id` | INT | FK -> `roles(id)` (RESTRICT) | Assigned system role |
| `status` | ENUM | `'ACTIVE','INACTIVE','LOCKED'` | Account access status |
| `created_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Account creation timestamp |
| `updated_at` | TIMESTAMP | ON UPDATE CURRENT_TIMESTAMP | Account update timestamp |

### `mentors`
| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | INT | PK, AUTO_INCREMENT | Unique Mentor ID |
| `user_id` | INT | UNIQUE, FK -> `users(id)` (CASCADE) | Linked mentor user account |
| `full_name` | VARCHAR(100) | NOT NULL | Full name of mentor |
| `phone_number` | VARCHAR(20) | NULLABLE | Contact telephone |
| `department` | VARCHAR(100) | NOT NULL | Assigned department |
| `specialization` | VARCHAR(150) | NULLABLE | Technical domain / expertise |
| `created_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Record creation timestamp |
| `updated_at` | TIMESTAMP | ON UPDATE CURRENT_TIMESTAMP | Record update timestamp |

### `students`
| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | INT | PK, AUTO_INCREMENT | Unique Student ID |
| `user_id` | INT | UNIQUE, NULLABLE, FK -> `users(id)` (SET NULL) | Linked user account (if activated) |
| `student_code` | VARCHAR(50) | UNIQUE, NOT NULL | Unique Student Matriculation Code |
| `full_name` | VARCHAR(100) | NOT NULL | Full name of student intern |
| `phone_number` | VARCHAR(20) | NULLABLE | Contact telephone |
| `university` | VARCHAR(150) | NOT NULL | University / Academic institution |
| `major` | VARCHAR(100) | NOT NULL | Academic major / field of study |
| `mentor_id` | INT | NULLABLE, FK -> `mentors(id)` (SET NULL) | Assigned mentor supervisor |
| `created_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Record creation timestamp |
| `updated_at` | TIMESTAMP | ON UPDATE CURRENT_TIMESTAMP | Record update timestamp |

---

## 3. Role-Based Access Control (RBAC) Matrix

| Endpoint | Method | Allowed Roles | Description |
|---|---|---|---|
| `/api/auth/login` | `POST` | *Public (All)* | Authenticate credentials and acquire JWT |
| `/api/admin/users` | `GET` | `ROLE_ADMIN` | List all registered system users and roles |
| `/api/admin/users` | `POST` | `ROLE_ADMIN` | Create new system account (HR, Mentor, Student) |
| `/api/hr/mentors` | `GET` | `ROLE_HR`, `ROLE_ADMIN` | List all mentor profiles with student counts |
| `/api/hr/mentors` | `POST` | `ROLE_HR`, `ROLE_ADMIN` | Create a new mentor profile |
| `/api/hr/students` | `POST` | `ROLE_HR`, `ROLE_ADMIN` | Create a new student profile |
| `/api/hr/students/{id}` | `GET` | `ROLE_HR`, `ROLE_ADMIN` | Get detailed student profile |
| `/api/hr/students/{id}` | `PUT` | `ROLE_HR`, `ROLE_ADMIN` | Update student profile information |
| `/api/hr/students/search` | `GET` | `ROLE_HR`, `ROLE_ADMIN` | Search & filter students with pagination |
| `/api/hr/students/{studentId}/assign-mentor` | `PUT` | `ROLE_HR`, `ROLE_ADMIN` | Assign or reassign mentor to student |

---

## 4. API Endpoints Specification

### A. Authentication
#### `POST /api/auth/login`
- **Request Body:**
  ```json
  {
    "email": "hung.nt.admin@gmail.com",
    "password": "Admin@123"
  }
  ```
- **Response (200 OK):**
  ```json
  {
    "success": true,
    "message": "Login successful.",
    "data": {
      "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
      "tokenType": "Bearer",
      "expiresIn": 28800,
      "expiresAt": "2026-09-24T18:03:46Z",
      "user": {
        "userId": 1,
        "email": "hung.nt.admin@gmail.com",
        "role": "ROLE_ADMIN",
        "status": "ACTIVE"
      }
    },
    "errors": null,
    "timestamp": "2026-09-24T10:03:46Z"
  }
  ```

---

### B. User & Account Management (Admin)
#### `POST /api/admin/users`
- **Headers:** `Authorization: Bearer <ADMIN_JWT>`
- **Request Body:**
  ```json
  {
    "email": "elena.mentor@system.local",
    "password": "Password@123",
    "roleName": "ROLE_MENTOR",
    "status": 0
  }
  ```
- **Response (201 Created):**
  ```json
  {
    "success": true,
    "message": "User account created successfully.",
    "data": {
      "id": 15,
      "email": "elena.mentor@system.local",
      "roleId": 3,
      "roleName": "ROLE_MENTOR",
      "status": "ACTIVE",
      "createdAt": "2026-09-24T10:04:14Z",
      "updatedAt": "2026-09-24T10:04:14Z"
    }
  }
  ```

#### `GET /api/admin/users`
- **Headers:** `Authorization: Bearer <ADMIN_JWT>`
- **Response (200 OK):** Returns array of all system user accounts.

---

### C. Student Profile Management (HR)
#### `POST /api/hr/students`
- **Headers:** `Authorization: Bearer <HR_JWT>`
- **Request Body:**
  ```json
  {
    "studentCode": "STU2026888",
    "fullName": "Michael Scott",
    "phoneNumber": "0911223344",
    "university": "Hanoi University of Science and Technology",
    "major": "Computer Science",
    "userId": 16
  }
  ```
- **Response (201 Created):** Returns created student entity with populated relationships.

#### `GET /api/hr/students/{id}`
- **Headers:** `Authorization: Bearer <HR_JWT>`
- **Response (200 OK):** Returns single student profile with mentor and user summaries.

#### `PUT /api/hr/students/{id}`
- **Headers:** `Authorization: Bearer <HR_JWT>`
- **Request Body:** Updated fields (`fullName`, `phoneNumber`, `university`, `major`, etc.)
- **Response (200 OK):** Returns updated student profile.

#### `GET /api/hr/students/search`
- **Query Parameters:**
  - `university` (string, optional)
  - `major` (string, optional)
  - `fullName` (string, optional)
  - `keyword` (string, optional - searches name, code, university, major)
  - `page` (int, default: 1)
  - `pageSize` (int, default: 10, max: 100)
- **Response (200 OK):**
  ```json
  {
    "success": true,
    "message": "Filtered students retrieved successfully.",
    "data": {
      "items": [ ... ],
      "totalItems": 15,
      "pageNumber": 1,
      "pageSize": 10,
      "totalPages": 2,
      "hasPreviousPage": false,
      "hasNextPage": true
    }
  }
  ```

#### `PUT /api/hr/students/{studentId}/assign-mentor`
- **Headers:** `Authorization: Bearer <HR_JWT>`
- **Request Body:**
  ```json
  {
    "mentorId": 2
  }
  ```
- **Response (200 OK):** Returns updated student profile with assigned mentor details.

---

### D. Mentor Management (HR)
#### `POST /api/hr/mentors`
- **Headers:** `Authorization: Bearer <HR_JWT>`
- **Request Body:**
  ```json
  {
    "userId": 15,
    "fullName": "Elena Rostova",
    "phoneNumber": "0988776655",
    "department": "AI & Cloud Engineering",
    "specialization": "Distributed Systems & Machine Learning"
  }
  ```
- **Response (201 Created):** Returns created mentor record.

#### `GET /api/hr/mentors`
- **Headers:** `Authorization: Bearer <HR_JWT>`
- **Response (200 OK):** Returns list of all mentors with active assigned student counts.

---

## 5. Standard Error Format & HTTP Status Codes

All API errors conform to a unified JSON contract:
```json
{
  "success": false,
  "message": "Brief description of the failure condition.",
  "data": null,
  "errors": { ... },
  "timestamp": "2026-09-24T10:04:15Z"
}
```

- **`200 OK`**: Operation succeeded.
- **`201 Created`**: Resource successfully created.
- **`400 Bad Request`**: Validation error, malformed body, or business rule violation.
- **`401 Unauthorized`**: Missing, invalid, or expired Bearer token; invalid credentials.
- **`403 Forbidden`**: Authenticated user lacks required role permissions.
- **`404 Not Found`**: Requested resource or record does not exist.
- **`409 Conflict`**: Unique key conflict (e.g. duplicate email, student code, or mentor profile).
- **`500 Internal Server Error`**: Unexpected system failure caught by middleware.

---

## 6. How to Run & Test Locally

### 1. Prerequisites
- .NET 8.0 SDK or higher
- Node.js (v18+) & npm
- MySQL Server 8.0 running on `localhost:3306` with database `internship_management`

### 2. Database Initialization
```powershell
mysql -u root -p123456 -e "CREATE DATABASE IF NOT EXISTS internship_management CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"
mysql --default-character-set=utf8mb4 -u root -p123456 -e "source schema.sql"
mysql --default-character-set=utf8mb4 -u root -p123456 -e "source seed_data.sql"
```

### 3. Run Backend API
```powershell
dotnet run --launch-profile http
```
The server will start listening at:
- **API Base:** `http://localhost:5000`
- **Swagger Documentation:** `http://localhost:5000/swagger`

### 4. Run Frontend Web
```powershell
cd frontend
npm install
npm run dev
```
Web client will start at:
- **Web App:** `http://localhost:5173/`

### 5. Automated Tests
```powershell
python test_suite.py
python validate_all_requirements.py
```

### 6. Default Seed Test Credentials (All Roles: Admin@123)
- **Admin Account:** `hung.nt.admin@gmail.com` / `Admin@123`
- **HR Account:** `customer.hr@company.com` / `Admin@123`
- **Mentor Account:** `tung.nk@gmail.com` / `Admin@123`
- **Student Account:** `hung.dm@gmail.com` / `Admin@123`
