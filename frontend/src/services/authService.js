import api from './api';
import { getStoredUsers } from './mockData';

export const authService = {
  /**
   * Story 1: Authenticate user against POST /api/auth/login
   * @param {Object} credentials { email, password }
   */
  async login({ email, password }) {
    try {
      const response = await api.post('/auth/login', { email, password });
      const payload = response.data?.data || response.data;
      
      const token = payload.token || payload.accessToken || 'demo_jwt_token_' + Date.now();
      const rawUser = payload.user || payload;
      const user = {
        userId: rawUser.userId || rawUser.id || payload.userId || 'USR-001',
        email: rawUser.email || payload.email || email,
        role: rawUser.role || payload.role || 'ROLE_ADMIN',
        status: rawUser.status || payload.status || 'ACTIVE',
        fullName: rawUser.fullName || rawUser.name || email.split('@')[0],
      };

      localStorage.setItem('token', token);
      localStorage.setItem('user', JSON.stringify(user));
      return { token, user };
    } catch (error) {
      // If network error (backend server offline), check mock users for seamless developer demo
      if (!error.response) {
        console.info('Backend unreachable, testing against mock credential repository.');
        const mockUsers = getStoredUsers();
        const matched = mockUsers.find(
          (u) => u.email.toLowerCase() === email.trim().toLowerCase()
        );

        if (matched) {
          const user = {
            userId: matched.id,
            email: matched.email,
            role: matched.role,
            fullName: matched.email.split('@')[0].replace('.', ' ').toUpperCase(),
          };
          const token = `mock_jwt_token_${matched.role}_${Date.now()}`;
          localStorage.setItem('token', token);
          localStorage.setItem('user', JSON.stringify(user));
          return { token, user, isMock: true };
        }

        // Allow instant role testing for standard test accounts if not matched
        if (email.includes('admin')) {
          const user = { userId: 'USR-001', email, role: 'ROLE_ADMIN', fullName: 'Administrator' };
          const token = `mock_jwt_token_ROLE_ADMIN_${Date.now()}`;
          localStorage.setItem('token', token);
          localStorage.setItem('user', JSON.stringify(user));
          return { token, user, isMock: true };
        } else if (email.includes('hr')) {
          const user = { userId: 'USR-002', email, role: 'ROLE_HR', fullName: 'HR Specialist' };
          const token = `mock_jwt_token_ROLE_HR_${Date.now()}`;
          localStorage.setItem('token', token);
          localStorage.setItem('user', JSON.stringify(user));
          return { token, user, isMock: true };
        } else if (email.includes('mentor')) {
          const user = { userId: 'USR-003', email, role: 'ROLE_MENTOR', fullName: 'Lead Mentor' };
          const token = `mock_jwt_token_ROLE_MENTOR_${Date.now()}`;
          localStorage.setItem('token', token);
          localStorage.setItem('user', JSON.stringify(user));
          return { token, user, isMock: true };
        } else if (email.includes('student')) {
          const user = { userId: 'USR-004', email, role: 'ROLE_STUDENT', fullName: 'Sarah Johnson' };
          const token = `mock_jwt_token_ROLE_STUDENT_${Date.now()}`;
          localStorage.setItem('token', token);
          localStorage.setItem('user', JSON.stringify(user));
          return { token, user, isMock: true };
        }

        throw new Error('Invalid email or password. Please verify your credentials.');
      }

      // Re-throw server error
      const message = error.response?.data?.message || 'Login failed. Please check your credentials.';
      throw new Error(message);
    }
  },

  /**
   * Log out the current user and purge session storage
   */
  logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    localStorage.removeItem('mock_students');
    localStorage.removeItem('mock_mentors');
    localStorage.removeItem('mock_users');
  },

  /**
   * Get the current authenticated user object
   */
  getCurrentUser() {
    try {
      const userStr = localStorage.getItem('user');
      return userStr ? JSON.parse(userStr) : null;
    } catch {
      return null;
    }
  },

  /**
   * Retrieve current JWT token string
   */
  getToken() {
    return localStorage.getItem('token');
  },

  /**
   * Check if user is currently authenticated
   */
  isAuthenticated() {
    return !!localStorage.getItem('token') && !!this.getCurrentUser();
  }
};

export default authService;
