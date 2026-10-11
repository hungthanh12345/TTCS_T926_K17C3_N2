import api from './api';

const getData = (response) => response.data?.data ?? response.data;

const createUploadForm = (file) => {
  const form = new FormData();
  form.append('file', file);
  return form;
};

export const hrContractService = {
  async getContracts(search) {
    const response = await api.get('/hr/contracts', {
      params: search ? { search } : undefined,
    });
    return getData(response) || [];
  },

  // Tải lên lần đầu hoặc thay thế hợp đồng của một sinh viên
  async upload(studentId, file) {
    const response = await api.put(
      `/hr/contracts/students/${studentId}`,
      createUploadForm(file),
      { timeout: 30000, headers: { 'Content-Type': 'multipart/form-data' } }
    );
    return getData(response);
  },

  async download(studentId) {
    return api.get(`/hr/contracts/students/${studentId}/download`, { responseType: 'blob' });
  },
};

export default hrContractService;
