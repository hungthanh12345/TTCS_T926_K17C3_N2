import api from './api';
import { apiErrorMessage } from './apiErrorMessage';

const getExportErrorMessage = async (error) => {
  const responseData = error?.response?.data;
  if (typeof Blob !== 'undefined' && responseData instanceof Blob) {
    try {
      const parsed = JSON.parse(await responseData.text());
      return apiErrorMessage({
        ...error,
        response: { ...error.response, data: parsed },
      }, 'Không thể xuất báo cáo. Vui lòng thử lại sau.');
    } catch {
      // Keep the standard localized status message for non-JSON error bodies.
    }
  }

  return apiErrorMessage(error, 'Không thể xuất báo cáo. Vui lòng thử lại sau.');
};

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
      if (response.status === 204 || response.data?.size === 0) {
        throw new Error('Không có dữ liệu phù hợp với loại báo cáo và bộ lọc đã chọn.');
      }
      return response.data;
    } catch (error) {
      if (error instanceof Error && error.message === 'Không có dữ liệu phù hợp với loại báo cáo và bộ lọc đã chọn.') {
        throw error;
      }
      throw new Error(await getExportErrorMessage(error));
    }
  },
};

export default reportExportService;
