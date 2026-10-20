# Đồng bộ MySQL cho thành viên

Hướng dẫn này áp dụng cho source trên develop sau khi chạy git pull origin develop. Không dùng bộ cài mới trên database đã có dữ liệu.

## Cài database mới, trống

Bản cài đã được Owner chạy trên MySQL 8 với schema internship_sprint5_v2_verify_20261017. File đã kiểm thử:

- setup/20261017_new_database_install_v2_internship_sprint5_v2_verify_20261017.sql
- SHA-256: CD5128BC260B833439E527402FFDFDB26399B2DB4C0884C22F813780D60CA96C

Chỉ dùng đúng schema mới, trống có tên trên. File không tạo database. Tạo schema cục bộ trước bằng tài khoản quản trị MySQL:

    CREATE DATABASE internship_sprint5_v2_verify_20261017 CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

Sau đó dùng tài khoản cài đặt riêng, không dùng root; cấp SELECT, INSERT, CREATE, ALTER, INDEX, REFERENCES chỉ trên schema đó. Chạy setup/preflight_internship_sprint5_v2_verify_20261017.sql: cần thấy schema tồn tại và không có bảng/view. Nếu đã có object, dừng. Chạy file cài đúng một lần. Nếu lỗi giữa chừng, dừng và kiểm tra trạng thái; không chạy lại mù quáng vì DDL MySQL có thể commit từng phần. Dùng một tài khoản SELECT-only riêng để chạy checker.

Sau khi cài, dùng tài khoản SELECT-only chạy:

    powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\check-mysql-schema.ps1 -Schema internship_sprint5_v2_verify_20261017 -ConfirmTestSchema internship_sprint5_v2_verify_20261017

Checker hỏi connection string tại prompt cục bộ; không đưa secret vào command line hoặc Git. Báo cáo MySQL thực tế của Owner: 25 bảng, 226 cột, 38 FK, 55 index bắt buộc, 21 CHECK; không có thành phần bắt buộc MISSING/DIFFERENT. Hai index EXTRA được giữ. Kết quả này chứng minh bản cài trên schema thử nghiệm đã kiểm tra, không thay thế kiểm tra database khác.

## Database đã có dữ liệu

1. Chạy git pull origin develop.
2. Tạo backup và xác minh có thể đọc/khôi phục.
3. Dùng checker SELECT-only với schema Test được phép; lưu báo cáo cấu trúc.
4. Không chạy file cài mới. Không dùng một script nâng cấp chung dựa trên kết quả của schema khác.
5. Candidate review/20261017_internship_restore_check_us33_upgrade_review_only.sql chỉ tham khảo bằng chứng cũ của internship_restore_check; DDL đều nằm trong comment và file không phải script nâng cấp.
6. Chỉ tạo/apply SQL riêng sau khi rà soát report MISSING/DIFFERENT của đúng database và được Owner duyệt.

## Giới hạn migration

20261008065402_AddRegistrationReceivedEmailTemplate vẫn SOURCE_MISSING. File cài mới không tạo history row giả cho ID này; nó ghi nhận các migration có source được thực hiện trong artifact. Checker tiếp tục báo gap. Với database hiện có có ID thiếu trong __EFMigrationsHistory, không chạy dotnet ef database update hoặc script nâng cấp cho tới khi có reconciliation được review. Bản cài mới đã được đối chiếu cấu trúc bằng checker, nhưng việc EF tự động APPLY migration sau baseline chưa được kiểm thử runtime.

Hai SQL cài đặt/kiểm tra đều không tự sửa production. Cả cài mới lẫn nâng cấp vẫn cần xác nhận đúng schema, tài khoản và backup trước khi chạy thao tác ghi.