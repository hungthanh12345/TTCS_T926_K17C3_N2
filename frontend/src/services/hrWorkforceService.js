import api from './api';
import studentService from './studentService';
import { apiErrorMessage } from './apiErrorMessage';

const unwrap = (response) => response.data?.data ?? response.data;
const message = (error, fallback) => apiErrorMessage(error, fallback);
const call = async (request) => {
  try { return unwrap(await request()); }
  catch (error) { throw new Error(message(error)); }
};

const hrWorkforceService = {
  getAttendanceReport: (params) => call(() => api.get('/hr/attendance-reports', { params })),
  getSchedules: () => call(() => api.get('/hr/work-schedules')),
  createSchedule: (payload) => call(() => api.post('/hr/work-schedules', payload)),
  updateSchedule: (id, payload) => call(() => api.put(`/hr/work-schedules/${id}`, payload)),
  deleteSchedule: (id) => call(() => api.delete(`/hr/work-schedules/${id}`)),
  getGroups: () => call(() => api.get('/hr/student-groups')),
  createGroup: (payload) => call(() => api.post('/hr/student-groups', payload)),
  updateGroup: (id, payload) => call(() => api.put(`/hr/student-groups/${id}`, payload)),
  addGroupMember: (groupId, studentId) => call(() => api.put(`/hr/student-groups/${groupId}/members/${studentId}`)),
  removeGroupMember: (groupId, studentId) => call(() => api.delete(`/hr/student-groups/${groupId}/members/${studentId}`)),
  getStudents: async () => {
    try {
      return await studentService.getStudents();
    } catch (error) {
      throw new Error(message(error, 'Không thể tải danh sách sinh viên.'));
    }
  },
  getLeaveRequests: (params) => call(() => api.get('/hr/leave-requests', { params })),
  reviewLeave: (id, payload) => call(() => api.post(`/hr/leave-requests/${id}/review`, payload)),
  getAllowances: (params) => call(() => api.get('/hr/allowances', { params })),
  getAllowance: (id) => call(() => api.get(`/hr/allowances/${id}`)),
  createAllowance: (studentId, payload) => call(() => api.post('/hr/allowances', payload, { params: { studentId } })),
  updateAllowance: (id, payload) => call(() => api.put(`/hr/allowances/${id}`, payload)),
  cancelAllowance: (id) => call(() => api.delete(`/hr/allowances/${id}`)),
  recordPayment: (id, payload) => call(() => api.post(`/hr/allowances/${id}/payments`, payload)),
  getSupportRequests: (params) => call(() => api.get('/hr/support-requests', { params })),
  updateSupportRequest: (id, payload) => call(() => api.put(`/hr/support-requests/${id}`, payload)),
};

export default hrWorkforceService;
