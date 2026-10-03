import api from './api';

const unwrap = (response) => response.data?.data ?? response.data;
const getErrorMessage = (error) => error.response?.data?.message || error.message || 'Yêu cầu không thành công.';

const studentRegistrationService = {
  async register(data) {
    try {
      return unwrap(await api.post('/auth/register', data));
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

  async approve(studentId) {
    try {
      return unwrap(await api.post(`/hr/student-registrations/${studentId}/approve`));
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  },

  async reject(studentId) {
    try {
      return unwrap(await api.post(`/hr/student-registrations/${studentId}/reject`));
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  },
};

export default studentRegistrationService;
