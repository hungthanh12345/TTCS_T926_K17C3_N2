import api from './api';

const getData = (response) => response.data?.data ?? response.data;

export const studentContractService = {
  // Lấy hợp đồng thực tập của sinh viên đang đăng nhập (null nếu chưa có)
  async getMyContract() {
    try {
      const response = await api.get('/student/contract');
      return getData(response) || null;
    } catch (error) {
      if (error.response?.status === 404) return null;
      throw error;
    }
  },

  // Sinh viên xác nhận hợp đồng
  async confirm(contractId) {
    const response = await api.post(`/student/contract/${contractId}/confirm`);
    return getData(response);
  },

  async download(contractId) {
    return api.get(`/student/contract/${contractId}/download`, { responseType: 'blob' });
  },
};

export default studentContractService;
