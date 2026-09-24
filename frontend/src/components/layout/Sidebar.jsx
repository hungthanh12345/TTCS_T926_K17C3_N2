import React from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import {
  Users,
  GraduationCap,
  Briefcase,
  Layers,
  LayoutDashboard,
  LogOut,
  ChevronRight,
  UserCheck,
  Shield,
} from 'lucide-react';
import Badge from '../common/Badge';

export const Sidebar = ({ isMobileOpen, setIsMobileOpen }) => {
  const { user, logout } = useAuth();
  const location = useLocation();

  // Navigation items categorized by access roles
  const navItems = [
    {
      title: 'Quản Lý Tài Khoản',
      path: '/admin/users',
      icon: Users,
      roles: ['ROLE_ADMIN'],
      badge: 'Admin',
    },
    {
      title: 'Hồ Sơ Sinh Viên',
      path: '/hr/students',
      icon: GraduationCap,
      roles: ['ROLE_ADMIN', 'ROLE_HR'],
      badge: 'HR',
    },
    {
      title: 'Danh Bạ Mentor',
      path: '/hr/mentors',
      icon: Briefcase,
      roles: ['ROLE_ADMIN', 'ROLE_HR'],
      badge: null,
    },
    {
      title: 'Sinh Viên Hướng Dẫn',
      path: '/mentor/students',
      icon: UserCheck,
      roles: ['ROLE_MENTOR', 'ROLE_ADMIN'],
      badge: null,
    },
    {
      title: 'Hồ Sơ Thực Tập',
      path: '/student/profile',
      icon: LayoutDashboard,
      roles: ['ROLE_STUDENT', 'ROLE_ADMIN'],
      badge: null,
    },
  ];

  const visibleNavItems = navItems.filter(
    (item) => !item.roles || item.roles.includes(user?.role)
  );

  return (
    <>
      {/* Mobile backdrop */}
      {isMobileOpen && (
        <div
          className="fixed inset-0 z-40 bg-slate-950/80 backdrop-blur-sm lg:hidden transition-opacity"
          onClick={() => setIsMobileOpen(false)}
        />
      )}

      {/* Main Sidebar */}
      <aside
        className={`fixed top-0 bottom-0 left-0 z-40 w-64 bg-slate-950 text-slate-300 flex flex-col transition-transform duration-300 ease-in-out lg:translate-x-0 border-r border-slate-800/80 select-none ${
          isMobileOpen ? 'translate-x-0' : '-translate-x-full'
        }`}
      >
        {/* Brand Header */}
        <div className="h-16 flex items-center px-5 border-b border-slate-800/80 bg-slate-950">
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-xl bg-gradient-to-tr from-indigo-600 via-indigo-500 to-sky-400 flex items-center justify-center text-white shadow-md shadow-indigo-500/20 ring-1 ring-white/15">
              <Layers className="w-5 h-5" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <span className="text-sm font-bold text-white tracking-tight">Quản Lý Thực Tập Sinh</span>
              </div>
              <p className="text-[11px] text-slate-400 font-medium">Cổng thông tin đào tạo & thực tập</p>
            </div>
          </div>
        </div>

        {/* Navigation Links */}
        <div className="flex-1 overflow-y-auto py-5 px-3 space-y-6 scrollbar-thin">
          <div>
            <div className="px-3 mb-2.5 text-[11px] font-bold text-slate-400 uppercase tracking-wider flex items-center justify-between">
              <span>Menu Phân Hệ</span>
              <span className="text-[10px] text-slate-400 font-medium">{visibleNavItems.length} mục</span>
            </div>
            <nav className="space-y-1">
              {visibleNavItems.map((item) => {
                const Icon = item.icon;
                const isActive =
                  location.pathname === item.path ||
                  (item.path === '/mentor/students' && location.pathname.startsWith('/mentor')) ||
                  (item.path === '/student/profile' && location.pathname.startsWith('/student'));

                return (
                  <NavLink
                    key={item.path}
                    to={item.path}
                    onClick={() => setIsMobileOpen(false)}
                    className={`group relative flex items-center justify-between px-3 py-2.5 rounded-xl text-xs font-medium transition-all duration-150 ${
                      isActive
                        ? 'bg-indigo-600/15 text-indigo-300 font-semibold border border-indigo-500/30 shadow-xs'
                        : 'text-slate-400 hover:text-slate-200 hover:bg-slate-900 border border-transparent'
                    }`}
                  >
                    <div className="flex items-center gap-2.5">
                      <Icon
                        className={`w-4 h-4 transition-colors ${
                          isActive ? 'text-indigo-400' : 'text-slate-400 group-hover:text-slate-200'
                        }`}
                      />
                      <span>{item.title}</span>
                    </div>

                    <div className="flex items-center gap-1.5">
                      {item.badge && !isActive && (
                        <span className="text-[10px] font-semibold uppercase px-1.5 py-0.5 rounded bg-slate-900 text-slate-400 border border-slate-800">
                          {item.badge}
                        </span>
                      )}
                      {isActive && (
                        <div className="w-1.5 h-1.5 rounded-full bg-indigo-400 shadow-[0_0_8px_rgba(129,140,248,0.8)]" />
                      )}
                    </div>
                  </NavLink>
                );
              })}
            </nav>
          </div>
        </div>

        {/* User Profile Footer */}
        <div className="p-3 border-t border-slate-800/80 bg-slate-950/80">
          <div className="flex items-center justify-between p-2 rounded-xl bg-slate-900/80 border border-slate-800/90 hover:border-slate-700/80 transition-all">
            <div className="flex items-center gap-2.5 overflow-hidden">
              <div className="relative shrink-0">
                <div className="w-8 h-8 rounded-lg bg-gradient-to-tr from-indigo-600 to-sky-500 text-white font-bold text-xs flex items-center justify-center shadow-xs">
                  {user?.fullName?.charAt(0) || user?.email?.charAt(0).toUpperCase() || 'U'}
                </div>
                <span className="absolute -bottom-0.5 -right-0.5 w-2.5 h-2.5 rounded-full bg-emerald-500 ring-2 ring-slate-950" />
              </div>
              <div className="truncate">
                <p className="text-xs font-semibold text-white truncate">
                  {user?.fullName || user?.email}
                </p>
                <p className="text-[10px] text-slate-400 truncate">
                  {user?.role?.replace('ROLE_', '') || 'USER'}
                </p>
              </div>
            </div>

            <button
              onClick={logout}
              title="Đăng xuất khỏi hệ thống"
              className="p-1.5 text-slate-400 hover:text-rose-400 hover:bg-rose-500/10 rounded-lg transition-colors cursor-pointer"
            >
              <LogOut className="w-4 h-4" />
            </button>
          </div>
        </div>
      </aside>
    </>
  );
};

export default Sidebar;
