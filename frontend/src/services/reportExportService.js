import api from './api';
import { apiErrorMessage } from './apiErrorMessage';

const reportExportService = {
  async download(format, filters = {}) {
    if (format !== 'excel' && format !== 'pdf') {
      throw new Error('Định dạng báo cáo không hợp lệ.');
    }

    try {
      const response = await api.get('/hr/reports/export/' + format, {
        responseType: 'blob',
        params: filters,
      });
      return response.data;
    } catch (error) {
      throw new Error(apiErrorMessage(error, 'Không thể xuất báo cáo. Vui lòng thử lại sau.'));
    }
  },
};

export default reportExportService;