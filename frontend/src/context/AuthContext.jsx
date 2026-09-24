import React, { createContext, useContext, useState, useEffect } from 'react';
import authService from '../services/authService';
import toast from 'react-hot-toast';

const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [token, setToken] = useState(null);
  const [isLoading, setIsLoading] = useState(true);

  // Initialize auth state from localStorage
  useEffect(() => {
    try {
      const storedToken = authService.getToken();
      const storedUser = authService.getCurrentUser();
      if (storedToken && storedUser) {
        setToken(storedToken);
        setUser(storedUser);
      }
    } catch (err) {
      console.error('Lỗi khi tải thông tin phiên làm việc', err);
      authService.logout();
    } finally {
      setIsLoading(false);
    }
  }, []);

  /**
   * Login handler
   */
  const login = async (credentials) => {
    setIsLoading(true);
    try {
      const result = await authService.login(credentials);
      setUser(result.user);
      setToken(result.token);
      toast.success(`Xin chào, ${result.user.fullName || result.user.email}!`, {
        icon: '👋',
      });
      return result.user;
    } catch (error) {
      toast.error(error.message || 'Đăng nhập không thành công. Vui lòng kiểm tra lại tài khoản.');
      throw error;
    } finally {
      setIsLoading(false);
    }
  };

  /**
   * Logout handler
   */
  const logout = (silent = false) => {
    authService.logout();
    setUser(null);
    setToken(null);
    if (!silent) {
      toast.success('Đã đăng xuất khỏi hệ thống.');
    }
  };

  /**
   * Clean session reset handler without toast notifications
   */
  const resetSession = () => {
    authService.logout();
    setUser(null);
    setToken(null);
  };

  /**
   * Quick role checker helper
   */
  const hasRole = (allowedRoles) => {
    if (!user) return false;
    if (Array.isArray(allowedRoles)) {
      return allowedRoles.includes(user.role);
    }
    return user.role === allowedRoles;
  };

  /**
   * Helper to switch role on the fly for demo evaluation
   */
  const switchRole = (newRole) => {
    if (!user) return;
    const roleNames = {
      ROLE_ADMIN: 'Quản trị viên (Admin)',
      ROLE_HR: 'Quản lý Nhân sự (HR)',
      ROLE_MENTOR: 'Mentor Doanh nghiệp',
      ROLE_STUDENT: 'Sinh viên Thực tập',
    };
    const updated = { ...user, role: newRole };
    setUser(updated);
    localStorage.setItem('user', JSON.stringify(updated));
    toast.success(`Đã chuyển sang vai trò: ${roleNames[newRole] || newRole}`, {
      icon: '🛡️',
    });
  };

  const value = {
    user,
    token,
    isLoading,
    isAuthenticated: !!token && !!user,
    login,
    logout,
    resetSession,
    hasRole,
    switchRole,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

export default AuthContext;
