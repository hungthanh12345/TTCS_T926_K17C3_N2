import api from './api';

const getData = (response) => response.data?.data ?? response.data;

const createUploadForm = (file, documentType) => {
  const form = new FormData();
  if (documentType) form.append('documentType', documentType);
  form.append('file', file);
  return form;
};

export const studentDocumentService = {
  async getMyDocuments() {
    const response = await api.get('/student/documents');
    return getData(response) || [];
  },

  async upload(documentType, file) {
    const response = await api.post(
      '/student/documents',
      createUploadForm(file, documentType),
      { timeout: 30000 }
    );
    return getData(response);
  },

  async replace(documentId, file) {
    const response = await api.put(
      `/student/documents/${documentId}`,
      createUploadForm(file),
      { timeout: 30000 }
    );
    return getData(response);
  },

  async download(documentId) {
    return api.get(`/student/documents/${documentId}/download`, { responseType: 'blob' });
  },

  async remove(documentId) {
    const response = await api.delete(`/student/documents/${documentId}`);
    return getData(response);
  },
};

export default studentDocumentService;
