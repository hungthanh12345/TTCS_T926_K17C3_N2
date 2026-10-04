import api from './api';

const unwrap = (response) => response.data?.data || response.data;
const errorMessage = (error, fallback) => error.response?.data?.message || error.message || fallback;

const normalizeStudent = (student) => ({
  ...student,
  id: student.id,
  studentCode: student.studentCode,
  fullName: student.fullName,
  phone: student.phoneNumber || student.phone || '',
  phoneNumber: student.phoneNumber || student.phone || '',
  email: student.user?.email || student.email || '',
  userId: student.userId ?? student.user?.id ?? null,
  accountStatus: student.user?.status || null,
  university: student.university,
  major: student.major,
  programId: student.programId,
  programName: student.programName,
  mentorId: student.mentorId,
  assignedMentor: student.mentor || student.assignedMentor || null,
  mentor: student.mentor || student.assignedMentor || null,
  status: student.mentorId ? 'ACTIVE' : 'PENDING_ASSIGNMENT',
});

export const studentService = {
  async getMyProfile() {
    try {
      const student = unwrap(await api.get('/student/profile'));
      return normalizeStudent(student);
    } catch (error) {
      throw new Error(errorMessage(error, 'Không thể tải hồ sơ sinh viên của bạn.'));
    }
  },

  async getStudents(params = {}) {
    try {
      const pageSize = Math.min(100, Math.max(1, Number(params.pageSize) || 100));
      const requestedPage = Number(params.page);
      const shouldLoadAllPages = !Number.isInteger(requestedPage) || requestedPage < 1;
      const firstPage = shouldLoadAllPages ? 1 : requestedPage;
      const response = await api.get('/hr/students', {
        params: { ...params, page: firstPage, pageSize },
      });
      const payload = unwrap(response);
      let students = Array.isArray(payload) ? payload : (payload?.items || []);

      if (shouldLoadAllPages && !Array.isArray(payload)) {
        const totalPages = payload?.totalPages || Math.ceil((payload?.totalItems || students.length) / pageSize);
        for (let page = 2; page <= totalPages; page += 1) {
          const nextPayload = unwrap(await api.get('/hr/students', { params: { ...params, page, pageSize } }));
          students = students.concat(Array.isArray(nextPayload) ? nextPayload : (nextPayload?.items || []));
        }
      }

      return students.map(normalizeStudent);
    } catch (error) {
      throw new Error(errorMessage(error, 'Không thể tải danh sách sinh viên.'));
    }
  },

  async getStudentAccountLinks() {
    try {
      return unwrap(await api.get('/hr/student-accounts'));
    } catch (error) {
      throw new Error(errorMessage(error, 'Không thể tải trạng thái liên kết tài khoản sinh viên.'));
    }
  },

  async getStudentById(id) {
    try {
      return normalizeStudent(unwrap(await api.get(`/hr/students/${id}`)));
    } catch (error) {
      throw new Error(errorMessage(error, 'Không thể tải chi tiết sinh viên.'));
    }
  },

  async createStudent(studentData) {
    if (!studentData.userId) throw new Error('Vui lòng chọn tài khoản ROLE_STUDENT đã tồn tại.');
    try {
      const payload = {
        studentCode: studentData.studentCode?.trim().toUpperCase(),
        fullName: studentData.fullName?.trim(),
        phoneNumber: studentData.phoneNumber || studentData.phone || null,
        university: studentData.university?.trim(),
        major: studentData.major?.trim(),
        userId: Number(studentData.userId),
        mentorId: studentData.mentorId ? Number(studentData.mentorId) : null,
      };
      return unwrap(await api.post('/hr/students', payload));
    } catch (error) {
      throw new Error(errorMessage(error, 'Không thể liên kết hồ sơ sinh viên.'));
    }
  },

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
      const result = unwrap(await api.put(`/hr/students/${id}`, payload));
      return {
        ...result,
        phone: result.phoneNumber || result.phone,
        email: result.user?.email || result.email,
        assignedMentor: result.mentor || result.assignedMentor,
      };
    } catch (error) {
      throw new Error(errorMessage(error, 'Không thể cập nhật hồ sơ sinh viên.'));
    }
  },

  async assignMentor(studentId, mentorId) {
    try {
      const result = unwrap(await api.put(`/hr/students/${studentId}/assign-mentor`, {
        mentorId: mentorId ? Number(mentorId) : null,
      }));
      return {
        ...result,
        phone: result.phoneNumber || result.phone,
        email: result.user?.email || result.email,
        assignedMentor: result.mentor || result.assignedMentor,
      };
    } catch (error) {
      throw new Error(errorMessage(error, 'Không thể phân công Mentor.'));
    }
  },

  async deleteStudent(id) {
    try {
      return (await api.delete(`/hr/students/${id}`)).data;
    } catch (error) {
      throw new Error(errorMessage(error, 'Không thể xóa hồ sơ sinh viên.'));
    }
  },
};

export default studentService;
