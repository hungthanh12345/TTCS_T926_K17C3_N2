import api from './api';
import { getStoredUsers, saveStoredUsers } from './mockData';

export const userService = {
  /**
   * Fetch all users for Admin User Management View
   * GET /api/admin/users
   */
  async getUsers(params = {}) {
    try {
      const response = await api.get('/admin/users', { params });
      return response.data?.data || response.data;
    } catch (error) {
      if (!error.response) {
        // Fallback to local storage persistence
        let users = getStoredUsers();
        if (params.role) {
          users = users.filter((u) => u.role === params.role);
        }
        if (params.search) {
          const q = params.search.toLowerCase();
          users = users.filter((u) => u.email.toLowerCase().includes(q));
        }
        return users;
      }
      throw new Error(error.response?.data?.message || 'Không thể tải danh sách tài khoản');
    }
  },

  /**
   * Create a new user
   * POST /api/admin/users
   * Payload: { email, password, roleId, roleName }
   */
  async createUser(userData) {
    try {
      // Normalize role payload to match CreateUserRequestDto exactly
      const rawRoleId = userData.roleId ?? userData.role_id;
      const roleId = rawRoleId ? parseInt(rawRoleId, 10) : undefined;
      const roleName =
        userData.roleName ??
        userData.role_name ??
        (typeof userData.role === 'string' && isNaN(userData.role) ? userData.role : undefined);

      const payload = {
        email: userData.email.trim(),
        password: userData.password,
        roleId:
          roleId ||
          (roleName === 'ROLE_ADMIN'
            ? 1
            : roleName === 'ROLE_HR'
            ? 2
            : roleName === 'ROLE_MENTOR'
            ? 3
            : roleName === 'ROLE_STUDENT'
            ? 4
            : undefined),
        roleName:
          roleName ||
          (roleId === 1
            ? 'ROLE_ADMIN'
            : roleId === 2
            ? 'ROLE_HR'
            : roleId === 3
            ? 'ROLE_MENTOR'
            : roleId === 4
            ? 'ROLE_STUDENT'
            : undefined),
      };

      const response = await api.post('/admin/users', payload);
      return response.data?.data || response.data;
    } catch (error) {
      if (!error.response) {
        // Local fallback creation
        const users = getStoredUsers();
        
        // Check uniqueness
        if (users.some((u) => u.email.toLowerCase() === userData.email.trim().toLowerCase())) {
          throw new Error('Email này đã tồn tại trên hệ thống hoặc thông tin không hợp lệ.');
        }

        const fallbackRole = userData.roleName || userData.role || 'ROLE_HR';
        const newUser = {
          id: `USR-00${users.length + 1}`,
          email: userData.email.trim(),
          role: fallbackRole,
          roleName: fallbackRole,
          roleId: userData.roleId || (fallbackRole === 'ROLE_ADMIN' ? 1 : fallbackRole === 'ROLE_HR' ? 2 : fallbackRole === 'ROLE_MENTOR' ? 3 : 4),
          status: 'ACTIVE',
          createdAt: new Date().toISOString(),
        };

        const updatedUsers = [newUser, ...users];
        saveStoredUsers(updatedUsers);
        return newUser;
      }
      throw error;
    }
  },

  /**
   * Delete or deactivate user
   * DELETE /api/admin/users/{id}
   */
  async deleteUser(id) {
    try {
      const response = await api.delete(`/admin/users/${id}`);
      return response.data;
    } catch (error) {
      if (!error.response) {
        const users = getStoredUsers();
        const updated = users.filter((u) => String(u.id) !== String(id));
        saveStoredUsers(updated);
        return { success: true };
      }
      throw error;
    }
  }
};

export default userService;
