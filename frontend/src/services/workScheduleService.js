import api from './api';

const unwrap = (response) => response.data?.data ?? response.data;
const getError = (error, fallback) =>
  error.response?.data?.message || error.response?.data?.title || error.message || fallback;

// api.js already prefixes requests with /api, so these paths map to /api/hr/work-schedules.
const workScheduleService = {
  async getAll() {
    try {
      const result = unwrap(await api.get('/hr/work-schedules'));
      return Array.isArray(result) ? result : (result?.items || []);
    } catch (error) {
      throw new Error(getError(error, 'Không thể tải danh sách lịch làm việc.'));
    }
  },

  async getById(id) {
    try {
      return unwrap(await api.get(`/hr/work-schedules/${id}`));
    } catch (error) {
      throw new Error(getError(error, 'Không thể tải chi tiết lịch làm việc.'));
    }
  },

  async create(payload) {
    try {
      return unwrap(await api.post('/hr/work-schedules', payload));
    } catch (error) {
      throw new Error(getError(error, 'Không thể tạo lịch làm việc.'));
    }
  },

  async update(id, payload) {
    try {
      return unwrap(await api.put(`/hr/work-schedules/${id}`, payload));
    } catch (error) {
      throw new Error(getError(error, 'Không thể cập nhật lịch làm việc.'));
    }
  },

  async remove(id) {
    try {
      return unwrap(await api.delete(`/hr/work-schedules/${id}`));
    } catch (error) {
      throw new Error(getError(error, 'Không thể ngừng kích hoạt lịch làm việc.'));
    }
  },
};

export default workScheduleService;
