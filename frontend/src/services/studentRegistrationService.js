import api from './api';

const unwrap = (response) => response.data?.data ?? response.data;
const getErrorMessage = (error) => error.response?.data?.message || error.message || 'Yêu cầu không thành công.';

const studentRegistrationService = {
  async getPrograms() {
    try {
      return unwrap(await api.get('/programs')) || [];
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  },

  async register(data, documents) {
    try {
      const form = new FormData();
      Object.entries(data).forEach(([key, value]) => form.append(key, value));
      form.append('cv', documents.cv);
      form.append('internshipLetter', documents.internshipLetter);
      return unwrap(await api.post('/auth/register-application', form, { timeout: 60000 }));
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  },

  async getOwnStatus(credentials) {
    try {
      return unwrap(await api.post('/auth/registration-status', credentials));
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  },

  async getPending() {
    try {
      return unwrap(await api.get('/hr/student-registrations'));
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  },

  async getPendingDetails(studentId) {
    try {
      return unwrap(await api.get(`/hr/student-registrations/${studentId}`));
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  },

  async getRegistrationDocuments(studentId) {
    try {
      return unwrap(await api.get(`/hr/student-registrations/${studentId}/documents`)) || [];
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  },

  async downloadRegistrationDocument(studentId, documentId) {
    try {
      return await api.get(`/hr/student-registrations/${studentId}/documents/${documentId}/download`, { responseType: 'blob' });
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  },

  async approve(studentId) {
    try {
      return unwrap(await api.post(`/hr/student-registrations/${studentId}/approve`));
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  },

  async reject(studentId, rejectionReason = '') {
    try {
      return unwrap(await api.post(`/hr/student-registrations/${studentId}/reject`, { rejectionReason }));
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  },
};

export default studentRegistrationService;
