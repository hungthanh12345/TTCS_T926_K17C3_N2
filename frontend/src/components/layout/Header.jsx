import React, { useState, useRef, useEffect } from 'react';
import { useLocation, Link } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import {
  Menu,
  Bell,
  ChevronDown,
  LogOut,
  User,
  Settings,
  ChevronRight,
} from 'lucide-react';
import Badge from '../common/Badge';

export const Header = ({ onOpenMobileSidebar, title, subtitle }) => {
  const { user, logout } = useAuth();
  const location = useLocation();

  const [isDropdownOpen, setIsDropdownOpen] = useState(false);
  const dropdownRef = useRef(null);

  // Close dropdown on outside click
  useEffect(() => {
    const handleClickOutside = (e) => {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target)) {
        setIsDropdownOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  // Dynamic breadcrumb items
  const getBreadcrumbs = () => {
    const path = location.pathname;
    if (path.includes('/admin')) {
      return [
        { label: 'Hệ Thống', path: '/admin/users' },
        { label: 'Quản Trị', path: '/admin/users' },
        { label: 'Tài Khoản và Quyền Hạn' },
      ];
    }
    if (path.includes('/hr/students')) {
      return [
        { label: 'Hệ Thống', path: '/hr/students' },
        { label: 'Nhân Sự', path: '/hr/students' },
        { label: 'Hồ Sơ Sinh Viên' },
      ];
    }
    if (path.includes('/hr/mentors')) {
      return [
        { label: 'Hệ Thống', path: '/hr/mentors' },
        { label: 'Nhân Sự', path: '/hr/mentors' },
        { label: 'Danh Bạ Mentor' },
      ];
    }
    if (path.includes('/mentor')) {
      return [
        { label: 'Hệ Thống', path: '/mentor/students' },
        { label: 'Hướng Dẫn', path: '/mentor/students' },
        { label: 'Sinh Viên Phụ Trách' },
      ];
    }
    if (path.includes('/student')) {
      return [
        { label: 'Hệ Thống', path: '/student/profile' },
        { label: 'Sinh Viên', path: '/student/profile' },
        { label: 'Hồ Sơ Thực Tập' },
      ];
    }
    return [{ label: 'Trang Chủ', path: '/' }, { label: title || 'Bảng Điều Khiển' }];
  };

  const breadcrumbs = getBreadcrumbs();

  return (
    <header className="sticky top-0 z-30 h-16 bg-white/90 backdrop-blur-md border-b border-slate-200/80 px-4 sm:px-6 lg:px-8 flex items-center justify-between transition-colors">
      {/* Left: Mobile hamburger & Breadcrumbs / Title */}
      <div className="flex items-center gap-3 min-w-0">
        <button
          type="button"
          onClick={onOpenMobileSidebar}
          className="p-2 -ml-2 text-slate-500 hover:text-slate-800 hover:bg-slate-100 rounded-lg lg:hidden transition-colors cursor-pointer"
          aria-label="Open mobile menu"
        >
          <Menu className="w-5 h-5" />
        </button>

        <div className="min-w-0">
          {/* Dynamic Breadcrumbs */}
          <nav className="flex items-center gap-1.5 text-[11px] text-slate-400 font-medium mb-0.5">
            {breadcrumbs.map((crumb, idx) => {
              const isLast = idx === breadcrumbs.length - 1;
              return (
                <React.Fragment key={idx}>
                  {idx > 0 && <ChevronRight className="w-3 h-3 text-slate-300 shrink-0" />}
                  {isLast ? (
                    <span className="text-slate-700 font-semibold truncate">{crumb.label}</span>
                  ) : (
                    <span className="hover:text-slate-600 transition-colors truncate">
                      {crumb.label}
                    </span>
                  )}
                </React.Fragment>
              );
            })}
          </nav>

          <h1 className="text-base font-bold text-slate-900 tracking-tight leading-tight truncate">
            {title || 'Bảng Điều Khiển'}
          </h1>
        </div>
      </div>

      {/* Right: Notifications, User Profile Summary & Logout */}
      <div className="flex items-center gap-2 sm:gap-3 shrink-0">
        {/* Notifications Bell */}
        <button
          type="button"
          className="relative p-2 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-xl transition-colors cursor-pointer"
          title="Thông báo mới"
        >
          <Bell className="w-4 h-4" />
          <span className="absolute top-1.5 right-1.5 w-2 h-2 rounded-full bg-indigo-600 ring-2 ring-white" />
        </button>

        <div className="h-5 w-[1px] bg-slate-200 hidden sm:block" />

        {/* User Avatar & Dropdown */}
        <div className="relative" ref={dropdownRef}>
          <button
            type="button"
            onClick={() => setIsDropdownOpen(!isDropdownOpen)}
            className="flex items-center gap-2 p-1 rounded-xl hover:bg-slate-100 transition-colors cursor-pointer"
          >
            <div className="w-8 h-8 rounded-lg bg-gradient-to-tr from-indigo-600 to-sky-500 text-white font-bold text-xs flex items-center justify-center shadow-xs ring-1 ring-slate-200">
              {user?.fullName?.charAt(0) || user?.email?.charAt(0).toUpperCase() || 'U'}
            </div>
            <div className="hidden md:block text-left pr-1">
              <p className="text-xs font-bold text-slate-800 leading-none truncate max-w-[120px]">
                {user?.fullName || user?.email?.split('@')[0]}
              </p>
              <p className="text-[10px] text-slate-400 font-medium leading-none mt-1">
                {user?.role?.replace('ROLE_', '')}
              </p>
            </div>
            <ChevronDown className="w-3.5 h-3.5 text-slate-400 hidden sm:block" />
          </button>

          {isDropdownOpen && (
            <div className="absolute right-0 mt-2 w-56 rounded-2xl bg-white shadow-2xl border border-slate-200 py-1.5 z-50 animate-in fade-in zoom-in-95 duration-100">
              <div className="px-3.5 py-2.5 border-b border-slate-100">
                <p className="text-xs font-bold text-slate-900 truncate">
                  {user?.fullName || user?.email}
                </p>
                <p className="text-[11px] text-slate-400 truncate mt-0.5">{user?.email}</p>
                <div className="mt-2">
                  <Badge variant={user?.role} dot={true} className="text-[10px] py-0 px-2" />
                </div>
              </div>

              <div className="py-1">
                <Link
                  to={user?.role === 'ROLE_STUDENT' ? '/student/profile' : '/hr/students'}
                  onClick={() => setIsDropdownOpen(false)}
                  className="w-full text-left px-3.5 py-2 text-xs text-slate-700 hover:bg-slate-50 flex items-center gap-2.5 transition-colors cursor-pointer"
                >
                  <User className="w-3.5 h-3.5 text-slate-400" />
                  <span>Hồ Sơ Của Tôi</span>
                </Link>

                <button
                  type="button"
                  onClick={() => setIsDropdownOpen(false)}
                  className="w-full text-left px-3.5 py-2 text-xs text-slate-700 hover:bg-slate-50 flex items-center gap-2.5 transition-colors cursor-pointer"
                >
                  <Settings className="w-3.5 h-3.5 text-slate-400" />
                  <span>Cài Đặt Hệ Thống</span>
                </button>
              </div>

              <div className="pt-1 border-t border-slate-100">
                <button
                  type="button"
                  onClick={logout}
                  className="w-full text-left px-3.5 py-2 text-xs text-rose-600 hover:bg-rose-50 flex items-center gap-2.5 font-semibold transition-colors cursor-pointer"
                >
                  <LogOut className="w-3.5 h-3.5" />
                  <span>Đăng Xuất Khỏi Phiên Làm Việc</span>
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </header>
  );
};

export default Header;
