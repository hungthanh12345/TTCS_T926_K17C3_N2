@echo off
chcp 65001 >nul
cd /d "%~dp0"
title HE THONG SO HOA & QUAN LY THUC TAP SINH (SPRINT 1)
cls

echo ===============================================================================
echo        HE THONG SO HOA & QUAN LY THUC TAP SINH (SPRINT 1)
echo ===============================================================================
echo.
echo [1/3] Dang khoi dong Backend ASP.NET Core...
start "Backend - ASP.NET Core" cmd /k "cd backend && dotnet run --urls=http://localhost:5000"

echo [2/3] Dang khoi dong Frontend React Vite...
start "Frontend - React Vite" cmd /k "cd frontend && npm run dev"

echo [3/3] Cho may chu san sang (5 giay)...
ping 127.0.0.1 -n 6 >nul

echo Dang mo trinh duyet tai Giao dien Dang nhap...
start http://localhost:5173/login

echo.
echo ===============================================================================
echo                             MAY CHU DANG CHAY TAI:
echo ===============================================================================
echo  * Giao dien Dang nhap (Frontend):   http://localhost:5173/login
echo  * Tai lieu API (Swagger Docs):       http://localhost:5000/swagger
echo.
echo ===============================================================================
echo              TAI KHOAN DEMO (Mat khau mac dinh: Admin@123):
echo ===============================================================================
echo  * Quan tri vien (Admin):      hung.nt.admin@gmail.com
echo  * Nhan su (HR):               customer.hr@company.com
echo  * Mentor Doanh nghiep:        tung.nk@gmail.com
echo  * Sinh vien Thuc tap:         hung.dm@gmail.com
echo ===============================================================================
echo.
echo He thong da khoi dong thanh cong. Vui long khong dong cac cua so Backend va Frontend.
echo Nhan phim bat ky de dong console dieu khien nay...
pause >nul
