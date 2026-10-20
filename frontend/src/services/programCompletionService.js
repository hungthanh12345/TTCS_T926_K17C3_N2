import api from './api';
import { apiErrorMessage } from './apiErrorMessage';

const unwrap = (response) => response.data?.data ?? response.data;
const fallbackMessage = 'Không thể tải hoặc cập nhật thống kê hoàn thành chương trình.';

const call = async (request) => {
  try {
    return unwrap(await request());
  } catch (error) {
    throw new Error(apiErrorMessage(error, fallbackMessage));
  }
};

const programCompletionService = {
  getDashboard: (programId) => call(() => api.get('/hr/program-completion', {
    params: programId ? { programId } : undefined,
  })),
  updateParticipation: (studentId, payload) => call(() => api.put(
    `/hr/program-completion/students/${studentId}/participation`,
    payload,
  )),
  confirmCompletion: (studentId, programId, reason) => call(() => api.post(
    `/hr/program-completion/students/${studentId}/programs/${programId}/confirm`,
    { reason: reason || null },
  )),
  revokeCompletion: (studentId, programId, reason) => call(() => api.post(
    `/hr/program-completion/students/${studentId}/programs/${programId}/revoke`,
    { reason: reason || null },
  )),
  getHistory: (studentId, programId) => call(() => api.get(
    `/hr/program-completion/students/${studentId}/programs/${programId}/history`,
  )),
};

export default programCompletionService;
