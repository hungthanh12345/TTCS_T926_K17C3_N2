import api from './api';

const unwrap = (response) => response.data?.data || response.data;
const errorMessage = (error, fallback) => error.response?.data?.message || error.message || fallback;

const normalizeMentor = (mentor) => ({
  ...mentor,
  id: mentor.id,
  fullName: mentor.fullName,
  email: mentor.email || '',
  phone: mentor.phoneNumber || mentor.phone || '',
  phoneNumber: mentor.phoneNumber || mentor.phone || '',
  department: mentor.department || '',
  specialization: mentor.specialization || '',
  activeMentees: mentor.assignedStudentsCount ?? mentor.activeMentees ?? 0,
  assignedStudentsCount: mentor.assignedStudentsCount ?? mentor.activeMentees ?? 0,
});

export const mentorService = {
  async getMentors(params = {}) {
    try {
      const payload = unwrap(await api.get('/hr/mentors', { params }));
      const list = Array.isArray(payload) ? payload : payload?.items || [];
      return list.map(normalizeMentor);
    } catch (error) {
      throw new Error(errorMessage(error, 'Không thể tải danh sách Mentor.'));
    }
  },

  async getMentorById(id) {
    try {
      return normalizeMentor(unwrap(await api.get(`/hr/mentors/${id}`)));
    } catch (error) {
      throw new Error(errorMessage(error, 'Không thể tải chi tiết Mentor.'));
    }
  },

  async createMentor(mentorData) {
    try {
      const payload = {
        fullName: mentorData.fullName?.trim(),
        email: mentorData.email?.trim() || null,
        password: mentorData.password || null,
        phoneNumber: mentorData.phoneNumber || mentorData.phone || null,
        phone: mentorData.phone || mentorData.phoneNumber || null,
        department: mentorData.department?.trim(),
        specialization: mentorData.specialization?.trim() || null,
      };
      return normalizeMentor(unwrap(await api.post('/hr/mentors', payload)));
    } catch (error) {
      throw new Error(errorMessage(error, 'Không thể tạo mới hồ sơ Mentor.'));
    }
  },

  async deleteMentor(id) {
    try {
      return (await api.delete(`/hr/mentors/${id}`)).data;
    } catch (error) {
      throw new Error(errorMessage(error, 'Không thể xóa hồ sơ Mentor.'));
    }
  },
};

export default mentorService;
