import api from './api';

const notificationService = {
  async getMine() {
    try {
      const response = await api.get('/notifications');
      const payload = response.data?.data || response.data;
      return Array.isArray(payload) ? payload : [];
    } catch {
      throw new Error('Không thể tải thông báo. Vui lòng thử lại.');
    }
  },

  async markRead(notificationId) {
    try {
      await api.post(`/notifications/${notificationId}/read`);
    } catch {
      throw new Error('Không thể cập nhật trạng thái thông báo. Vui lòng thử lại.');
    }
  },

  async markAllRead() {
    try {
      await api.post('/notifications/read-all');
    } catch {
      throw new Error('Không thể cập nhật trạng thái thông báo. Vui lòng thử lại.');
    }
  },
};

export default notificationService;
