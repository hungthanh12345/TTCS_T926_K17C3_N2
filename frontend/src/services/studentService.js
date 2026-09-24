import api from './api';
import { getStoredStudents, saveStoredStudents, getStoredMentors } from './mockData';

export const studentService = {
  /**
   * Get all students with filtering, search, and pagination
   * GET /api/hr/students
   */
  async getStudents(params = {}) {
    try {
      const response = await api.get('/hr/students', { params });
      const payload = response.data?.data || response.data;
      const rawList = Array.isArray(payload) ? payload : (payload?.items || []);
      return rawList.map((s) => ({
        ...s,
        id: s.id,
        studentCode: s.studentCode,
        fullName: s.fullName,
        phone: s.phoneNumber || s.phone || '',
        phoneNumber: s.phoneNumber || s.phone || '',
        email: s.user?.email || s.email || `${s.studentCode?.toLowerCase()}@ictu.edu.vn`,
        university: s.university,
        major: s.major,
        mentorId: s.mentorId,
        assignedMentor: s.mentor || s.assignedMentor || null,
        mentor: s.mentor || s.assignedMentor || null,
        status: s.mentorId ? 'ACTIVE' : 'PENDING_ASSIGNMENT',
      }));
    } catch (error) {
      if (!error.response) {
        let students = getStoredStudents();

        if (params.search) {
          const q = params.search.toLowerCase().trim();
          students = students.filter(
            (s) =>
              s.fullName.toLowerCase().includes(q) ||
              s.studentCode.toLowerCase().includes(q) ||
              (s.email && s.email.toLowerCase().includes(q))
          );
        }

        if (params.university && params.university !== 'ALL') {
          students = students.filter((s) => s.university === params.university);
        }

        if (params.major && params.major !== 'ALL') {
          students = students.filter((s) => s.major === params.major);
        }

        if (params.status && params.status !== 'ALL') {
          students = students.filter((s) => s.status === params.status);
        }

        return students;
      }
      throw new Error(error.response?.data?.message || 'Không thể tải danh sách sinh viên.');
    }
  },

  /**
   * Get student details by ID
   * GET /api/hr/students/{id}
   */
  async getStudentById(id) {
    try {
      const response = await api.get(`/hr/students/${id}`);
      const s = response.data?.data || response.data;
      return {
        ...s,
        id: s.id,
        studentCode: s.studentCode,
        fullName: s.fullName,
        phone: s.phoneNumber || s.phone || '',
        phoneNumber: s.phoneNumber || s.phone || '',
        email: s.user?.email || s.email || `${s.studentCode?.toLowerCase()}@ictu.edu.vn`,
        university: s.university,
        major: s.major,
        mentorId: s.mentorId,
        assignedMentor: s.mentor || s.assignedMentor || null,
        mentor: s.mentor || s.assignedMentor || null,
      };
    } catch (error) {
      if (!error.response) {
        const students = getStoredStudents();
        const found = students.find((s) => s.id === id);
        if (!found) throw new Error('Student not found');
        return found;
      }
      throw new Error(error.response?.data?.message || 'Không thể tải chi tiết sinh viên.');
    }
  },

  /**
   * Create a new student
   * POST /api/hr/students
   */
  async createStudent(studentData) {
    try {
      const payload = {
        studentCode: studentData.studentCode?.trim().toUpperCase(),
        fullName: studentData.fullName?.trim(),
        phoneNumber: studentData.phoneNumber || studentData.phone || null,
        university: studentData.university?.trim(),
        major: studentData.major?.trim(),
        userId: studentData.userId ? parseInt(studentData.userId, 10) : null,
        mentorId: studentData.mentorId ? parseInt(studentData.mentorId, 10) : null,
      };
      const response = await api.post('/hr/students', payload);
      return response.data?.data || response.data;
    } catch (error) {
      if (!error.response) {
        const students = getStoredStudents();
        
        if (students.some((s) => s.studentCode.toLowerCase() === studentData.studentCode.trim().toLowerCase())) {
          throw new Error('Mã sinh viên này đã tồn tại trong hệ thống.');
        }

        const newStudent = {
          id: `STD-${Date.now().toString().slice(-4)}`,
          studentCode: studentData.studentCode.trim().toUpperCase(),
          fullName: studentData.fullName.trim(),
          email: studentData.email ? studentData.email.trim() : `${studentData.studentCode.toLowerCase()}@ictu.edu.vn`,
          phone: studentData.phone.trim(),
          phoneNumber: studentData.phone.trim(),
          university: studentData.university.trim(),
          major: studentData.major.trim(),
          status: 'PENDING_ASSIGNMENT',
          mentorId: null,
          assignedMentor: null,
          createdAt: new Date().toISOString(),
        };

        const updated = [newStudent, ...students];
        saveStoredStudents(updated);
        return newStudent;
      }
      throw new Error(error.response?.data?.message || 'Không thể tạo mới sinh viên.');
    }
  },

  /**
   * Update existing student profile
   * PUT /api/hr/students/{id}
   */
  async updateStudent(id, studentData) {
    try {
      const payload = {
        studentCode: studentData.studentCode?.trim().toUpperCase(),
        fullName: studentData.fullName?.trim(),
        phoneNumber: studentData.phoneNumber || studentData.phone || '',
        phone: studentData.phone || studentData.phoneNumber || '',
        university: studentData.university?.trim(),
        major: studentData.major?.trim(),
        userId: studentData.userId !== undefined ? studentData.userId : studentData.user?.id,
        mentorId: studentData.mentorId !== undefined ? studentData.mentorId : studentData.mentor?.id,
      };
      const response = await api.put(`/hr/students/${id}`, payload);
      const resData = response.data?.data || response.data;
      return {
        ...resData,
        phone: resData.phoneNumber || resData.phone,
        email: resData.user?.email || resData.email,
        assignedMentor: resData.mentor || resData.assignedMentor,
      };
    } catch (error) {
      if (!error.response) {
        const students = getStoredStudents();
        const index = students.findIndex((s) => s.id === id);
        if (index === -1) throw new Error('Không tìm thấy sinh viên');

        const updatedStudent = {
          ...students[index],
          ...studentData,
          studentCode: studentData.studentCode || students[index].studentCode,
          fullName: studentData.fullName || students[index].fullName,
          phone: studentData.phone || students[index].phone,
          university: studentData.university || students[index].university,
          major: studentData.major || students[index].major,
          updatedAt: new Date().toISOString(),
        };

        students[index] = updatedStudent;
        saveStoredStudents(students);
        return updatedStudent;
      }
      throw new Error(error.response?.data?.message || 'Không thể cập nhật hồ sơ sinh viên.');
    }
  },

  /**
   * Assign a Mentor to a Student
   * PUT /api/hr/students/{id}/assign-mentor
   * Payload: { mentorId }
   */
  async assignMentor(studentId, mentorId) {
    try {
      const parsedId = mentorId ? parseInt(mentorId, 10) : null;
      const response = await api.put(`/hr/students/${studentId}/assign-mentor`, { mentorId: parsedId });
      const resData = response.data?.data || response.data;
      return {
        ...resData,
        phone: resData.phoneNumber || resData.phone,
        email: resData.user?.email || resData.email,
        assignedMentor: resData.mentor || resData.assignedMentor,
      };
    } catch (error) {
      if (!error.response) {
        const students = getStoredStudents();
        const mentors = getStoredMentors();

        const sIndex = students.findIndex((s) => s.id === studentId);
        if (sIndex === -1) throw new Error('Không tìm thấy bản ghi sinh viên.');

        const mentor = mentors.find((m) => m.id === mentorId);
        if (!mentor && mentorId) throw new Error('Mentor được chọn không tồn tại.');

        students[sIndex] = {
          ...students[sIndex],
          mentorId: mentor ? mentor.id : null,
          assignedMentor: mentor
            ? {
                id: mentor.id,
                fullName: mentor.fullName,
                department: mentor.department,
              }
            : null,
          status: mentor ? 'ACTIVE' : 'PENDING_ASSIGNMENT',
        };

        saveStoredStudents(students);
        return students[sIndex];
      }
      throw new Error(error.response?.data?.message || 'Không thể phân công Mentor.');
    }
  },

  /**
   * Delete student record
   * DELETE /api/hr/students/{id}
   */
  async deleteStudent(id) {
    try {
      const response = await api.delete(`/hr/students/${id}`);
      return response.data;
    } catch (error) {
      if (!error.response) {
        const students = getStoredStudents();
        const updated = students.filter((s) => s.id !== id);
        saveStoredStudents(updated);
        return { success: true };
      }
      throw new Error(error.response?.data?.message || 'Không thể xóa hồ sơ sinh viên.');
    }
  },
};

export default studentService;
