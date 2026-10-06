import api from './api';

const unwrap = (response) => response.data?.data ?? response.data;
const errorMessage = (error) => error.response?.data?.message || error.message || 'Không thể tải báo cáo tổng hợp.';

const hrInternshipSummaryService = {
  async getSummary(programId) {
    try {
      return unwrap(await api.get('/hr/internship-summary', { params: programId ? { programId } : {} }));
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  },
};

export default hrInternshipSummaryService;
