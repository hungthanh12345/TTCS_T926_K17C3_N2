import api from './api';
import { getStoredMentors, saveStoredMentors, getStoredStudents } from './mockData';

export const mentorService = {
  /**
   * Get all mentors for HR Mentor Management View
   * GET /api/hr/mentors
   */
  async getMentors(params = {}) {
    try {
      const response = await api.get('/hr/mentors', { params });
      const payload = response.data?.data || response.data;
      const list = Array.isArray(payload) ? payload : payload?.items || [];
      return list.map((m) => ({
        ...m,
        id: m.id,
        fullName: m.fullName,
        email: m.email || '',
        phone: m.phoneNumber || m.phone || '',
        phoneNumber: m.phoneNumber || m.phone || '',
        department: m.department || '',
        specialization: m.specialization || '',
        activeMentees:
          m.assignedStudentsCount !== undefined
            ? m.assignedStudentsCount
            : m.activeMentees || 0,
        assignedStudentsCount:
          m.assignedStudentsCount !== undefined
            ? m.assignedStudentsCount
            : m.activeMentees || 0,
      }));
    } catch (error) {
      if (!error.response) {
        let mentors = getStoredMentors();
        const students = getStoredStudents();

        // Calculate dynamic active mentees count
        mentors = mentors.map((m) => {
          const menteeCount = students.filter((s) => s.mentorId === m.id).length;
          return {
            ...m,
            activeMentees: menteeCount,
            assignedStudentsCount: menteeCount,
          };
        });

        if (params.search) {
          const q = params.search.toLowerCase().trim();
          mentors = mentors.filter(
            (m) =>
              m.fullName.toLowerCase().includes(q) ||
              m.department.toLowerCase().includes(q) ||
              m.specialization.toLowerCase().includes(q)
          );
        }

        if (params.department && params.department !== 'ALL') {
          mentors = mentors.filter((m) => m.department === params.department);
        }

        return mentors;
      }
      throw new Error(error.response?.data?.message || 'Không thể tải danh sách Mentor.');
    }
  },

  /**
   * Get mentor by ID
   * GET /api/hr/mentors/{id}
   */
  async getMentorById(id) {
    try {
      const response = await api.get(`/hr/mentors/${id}`);
      const m = response.data?.data || response.data;
      return {
        ...m,
        id: m.id,
        fullName: m.fullName,
        email: m.email || '',
        phone: m.phoneNumber || m.phone || '',
        phoneNumber: m.phoneNumber || m.phone || '',
        department: m.department || '',
        specialization: m.specialization || '',
        activeMentees:
          m.assignedStudentsCount !== undefined
            ? m.assignedStudentsCount
            : m.activeMentees || 0,
      };
    } catch (error) {
      if (!error.response) {
        const mentors = getStoredMentors();
        const found = mentors.find((m) => m.id === id);
        if (!found) throw new Error('Không tìm thấy Mentor');
        return found;
      }
      throw new Error(error.response?.data?.message || 'Không thể tải chi tiết Mentor.');
    }
  },

  /**
   * Create a new mentor
   * POST /api/hr/mentors
   * Payload: { fullName, email, phone, department, specialization }
   */
  async createMentor(mentorData) {
    try {
      const payload = {
        fullName: mentorData.fullName?.trim(),
        email: mentorData.email?.trim() || null,
        phoneNumber: mentorData.phoneNumber || mentorData.phone || null,
        phone: mentorData.phone || mentorData.phoneNumber || null,
        department: mentorData.department?.trim(),
        specialization: mentorData.specialization?.trim() || null,
      };
      const response = await api.post('/hr/mentors', payload);
      const resData = response.data?.data || response.data;
      return {
        ...resData,
        phone: resData.phoneNumber || resData.phone,
        activeMentees: resData.assignedStudentsCount || 0,
      };
    } catch (error) {
      if (!error.response) {
        const mentors = getStoredMentors();

        const newMentor = {
          id: `MNT-${Date.now().toString().slice(-3)}`,
          fullName: mentorData.fullName.trim(),
          email: mentorData.email
            ? mentorData.email.trim()
            : `${mentorData.fullName.toLowerCase().replace(/\s+/g, '.')}@ictu.edu.vn`,
          phone: mentorData.phone.trim(),
          department: mentorData.department.trim(),
          specialization: mentorData.specialization.trim(),
          activeMentees: 0,
          createdAt: new Date().toISOString(),
        };

        const updated = [newMentor, ...mentors];
        saveStoredMentors(updated);
        return newMentor;
      }
      throw new Error(error.response?.data?.message || 'Không thể tạo mới hồ sơ Mentor.');
    }
  },

  /**
   * Delete mentor
   * DELETE /api/hr/mentors/{id}
   */
  async deleteMentor(id) {
    try {
      const response = await api.delete(`/hr/mentors/${id}`);
      return response.data;
    } catch (error) {
      if (!error.response) {
        const mentors = getStoredMentors();
        const updated = mentors.filter((m) => m.id !== id);
        saveStoredMentors(updated);
        return { success: true };
      }
      throw new Error(error.response?.data?.message || 'Không thể xóa hồ sơ Mentor.');
    }
  },
};

export default mentorService;
