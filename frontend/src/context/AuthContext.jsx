import React, { createContext, useCallback, useContext, useState } from 'react';
import authService from '../services/authService';
import toast from 'react-hot-toast';

const AuthContext = createContext(null);

const readInitialSession = () => {
  try {
    const token = authService.getToken();
    const user = authService.getCurrentUser();
    return token && user ? { token, user } : { token: null, user: null };
  } catch {
    try {
      authService.logout();
    } catch {
      // Browser storage may be unavailable; start unauthenticated in that case.
    }
    return { token: null, user: null };
  }
};

export const AuthProvider = ({ children }) => {
  const [session, setSession] = useState(readInitialSession);
  const { user, token } = session;
  const [isLoading, setIsLoading] = useState(false);

  /**
   * Login handler
   */
  const login = async (credentials) => {
    setIsLoading(true);
    try {
      const result = await authService.login(credentials);
      setSession({ user: result.user, token: result.token });
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
    setSession({ user: null, token: null });
    if (!silent) {
      toast.success('Đã đăng xuất khỏi hệ thống.');
    }
  };

  /**
   * Clean session reset handler without toast notifications
   */
  const resetSession = useCallback(() => {
    authService.logout();
    setSession({ user: null, token: null });
  }, []);

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

  const value = {
    user,
    token,
    isLoading,
    isAuthenticated: !!token && !!user,
    login,
    logout,
    resetSession,
    hasRole,
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
