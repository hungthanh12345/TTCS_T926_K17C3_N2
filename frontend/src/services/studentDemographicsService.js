import api from './api';
import { apiErrorMessage } from './apiErrorMessage';

const studentDemographicsService = {
  async get() {
    try {
      const response = await api.get('/hr/reports/student-demographics');
      const result = response.data?.data ?? response.data;
      if (!result || !Array.isArray(result.items)) {
        throw new Error('Dữ liệu thống kê không đúng định dạng.');
      }
      return result;
    } catch (error) {
      throw new Error(apiErrorMessage(error, 'Không thể tải thống kê trường và ngành.'));
    }
  },
};

export default studentDemographicsService;