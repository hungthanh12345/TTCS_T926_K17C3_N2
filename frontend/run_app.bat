@echo off
chcp 65001 >nul
cd /d "%~dp0"
if exist "..\backend" cd /d "%~dp0.."
title HE THONG SO HOA & QUAN LY THUC TAP SINH (SPRINT 2)
cls

echo ===============================================================================
echo        HE THONG SO HOA & QUAN LY THUC TAP SINH (SPRINT 2)
echo ===============================================================================
echo.
echo [1/3] Dang khoi dong Backend ASP.NET Core...
start "Backend - ASP.NET Core" cmd /k "cd backend && dotnet run --urls=http://localhost:5000"

echo [2/3] Dang khoi dong Frontend React Vite...
start "Frontend - React Vite" cmd /k "cd frontend && npm run dev"

echo [3/3] Dang cho cac tien trinh khoi dong (5 giay)...
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
echo Kiem tra hai terminal Backend va Frontend de xac nhan tien trinh da khoi dong.
echo Cau hinh MySQL va JWT qua .NET User Secrets truoc khi chay backend.
echo Vui long khong dong cac cua so Backend va Frontend.
echo Nhan phim bat ky de dong console dieu khien nay...
pause >nul
