import api from './api';

const unwrap = (response) => response.data?.data ?? response.data;
const errorMessage = (error) => error.response?.data?.message || error.message || 'Yêu cầu báo cáo tuần không thành công.';

const mentorWeeklyReportService = {
  async getAssigned() {
    try {
      const result = unwrap(await api.get('/mentor/weekly-reports'));
      return Array.isArray(result) ? result : [];
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  },

  async getById(reportId) {
    try {
      return unwrap(await api.get(`/mentor/weekly-reports/${reportId}`));
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  },

  async createFeedback(reportId, content) {
    try {
      return unwrap(await api.post(`/mentor/weekly-reports/${reportId}/feedback`, { content }));
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  },

  async updateFeedback(reportId, content) {
    try {
      return unwrap(await api.put(`/mentor/weekly-reports/${reportId}/feedback`, { content }));
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  },
};

export default mentorWeeklyReportService;
