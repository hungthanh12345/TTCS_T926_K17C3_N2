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
      throw new Error(error.response?.data?.message || 'Failed to fetch users');
    }
  },

  /**
   * Create a new user
   * POST /api/admin/users
   * Payload: { email, password, role }
   */
  async createUser(userData) {
    try {
      const response = await api.post('/admin/users', userData);
      return response.data?.data || response.data;
    } catch (error) {
      if (!error.response) {
        // Local fallback creation
        const users = getStoredUsers();
        
        // Check uniqueness
        if (users.some((u) => u.email.toLowerCase() === userData.email.trim().toLowerCase())) {
          throw new Error('A user with this email already exists.');
        }

        const newUser = {
          id: `USR-00${users.length + 1}`,
          email: userData.email.trim(),
          role: userData.role,
          status: 'ACTIVE',
          createdAt: new Date().toISOString(),
        };

        const updatedUsers = [newUser, ...users];
        saveStoredUsers(updatedUsers);
        return newUser;
      }
      throw new Error(error.response?.data?.message || 'Failed to create user');
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
        const updated = users.filter((u) => u.id !== id);
        saveStoredUsers(updated);
        return { success: true };
      }
      throw new Error(error.response?.data?.message || 'Failed to delete user');
    }
  }
};

export default userService;
