import api from './api';

const unwrap = (response) => response.data?.data ?? response.data;
const errorMessage = (error) => error.response?.data?.message || error.message || 'Yêu cầu đánh giá không thành công.';

const internshipEvaluationService = {
  async getAssignedStudents() {
    try {
      const result = unwrap(await api.get('/mentor/evaluations'));
      return Array.isArray(result) ? result : [];
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  },

  async create(studentId, data) {
    try {
      return unwrap(await api.post(`/mentor/evaluations/students/${studentId}`, data));
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  },

  async update(evaluationId, data) {
    try {
      return unwrap(await api.put(`/mentor/evaluations/${evaluationId}`, data));
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  },

  async getMine() {
    try {
      const result = unwrap(await api.get('/student/evaluations'));
      return Array.isArray(result) ? result : [];
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  },
};

export default internshipEvaluationService;
