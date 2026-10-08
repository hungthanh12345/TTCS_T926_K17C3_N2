import api from './api';

const errorMessage = (error, fallback) => error.response?.data?.message || error.message || fallback;

export const userService = {
  async getUsers(params = {}) {
    try {
      const response = await api.get('/admin/users', { params });
      return response.data?.data || response.data;
    } catch (error) {
      throw new Error(errorMessage(error, 'Không thể tải danh sách tài khoản.'));
    }
  },

  // Retained for existing API clients; the Admin user-management screen does not expose account creation.
  async createUser(userData) {
    const roleId = Number(userData.roleId ?? userData.role_id ?? 0) || undefined;
    const roleName = userData.roleName ?? userData.role_name ??
      (typeof userData.role === 'string' && Number.isNaN(Number(userData.role)) ? userData.role : undefined);
    if (roleId === 1 || String(roleName || '').trim().toUpperCase() === 'ROLE_ADMIN') {
      throw new Error('Không thể tạo thêm tài khoản quản trị viên.');
    }
    if (roleId === 4 || String(roleName || '').trim().toUpperCase() === 'ROLE_STUDENT') {
      throw new Error('Tài khoản sinh viên phải được tạo qua luồng đăng ký để liên kết hồ sơ.');
    }

    try {
      const response = await api.post('/admin/users', {
        email: userData.email.trim(),
        password: userData.password,
        roleId: roleId || (roleName === 'ROLE_HR' ? 2 : roleName === 'ROLE_MENTOR' ? 3 : roleName === 'ROLE_STUDENT' ? 4 : undefined),
        roleName: roleName || (roleId === 2 ? 'ROLE_HR' : roleId === 3 ? 'ROLE_MENTOR' : roleId === 4 ? 'ROLE_STUDENT' : undefined),
      });
      return response.data?.data || response.data;
    } catch (error) {
      throw new Error(errorMessage(error, 'Không thể tạo tài khoản.'));
    }
  },

  async deleteUser(id) {
    try {
      const response = await api.delete(`/admin/users/${id}`);
      return response.data;
    } catch (error) {
      throw new Error(errorMessage(error, 'Không thể xóa tài khoản.'));
    }
  },
};

export default userService;
