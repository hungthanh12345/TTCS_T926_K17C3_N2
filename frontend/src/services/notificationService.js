import api from './api';

const notificationService = {
  async getMine() {
    try {
      const response = await api.get('/notifications');
      const payload = response.data?.data || response.data;
      return Array.isArray(payload) ? payload : [];
    } catch (error) {
      throw new Error(error.response?.data?.message || error.message || 'Không thể tải thông báo.');
    }
  },
};

export default notificationService;
