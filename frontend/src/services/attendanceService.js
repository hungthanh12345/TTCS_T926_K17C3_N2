import api from './api';

const unwrap = (response) => response.data?.data ?? response.data;
const errorMessage = (error) => error.response?.data?.message || error.message || 'Yêu cầu chấm công không thành công.';

const attendanceService = {
  async getTodayStatus() {
    try {
      return unwrap(await api.get('/student/attendance/today'));
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  },

  async checkIn(data = {}) {
    try {
      return unwrap(await api.post('/student/attendance/check-in', data));
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  },

  async checkOut(data = {}) {
    try {
      return unwrap(await api.post('/student/attendance/check-out', data));
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  },

  async getHistory(params = {}) {
    try {
      return unwrap(await api.get('/student/attendance/history', { params }));
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  },
};

export default attendanceService;
