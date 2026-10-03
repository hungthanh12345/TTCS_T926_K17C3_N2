import api from './api';

const unwrap = (response) => response.data?.data ?? response.data;
const errorMessage = (error) => error.response?.data?.message || error.message || 'Yêu cầu không thành công.';

const mentorTaskService = {
  async getAssignedStudents() {
    try {
      const result = unwrap(await api.get('/mentor/students'));
      return (Array.isArray(result) ? result : []).map((student) => ({
        ...student,
        id: student.studentId,
        phone: student.phoneNumber || '',
      }));
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  },

  async getTasks() {
    try {
      const result = unwrap(await api.get('/mentor/tasks'));
      return Array.isArray(result) ? result : [];
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  },

  async getTask(taskId) {
    try {
      return unwrap(await api.get(`/mentor/tasks/${taskId}`));
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  },

  async createTask(data) {
    try {
      return unwrap(await api.post('/mentor/tasks', data));
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  },

  async updateTask(taskId, data) {
    try {
      return unwrap(await api.put(`/mentor/tasks/${taskId}`, data));
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  },

  async deleteTask(taskId) {
    try {
      return unwrap(await api.delete(`/mentor/tasks/${taskId}`));
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  },

  async getMyTasks() {
    try {
      const result = unwrap(await api.get('/student/tasks'));
      return Array.isArray(result) ? result : [];
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  },

  async getMyTask(taskId) {
    try {
      return unwrap(await api.get(`/student/tasks/${taskId}`));
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  },

  async updateMyTaskProgress(taskId, data) {
    try {
      return unwrap(await api.put(`/student/tasks/${taskId}/progress`, data));
    } catch (error) {
      throw new Error(errorMessage(error));
    }
  },
};

export default mentorTaskService;
