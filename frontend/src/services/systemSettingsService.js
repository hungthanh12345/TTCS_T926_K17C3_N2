import api from './api';

const unwrap = (response) => response.data?.data ?? response.data;

const systemSettingsService = {
  async get() {
    try {
      return unwrap(await api.get('/admin/system-settings'));
    } catch {
      throw new Error('Không thể tải thông tin hệ thống. Vui lòng thử lại.');
    }
  },
};

export default systemSettingsService;
