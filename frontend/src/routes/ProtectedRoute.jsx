import React from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { ShieldAlert, ArrowLeft, LogOut } from 'lucide-react';

/**
 * Role-Based Access Control (RBAC) Route Guard Component
 */
export const ProtectedRoute = ({ children, allowedRoles }) => {
  const { user, isAuthenticated, isLoading, logout } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-slate-900 text-white">
        <div className="flex flex-col items-center gap-3">
          <div className="w-10 h-10 border-4 border-indigo-500 border-t-transparent rounded-full animate-spin"></div>
          <p className="text-sm font-medium text-slate-300">Đang xác thực quyền truy cập hệ thống...</p>
        </div>
      </div>
    );
  }

  // Chưa đăng nhập -> chuyển hướng về Login
  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  // Kiểm tra quyền hạn vai trò (Role)
  if (allowedRoles && allowedRoles.length > 0) {
    const isAllowed = allowedRoles.includes(user?.role);
    if (!isAllowed) {
      // Xác định trang chủ tương ứng của vai trò hiện tại
      let homePath = '/login';
      if (user?.role === 'ROLE_ADMIN') homePath = '/admin/users';
      else if (user?.role === 'ROLE_HR') homePath = '/hr/students';
      else if (user?.role === 'ROLE_MENTOR') homePath = '/mentor/students';
      else if (user?.role === 'ROLE_STUDENT') homePath = '/student/profile';

      const roleLabels = {
        ROLE_ADMIN: 'Quản trị viên (Admin)',
        ROLE_HR: 'Quản lý Nhân sự (HR)',
        ROLE_MENTOR: 'Mentor Doanh nghiệp',
        ROLE_STUDENT: 'Sinh viên Thực tập',
      };

      const handleLoginAnother = () => {
        logout(true);
        navigate('/login', { replace: true });
      };

      return (
        <div className="min-h-screen flex items-center justify-center bg-slate-950 p-4">
          <div className="max-w-md w-full bg-slate-900/90 backdrop-blur-xl rounded-3xl shadow-2xl border border-slate-800 p-8 text-center text-white">
            <div className="w-16 h-16 bg-rose-500/10 text-rose-400 border border-rose-500/20 rounded-2xl flex items-center justify-center mx-auto mb-5 shadow-inner">
              <ShieldAlert className="w-8 h-8" />
            </div>
            
            <h2 className="text-2xl font-bold tracking-tight mb-2">Giới Hạn Quyền Truy Cập</h2>
            <p className="text-slate-400 text-sm mb-6 leading-relaxed">
              Tài khoản hiện tại của bạn thuộc nhóm vai trò <span className="inline-block font-semibold px-2.5 py-0.5 rounded-full bg-indigo-500/20 text-indigo-300 border border-indigo-500/30 text-xs">{roleLabels[user?.role] || user?.role}</span> không được phép truy cập vào phân hệ này.
            </p>

            <div className="flex flex-col gap-3">
              <button
                type="button"
                onClick={() => navigate(homePath, { replace: true })}
                className="w-full inline-flex items-center justify-center gap-2 px-5 py-3 rounded-xl text-sm font-semibold text-white bg-indigo-600 hover:bg-indigo-500 active:scale-98 transition-all shadow-lg shadow-indigo-600/30 cursor-pointer"
              >
                <ArrowLeft className="w-4 h-4" /> Về Không Gian Làm Việc Của Tôi
              </button>

              <button
                type="button"
                onClick={handleLoginAnother}
                className="w-full inline-flex items-center justify-center gap-2 px-5 py-2.5 rounded-xl text-sm font-medium text-slate-400 hover:text-white hover:bg-slate-800 transition-all border border-slate-800 cursor-pointer"
              >
                <LogOut className="w-4 h-4" /> Đăng Nhập Với Tài Khoản Khác
              </button>
            </div>
          </div>
        </div>
      );
    }
  }

  return children;
};

export default ProtectedRoute;
