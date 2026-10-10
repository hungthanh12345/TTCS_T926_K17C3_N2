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
5. Sau 1-2 phút, trạng thái chuyển sang **Running**. Tạo/chọn schema ứng dụng có tên chính xác `internship_management`; dùng service URI trỏ tới schema đó hoặc các thông số:
   * **Host**: `mysql-xxx.aivencloud.com`
   * **Port**: `12345`
   * **User**: `avnadmin`
   * **Password**: `******`
   * **Database**: `internship_management`
6. Kết nối bằng DBeaver hoặc MySQL Workbench hoặc Drizzle Studio bằng Service URI:
   * Chọn một database mới, trống và riêng cho ứng dụng.
   * Áp dụng `migrations/000_initial_core_schema.sql`, sau đó mọi migration trong `backend/migrations/` theo thứ tự tên file.
   * Không chạy `schema.sql` hoặc `seed_data.sql` trên cloud: đây là script reset/xóa dữ liệu mẫu dành cho database local dùng một lần.

### Lựa chọn B: TiDB Cloud Serverless (MySQL Compatible - 25GB Free Forever)
1. Đăng ký tài khoản tại [TiDB Cloud](https://tidbcloud.com/).
2. Tạo cụm **Serverless Tier** (Miễn phí 25GB, tương thích 100% chuẩn MySQL 8.0).
3. Lấy chuỗi kết nối trỏ đến schema `internship_management` và áp dụng baseline cùng các migration theo thứ tự như trên. Không tạo schema khác cho Sprint 2 hoặc theo từng role. Không nạp seed tài khoản mẫu lên cloud.

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
   | `ConnectionStrings__DefaultConnection` | `Server=host;Port=port;Database=internship_management;User=user;Password=pass;CharSet=utf8mb4;SslMode=Required;AllowPublicKeyRetrieval=True;` *(hoặc URI MySQL trỏ tới `internship_management`; production backend luôn bắt buộc TLS)* | Kết nối MySQL |
   | `CORS_ALLOWED_ORIGINS` | `https://your-project.vercel.app` | Danh sách chính xác các origin, phân tách bằng dấu phẩy; thêm từng domain preview riêng nếu cần. Không dùng wildcard. |
   | `ASPNETCORE_ENVIRONMENT` | `Production` | Chế độ Production |
   | `PORT` | `8080` | Port container |
   | `JWT_SECRET_KEY` | Generate a unique random secret of at least 32 UTF-8 bytes (for example, `openssl rand -base64 48`). Never commit or reuse a public development key. | Required for Production |
   | `JWT_ISSUER` | `InternshipManagementApi` | Issuer |
   | `JWT_AUDIENCE` | `InternshipManagementClient` | Audience |
6. Nhấn **Create Web Service**.
7. Render sẽ tự động kéo Dockerfile, build multi-stage SDK và khởi chạy container.
8. Khi build hoàn tất (sau ~2-3 phút), bạn sẽ nhận được URL Backend công khai:
   👉 **`https://internship-management-api.onrender.com`**
9. Kiểm tra:
   * Mở `https://internship-management-api.onrender.com` -> Giao diện Swagger UI tương tác trực quan.
   * Mở `https://internship-management-api.onrender.com/health` -> Trả HTTP 200 khi API kết nối được MySQL hoặc HTTP 503 khi database chưa sẵn sàng. Phản hồi không chứa thông tin kết nối.

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

## BƯỚC 4: KIỂM TRA DỊCH VỤ

Sau khi hoàn tất, kiểm tra `/health` và đăng nhập bằng các tài khoản đã được cấp riêng cho production. Không dùng tài khoản, mật khẩu hoặc dữ liệu mẫu từ seed script trên hệ thống công khai.

---

## CẤU HÌNH SMTP CHO US08

Email kết quả xét duyệt được gửi qua SMTP STARTTLS (Gmail dùng cổng 587). Cấu hình các giá trị sau bằng environment variables trên môi trường deploy:

| Environment variable | Nội dung |
|---|---|
| `SMTP_HOST` | SMTP host, ví dụ `smtp.gmail.com` |
| `SMTP_PORT` | SMTP port, thường là `587` |
| `SMTP_USER` | Tài khoản SMTP |
| `SMTP_PASS` | SMTP password hoặc Gmail App Password |
| `MAIL_FROM` | Địa chỉ người gửi đã được SMTP provider xác thực |

Khi chạy local, lưu bí mật bằng .NET User Secrets trong thư mục `backend` (không đưa các giá trị này vào Git):

    dotnet user-secrets set 'Smtp:Host' 'smtp.gmail.com' --project backend\InternshipManagementApi.csproj
    dotnet user-secrets set 'Smtp:Port' '587' --project backend\InternshipManagementApi.csproj
    dotnet user-secrets set 'Smtp:User' '<SMTP account>' --project backend\InternshipManagementApi.csproj
    dotnet user-secrets set 'Smtp:Password' '<SMTP app password>' --project backend\InternshipManagementApi.csproj
    dotnet user-secrets set 'Mail:From' '<verified sender address>' --project backend\InternshipManagementApi.csproj

The email worker polls `email_logs` in the background. It records the recipient snapshot from `users.email`, marks successful deliveries `SENT`, and retries a failed first send up to three additional times before marking the log `FAILED`.
