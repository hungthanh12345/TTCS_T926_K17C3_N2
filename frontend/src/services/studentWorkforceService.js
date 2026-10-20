import api from './api';
import { apiErrorMessage } from './apiErrorMessage';

const unwrap = (response) => response.data?.data ?? response.data;
const message = (error, fallback) => apiErrorMessage(error, fallback);

const call = async (request) => {
  try { return unwrap(await request()); }
  catch (error) { throw new Error(message(error)); }
};

const studentWorkforceService = {
  getTodayAttendance: () => call(() => api.get('/student/attendance/today')),
  checkIn: (notes) => call(() => api.post('/student/attendance/check-in', { notes: notes || null })),
  checkOut: () => call(() => api.post('/student/attendance/check-out')),
  getAttendanceHistory: (params) => call(() => api.get('/student/attendance/history', { params })),
  getLeaveRequests: () => call(() => api.get('/student/leave-requests')),
  createLeaveRequest: (payload) => call(() => api.post('/student/leave-requests', payload)),
  getAllowanceHistory: (params) => call(() => api.get('/student/allowances/history', { params })),
  getSupportRequests: () => call(() => api.get('/student/support-requests')),
  createSupportRequest: (payload) => call(() => api.post('/student/support-requests', payload)),
};

export default studentWorkforceService;
