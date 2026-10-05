import React, { useState, useRef, useEffect } from 'react';
import { useLocation, Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { Menu, ChevronDown, LogOut, LayoutDashboard, ChevronRight, Bell, RefreshCw, Settings } from 'lucide-react';
import Badge from '../common/Badge';
import notificationService from '../../services/notificationService';

const ROLE_HOME = {
  ROLE_ADMIN: '/admin/users',
  ROLE_HR: '/hr/students',
  ROLE_MENTOR: '/mentor/students',
  ROLE_STUDENT: '/student/profile',
};

const ROLE_LABELS = {
  ROLE_ADMIN: 'Quản trị viên',
  ROLE_HR: 'Nhân sự',
  ROLE_MENTOR: 'Mentor',
  ROLE_STUDENT: 'Sinh viên',
};

export const Header = ({ onToggleSidebar, isDesktopViewport, isDesktopSidebarOpen, isMobileSidebarOpen, title, subtitle }) => {
  const { user, logout } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();

  const [isDropdownOpen, setIsDropdownOpen] = useState(false);
  const [isNotificationsOpen, setIsNotificationsOpen] = useState(false);
  const [notifications, setNotifications] = useState([]);
  const [isLoadingNotifications, setIsLoadingNotifications] = useState(false);
  const [notificationError, setNotificationError] = useState('');
  const [notificationFilter, setNotificationFilter] = useState('all');
  const dropdownRef = useRef(null);
  const notificationRef = useRef(null);
  const isSidebarOpen = isDesktopViewport ? isDesktopSidebarOpen : isMobileSidebarOpen;

  // Close dropdown on outside click
  useEffect(() => {
    const handleClickOutside = (e) => {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target)) setIsDropdownOpen(false);
      if (notificationRef.current && !notificationRef.current.contains(e.target)) setIsNotificationsOpen(false);
    };
    const handleEscape = (e) => {
      if (e.key === 'Escape') {
        setIsDropdownOpen(false);
        setIsNotificationsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    document.addEventListener('keydown', handleEscape);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
      document.removeEventListener('keydown', handleEscape);
    };
  }, []);

  const loadNotifications = async () => {
    setIsLoadingNotifications(true);
    setNotificationError('');
    try {
      setNotifications(await notificationService.getMine());
    } catch (error) {
      setNotifications([]);
      setNotificationError(error.message || 'Không thể tải thông báo.');
    } finally {
      setIsLoadingNotifications(false);
    }
  };

  useEffect(() => {
    if (!user?.email) return undefined;
    let active = true;
    notificationService.getMine()
      .then((items) => { if (active) setNotifications(items); })
      .catch((error) => { if (active) setNotificationError(error.message || 'Không thể tải thông báo.'); })
      .finally(() => { if (active) setIsLoadingNotifications(false); });
    return () => { active = false; };
  }, [user?.email, user?.role]);

  // Dynamic breadcrumb items
  const getBreadcrumbs = () => {
    const path = location.pathname;
    if (path.startsWith('/hr/programs')) {
      return [
        { label: 'Hệ Thống', path: '/hr/programs' },
        { label: 'Nhân Sự', path: '/hr/programs' },
        { label: 'Chương Trình Thực Tập' },
      ];
    }
    if (path.startsWith('/hr/student-registrations')) {
      return [
        { label: 'Hệ Thống', path: '/hr/student-registrations' },
        { label: 'Nhân Sự', path: '/hr/students' },
        { label: 'Xét Duyệt Đăng Ký' },
      ];
    }
    if (path.startsWith('/hr/internship-summary')) {
      return [
        { label: 'Hệ Thống', path: '/hr/internship-summary' },
        { label: 'Nhân Sự', path: '/hr/students' },
        { label: 'Tổng Hợp Kết Quả Thực Tập' },
      ];
    }
    if (path === '/admin/settings') {
      return [
        { label: 'Hệ Thống', path: '/admin/users' },
        { label: 'Quản Trị', path: '/admin/users' },
        { label: 'Cài Đặt Hệ Thống' },
      ];
    }
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
    if (path === '/student/schedule') {
      return [
        { label: 'Hệ Thống', path: '/student/schedule' },
        { label: 'Sinh Viên', path: '/student/schedule' },
        { label: 'Lịch Thực Tập' },
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
  const unreadCount = notifications.filter((notification) => !notification.isRead).length;
  const visibleNotifications = notifications.filter((notification) => {
    if (notificationFilter === 'unread') return !notification.isRead;
    if (notificationFilter === 'read') return notification.isRead;
    return true;
  });

  const handleNotificationClick = async (notification) => {
    if (!notification.isRead) {
      try {
        await notificationService.markRead(notification.id);
        setNotifications((current) => current.map((item) => item.id === notification.id ? { ...item, isRead: true } : item));
      } catch (error) {
        setNotificationError(error.message || 'Không thể cập nhật trạng thái thông báo.');
      }
    }
    setIsNotificationsOpen(false);
    navigate(notification.route);
  };

  const markAllNotificationsRead = async () => {
    try {
      await notificationService.markAllRead();
      setNotifications((current) => current.map((item) => ({ ...item, isRead: true })));
    } catch (error) {
      setNotificationError(error.message || 'Không thể cập nhật trạng thái thông báo.');
    }
  };

  return (
    <header className="sticky top-0 z-50 min-h-16 bg-white/90 backdrop-blur-md border-b border-slate-200/80 px-4 py-2 sm:px-6 lg:px-8 flex items-center justify-between gap-3 transition-colors">
      {/* Left: Mobile hamburger & Breadcrumbs / Title */}
      <div className="flex items-center gap-3 min-w-0">
        <button
          type="button"
          onClick={onToggleSidebar}
          className="p-2 -ml-2 text-slate-500 hover:text-slate-800 hover:bg-slate-100 rounded-lg transition-colors cursor-pointer"
          aria-label={isSidebarOpen ? 'Thu gọn thanh điều hướng' : 'Mở thanh điều hướng'}
          title={isSidebarOpen ? 'Thu gọn thanh điều hướng' : 'Mở thanh điều hướng'}
          aria-expanded={Boolean(isSidebarOpen)}
          aria-controls="app-sidebar"
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
          {subtitle && <p className="hidden max-w-[52vw] truncate text-[11px] leading-4 text-slate-500 sm:block">{subtitle}</p>}
        </div>
      </div>

      {/* Right: signed-in account summary */}
      <div className="flex items-center gap-2 sm:gap-3 shrink-0">
        <div className="relative" ref={notificationRef}>
          <button
            type="button"
            onClick={() => {
              setIsNotificationsOpen((open) => !open);
              setIsDropdownOpen(false);
              if (!isNotificationsOpen) void loadNotifications();
            }}
            aria-label={`Thông báo${unreadCount ? `, ${unreadCount} chưa đọc` : ''}`}
            aria-expanded={isNotificationsOpen}
            aria-controls="notifications-menu"
            className="relative rounded-xl border border-slate-200 p-2 text-slate-500 transition-colors hover:bg-slate-100 hover:text-slate-800 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500 cursor-pointer"
          >
            <Bell className="h-4 w-4" />
            {unreadCount > 0 && (
              <span className="absolute -right-1 -top-1 min-w-4 rounded-full bg-rose-600 px-1 text-center text-[9px] font-bold leading-4 text-white">
                {unreadCount > 99 ? '99+' : unreadCount}
              </span>
            )}
          </button>

          {isNotificationsOpen && (
            <div id="notifications-menu" className="absolute right-0 z-50 mt-2 w-[min(24rem,calc(100vw-2rem))] overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-2xl">
              <div className="flex items-center justify-between border-b border-slate-100 px-4 py-3">
                <div>
                  <h2 className="text-sm font-bold text-slate-900">Thông báo</h2>
                  <p className="mt-0.5 text-[10px] text-slate-500">Dữ liệu từ hồ sơ, công việc và xét duyệt</p>
                </div>
                <div className="flex items-center gap-1">
                  {unreadCount > 0 && <button type="button" onClick={() => void markAllNotificationsRead()} className="rounded-lg px-2 py-1.5 text-[10px] font-semibold text-indigo-700 hover:bg-indigo-50">Đánh dấu đã đọc</button>}
                  <button type="button" onClick={() => void loadNotifications()} disabled={isLoadingNotifications} className="rounded-lg p-2 text-slate-500 hover:bg-slate-100 disabled:opacity-50" aria-label="Làm mới thông báo">
                    <RefreshCw className={`h-3.5 w-3.5 ${isLoadingNotifications ? 'animate-spin' : ''}`} />
                  </button>
                </div>
              </div>
              <div role="tablist" aria-label="Lọc thông báo" className="flex gap-1 border-b border-slate-100 px-3 py-2">
                {[
                  ['all', 'Tất cả'],
                  ['unread', 'Chưa đọc'],
                  ['read', 'Đã đọc'],
                ].map(([filter, label]) => (
                  <button key={filter} type="button" role="tab" aria-selected={notificationFilter === filter} onClick={() => setNotificationFilter(filter)} className={`rounded-lg px-2.5 py-1.5 text-[11px] font-semibold ${notificationFilter === filter ? 'bg-indigo-50 text-indigo-700' : 'text-slate-500 hover:bg-slate-50'}`}>
                    {label}{filter === 'unread' && unreadCount > 0 ? ` (${unreadCount})` : ''}
                  </button>
                ))}
              </div>
              <div className="max-h-[min(24rem,70vh)] overflow-y-auto p-2">
                {isLoadingNotifications && notifications.length === 0 ? (
                  <p className="px-3 py-8 text-center text-xs text-slate-500">Đang tải thông báo...</p>
                ) : notificationError ? (
                  <div className="px-3 py-6 text-center">
                    <p className="text-xs text-rose-600">{notificationError}</p>
                    <button type="button" onClick={() => void loadNotifications()} className="mt-2 text-xs font-semibold text-indigo-600 hover:text-indigo-700">Thử lại</button>
                  </div>
                ) : visibleNotifications.length === 0 ? (
                  <p className="px-3 py-8 text-center text-xs text-slate-500">{notificationFilter === 'unread' ? 'Không có thông báo chưa đọc.' : notificationFilter === 'read' ? 'Chưa có thông báo đã đọc.' : 'Hiện chưa có thông báo.'}</p>
                ) : visibleNotifications.map((notification) => (
                  <button
                    key={notification.id}
                    type="button"
                    onClick={() => void handleNotificationClick(notification)}
                    className={`block w-full rounded-xl px-3 py-3 text-left transition-colors hover:bg-slate-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500 ${notification.isRead ? '' : 'bg-indigo-50/60'}`}
                  >
                    <span className="flex items-center gap-2 text-xs font-semibold text-slate-900"><span className={`h-1.5 w-1.5 shrink-0 rounded-full ${notification.isRead ? 'bg-slate-300' : 'bg-indigo-600'}`} />{notification.title}</span>
                    <span className="mt-1 block text-[11px] leading-4 text-slate-600">{notification.message}</span>
                    {notification.createdAt && <span className="mt-1.5 block text-[10px] text-slate-400">{new Date(notification.createdAt).toLocaleString('vi-VN')}</span>}
                  </button>
                ))}
              </div>
            </div>
          )}
        </div>

        {/* User Avatar & Dropdown */}
        <div className="relative" ref={dropdownRef}>
          <button
            type="button"
            onClick={() => setIsDropdownOpen(!isDropdownOpen)}
            aria-label="Mở menu tài khoản"
            aria-expanded={isDropdownOpen}
            aria-controls="account-menu"
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
                {ROLE_LABELS[user?.role] || 'Tài khoản'}
              </p>
            </div>
            <ChevronDown className="w-3.5 h-3.5 text-slate-400 hidden sm:block" />
          </button>

          {isDropdownOpen && (
            <div id="account-menu" className="absolute right-0 mt-2 w-60 rounded-2xl bg-white shadow-2xl border border-slate-200 py-1.5 z-50 animate-in fade-in zoom-in-95 duration-100">
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
                  to={ROLE_HOME[user?.role] || '/login'}
                  onClick={() => setIsDropdownOpen(false)}
                  className="w-full text-left px-3.5 py-2 text-xs text-slate-700 hover:bg-slate-50 focus-visible:outline-none focus-visible:bg-slate-50 flex items-center gap-2.5 transition-colors cursor-pointer"
                >
                  <LayoutDashboard className="w-3.5 h-3.5 text-slate-400" />
                  <span>{user?.role === 'ROLE_STUDENT' ? 'Hồ Sơ Của Tôi' : 'Không gian làm việc'}</span>
                </Link>
                {user?.role === 'ROLE_ADMIN' && (
                  <Link to="/admin/settings" onClick={() => setIsDropdownOpen(false)} className="w-full px-3.5 py-2 text-xs text-slate-700 hover:bg-slate-50 flex items-center gap-2.5 transition-colors">
                    <Settings className="w-3.5 h-3.5 text-slate-400" />
                    <span>Cài Đặt Hệ Thống</span>
                  </Link>
                )}
              </div>

              <div className="pt-1 border-t border-slate-100">
                <button
                  type="button"
                  onClick={logout}
                  className="w-full text-left px-3.5 py-2 text-xs text-rose-600 hover:bg-rose-50 focus-visible:outline-none focus-visible:bg-rose-50 flex items-center gap-2.5 font-semibold transition-colors cursor-pointer"
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
