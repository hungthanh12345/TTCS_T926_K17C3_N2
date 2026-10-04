import api from './api';

const unwrap = (response) => response.data?.data ?? response.data;
const errorMessage = (error) => error.response?.data?.message || error.message || 'Không thể tải lịch thực tập.';

const studentScheduleService = {
  async getMySchedule() {
    try {
      const result = unwrap(await api.get('/student/schedule'));
      return Array.isArray(result) ? result : [];
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  },
};

export default studentScheduleService;
