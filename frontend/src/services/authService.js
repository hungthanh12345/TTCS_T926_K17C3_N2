import api from './api';

const saveSession = (token, user) => {
  sessionStorage.setItem('token', token);
  sessionStorage.setItem('user', JSON.stringify(user));
};

// Migrate an existing single-tab session once, then keep credentials tab-local.
const migrateLegacySession = () => {
  const hasSession = sessionStorage.getItem('token') && sessionStorage.getItem('user');
  if (hasSession) {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    return;
  }

  const legacyToken = localStorage.getItem('token');
  const legacyUser = localStorage.getItem('user');
  if (legacyToken && legacyUser) {
    sessionStorage.setItem('token', legacyToken);
    sessionStorage.setItem('user', legacyUser);
    localStorage.removeItem('token');
    localStorage.removeItem('user');
  }
};

export const authService = {
  /**
   * Story 1: Authenticate user against POST /api/auth/login
   * @param {Object} credentials { email, password }
   */
  async login({ email, password }) {
    try {
      const response = await api.post('/auth/login', { email, password });
      const payload = response.data?.data || response.data;

      const token = payload?.token || payload?.accessToken;
      const rawUser = payload?.user || payload;
      const allowedRoles = new Set(['ROLE_ADMIN', 'ROLE_HR', 'ROLE_MENTOR', 'ROLE_STUDENT']);
      if (typeof token !== 'string' || !token.trim() || !allowedRoles.has(rawUser?.role)) {
        throw new Error('API trả về thông tin đăng nhập không hợp lệ.');
      }

      const user = {
        userId: rawUser?.userId || rawUser?.id || payload?.userId,
        email: rawUser?.email || payload?.email,
        role: rawUser?.role,
        status: rawUser?.status || payload?.status || 'ACTIVE',
        fullName: rawUser?.fullName || rawUser?.name || email.split('@')[0],
      };

      if (user.userId == null || !user.email) {
        throw new Error('API trả về hồ sơ người dùng không đầy đủ.');
      }

      saveSession(token, user);
      return { token, user };
    } catch (error) {
      const message = error.response?.data?.message || error.message || 'Login failed. Please check your credentials.';
      throw new Error(message);
    }
  },

  /**
   * Log out the current user and purge session storage
   */
  logout() {
    sessionStorage.removeItem('token');
    sessionStorage.removeItem('user');
  },

  /**
   * Get the current authenticated user object
   */
  getCurrentUser() {
    try {
      migrateLegacySession();
      const userStr = sessionStorage.getItem('user');
      return userStr ? JSON.parse(userStr) : null;
    } catch {
      return null;
    }
  },

  /**
   * Retrieve current JWT token string
   */
  getToken() {
    migrateLegacySession();
    return sessionStorage.getItem('token');
  },

  /**
   * Check if user is currently authenticated
   */
  isAuthenticated() {
    return !!this.getToken() && !!this.getCurrentUser();
  }
};

export default authService;
