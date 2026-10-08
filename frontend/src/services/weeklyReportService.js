import api from './api';

const unwrap = (response) => response.data?.data ?? response.data;
const errorMessage = (error) => error.response?.data?.message || error.message || 'Yêu cầu báo cáo tuần không thành công.';

const weeklyReportService = {
  async getMine() {
    try {
      const result = unwrap(await api.get('/student/weekly-reports'));
      return Array.isArray(result) ? result : [];
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  },

  async getMineById(reportId) {
    try {
      return unwrap(await api.get(`/student/weekly-reports/${reportId}`));
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  },

  async create(data) {
    try {
      return unwrap(await api.post('/student/weekly-reports', data));
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  },

  async update(reportId, data) {
    try {
      return unwrap(await api.put(`/student/weekly-reports/${reportId}`, data));
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  },
};

export default weeklyReportService;
