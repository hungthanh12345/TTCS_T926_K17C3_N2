# HƯỚNG DẪN TRIỂN KHAI HỆ THỐNG LÊN CLOUD MIỄN PHÍ
## (Production Cloud Deployment Guide: Render / Railway, Free MySQL & Vercel)

Dự án **Hệ Thống Quản Lý Thực Tập Sinh (Full-Stack ASP.NET Core 8 Web API + React 19 Vite + MySQL)** đã được container hóa và cấu hình sẵn sàng 100% để triển khai lên các nền tảng Cloud miễn phí (Free-tier) với chứng chỉ HTTPS tự động, cho phép chia sẻ đường dẫn trực tiếp (Live URL) cho người dùng truy cập mọi lúc mọi nơi.

---

## KIẾN TRÚC TRIỂN KHAI (ARCHITECTURE)

```
[ Người Dùng / Trình Duyệt ]
           │
           ▼
[ Vercel Edge Network (HTTPS) ]
React 19 + Vite SPA (Vercel Routing Rewrite qua vercel.json)
           │
           │ RESTful API Calls (CORS Enabled)
           ▼
[ Render / Railway Cloud (HTTPS) ]
ASP.NET Core 8 Web API (Docker Linux Container)
           │
           │ TLS / SSL MySQL Connection
           ▼
[ Free Cloud MySQL ]
Aiven for MySQL / TiDB Cloud / Railway Managed MySQL
```

---

## BƯỚC 1: TẠO CƠ SỞ DỮ LIỆU MYSQL CLOUD (MIỄN PHÍ 100%)

Vì gói Free của Render chỉ cung cấp PostgreSQL, bạn có thể lựa chọn 1 trong 3 nhà cung cấp MySQL Cloud miễn phí tốt nhất hiện nay:

### Lựa chọn A: Aiven for MySQL (Khuyên dùng - 100% Free Forever)
1. Truy cập [Aiven Console](https://console.aiven.io/) và đăng ký tài khoản miễn phí.
2. Chọn **Create Service** -> Chọn **MySQL** -> Chọn gói **Free** (5GB Storage, 1 CPU).
3. Chọn Region gần Việt Nam nhất (ví dụ: `ap-southeast-1` Singapore).
4. Nhấn **Create Service**.
5. Sau 1-2 phút, trạng thái chuyển sang **Running**. Bạn sao chép **Service URI** (dạng `mysql://avnadmin:password@mysql-xxx.aivencloud.com:12345/defaultdb?ssl-mode=REQUIRED`) hoặc các thông số:
   * **Host**: `mysql-xxx.aivencloud.com`
   * **Port**: `12345`
   * **User**: `avnadmin`
   * **Password**: `******`
   * **Database**: `defaultdb`
6. Kết nối bằng DBeaver hoặc MySQL Workbench hoặc Drizzle Studio bằng Service URI:
   * Mở file [`schema.sql`](./schema.sql) -> Thực thi toàn bộ lệnh để tạo 6 bảng.
   * Mở file [`seed_data.sql`](./seed_data.sql) -> Thực thi để nạp dữ liệu mẫu ban đầu (Admin, HR, Mentor, 10 Sinh viên).

### Lựa chọn B: TiDB Cloud Serverless (MySQL Compatible - 25GB Free Forever)
1. Đăng ký tài khoản tại [TiDB Cloud](https://tidbcloud.com/).
2. Tạo cụm **Serverless Tier** (Miễn phí 25GB, tương thích 100% chuẩn MySQL 8.0).
3. Lấy chuỗi kết nối và thực thi `schema.sql` cùng `seed_data.sql`.

### Lựa chọn C: Railway MySQL
1. Đăng ký tài khoản tại [Railway.app](https://railway.app/).
2. Nhấn **New Project** -> Chọn **Provision MySQL**.
3. Railway tự động cấp 1 instance MySQL và biến môi trường `MYSQL_URL`.

---

## BƯỚC 2: DEPLOY BACKEND .NET 8 LÊN RENDER HOẶC RAILWAY

### Cách 1: Triển khai trên Render (Khuyên dùng)
1. Truy cập [Render Dashboard](https://dashboard.render.com/) -> Đăng nhập bằng GitHub.
2. Nhấn **New +** -> Chọn **Web Service**.
3. Kết nối với kho mã nguồn GitHub của bạn: `https://github.com/hungthanh12345/TTCS_T926_K17C3_N2.git`.
4. Cấu hình thông số:
   * **Name**: `internship-management-api`
   * **Region**: `Singapore (Southeast Asia)`
   * **Branch**: `main`
   * **Root Directory**: Để trống (hoặc `.`)
   * **Runtime**: `Docker`
   * **Instance Type**: `Free`
5. Trong mục **Environment Variables**, thêm các biến sau:
   | Key | Value | Ghi chú |
   |---|---|---|
   | `ConnectionStrings__DefaultConnection` | `Server=host;Port=port;Database=defaultdb;User=user;Password=pass;CharSet=utf8mb4;SslMode=Preferred;AllowPublicKeyRetrieval=True;` *(hoặc URI `mysql://...` từ Aiven)* | Kết nối MySQL |
   | `CORS_ALLOWED_ORIGINS` | `https://*.vercel.app,http://localhost:5173` | Hỗ trợ Vercel Frontend |
   | `ASPNETCORE_ENVIRONMENT` | `Production` | Chế độ Production |
   | `PORT` | `8080` | Port container |
   | `JWT_SECRET_KEY` | `InternshipManagementSystem_SuperSecretSecureKey_2026_JWT_Production_Key!` | Khóa bí mật JWT |
   | `JWT_ISSUER` | `InternshipManagementApi` | Issuer |
   | `JWT_AUDIENCE` | `InternshipManagementClient` | Audience |
6. Nhấn **Create Web Service**.
7. Render sẽ tự động kéo Dockerfile, build multi-stage SDK và khởi chạy container.
8. Khi build hoàn tất (sau ~2-3 phút), bạn sẽ nhận được URL Backend công khai:
   👉 **`https://internship-management-api.onrender.com`**
9. Kiểm tra:
   * Mở `https://internship-management-api.onrender.com` -> Giao diện Swagger UI tương tác trực quan.
   * Mở `https://internship-management-api.onrender.com/health` -> Trả về `{"status":"Healthy"}`.

---

## BƯỚC 3: DEPLOY FRONTEND REACT 19 LÊN VERCEL

1. Truy cập [Vercel Dashboard](https://vercel.com/) -> Đăng nhập bằng GitHub.
2. Nhấn **Add New...** -> Chọn **Project**.
3. Tìm và chọn repository: `TTCS_T926_K17C3_N2`.
4. Trong giao diện cấu hình dự án (**Configure Project**):
   * **Framework Preset**: Chọn **Vite**.
   * **Root Directory**: Bấm nút **Edit** và chọn thư mục **`frontend`**.
   * **Build Command**: `npm run build` (Mặc định).
   * **Output Directory**: `dist` (Mặc định).
5. Mở mục **Environment Variables** và cấu hình:
   | Name | Value |
   |---|---|
   | `VITE_API_BASE_URL` | `https://internship-management-api.onrender.com/api` *(Thay bằng URL Render thật ở Bước 2)* |
6. Nhấn **Deploy**.
7. Quá trình build Vite diễn ra cực nhanh (~30 giây). Vercel sẽ cấp domain công khai:
   👉 **`https://ttcs-t926-k17c3-n2.vercel.app`** (hoặc domain tùy chỉnh).

---

## BƯỚC 4: XÁC THỰC VÀ BÀN GIAO TRẢI NGHIỆM

Sau khi hoàn tất, hệ thống đã hoạt động trực tuyến 24/7. Bạn có thể gửi đường dẫn Vercel cho bất kỳ ai để đăng nhập và trải nghiệm đầy đủ các tính năng:

### Tài khoản mẫu thử nghiệm (Mật khẩu chung: `Admin@123`):
| Vai trò | Email đăng nhập | Mật khẩu | Tính năng chính |
|---|---|---|---|
| **Quản trị viên (Admin)** | `admin@company.com` | `Admin@123` | Quản lý người dùng, phân quyền hệ thống |
| **Quản lý Nhân sự (HR)** | `customer.hr@company.com` | `Admin@123` | Quản lý sinh viên thực tập, phân công mentor, thống kê |
| **Mentor Doanh nghiệp** | `tung.nk@gmail.com` | `Admin@123` | Theo dõi và hướng dẫn sinh viên trực thuộc |
| **Sinh viên Thực tập** | `hung.dm@gmail.com` | `Admin@123` | Xem thông tin thực tập cá nhân và mentor phụ trách |

> **Mẹo (Quick Login)**: Tại trang Đăng nhập, người dùng chỉ cần click vào các thẻ vai trò ("Quản trị viên", "Quản lý Nhân sự", "Mentor", "Sinh viên") để hệ thống tự động điền thông tin và đăng nhập tức thì!
