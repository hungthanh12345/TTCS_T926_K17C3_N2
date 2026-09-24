// Initial mock seed data for development and offline testing

const INITIAL_USERS = [
  {
    id: 'USR-001',
    email: 'hung.nt.admin@gmail.com',
    role: 'ROLE_ADMIN',
    status: 'ACTIVE',
    createdAt: '2026-09-01T08:00:00Z',
  },
  {
    id: 'USR-002',
    email: 'customer.hr@company.com',
    role: 'ROLE_HR',
    status: 'ACTIVE',
    createdAt: '2026-09-05T09:30:00Z',
  },
  {
    id: 'USR-003',
    email: 'tung.nk@gmail.com',
    role: 'ROLE_MENTOR',
    status: 'ACTIVE',
    createdAt: '2026-09-10T11:15:00Z',
  },
  {
    id: 'USR-004',
    email: 'hung.nt@gmail.com',
    role: 'ROLE_STUDENT',
    status: 'ACTIVE',
    createdAt: '2026-09-12T14:20:00Z',
  },
  {
    id: 'USR-005',
    email: 'hung.dm@gmail.com',
    role: 'ROLE_STUDENT',
    status: 'ACTIVE',
    createdAt: '2026-09-12T14:20:00Z',
  }
];

const INITIAL_MENTORS = [
  {
    id: '1',
    fullName: 'Nguyễn Khánh Tùng',
    email: 'tung.nk@gmail.com',
    phone: '0912345678',
    department: 'Kỹ thuật phần mềm',
    specialization: 'Full-stack Web và Cloud Native',
    activeMentees: 10,
    avatarUrl: 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150&auto=format&fit=crop&q=80',
    createdAt: '2026-08-15T08:00:00Z'
  }
];

const INITIAL_STUDENTS = [
  {
    id: 1,
    studentCode: 'SV2026001',
    fullName: 'Nguyễn Thành Hưng',
    email: 'hung.nt@gmail.com',
    phone: '0987654321',
    phoneNumber: '0987654321',
    university: 'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN',
    major: 'Kỹ thuật Phần mềm',
    status: 'ACTIVE',
    mentorId: 1,
    assignedMentor: {
      id: 1,
      fullName: 'Nguyễn Khánh Tùng',
      department: 'Kỹ thuật phần mềm',
      specialization: 'Full-stack Web và Cloud Native'
    },
    mentor: {
      id: 1,
      fullName: 'Nguyễn Khánh Tùng',
      department: 'Kỹ thuật phần mềm',
      specialization: 'Full-stack Web và Cloud Native'
    },
    createdAt: '2026-09-10T10:00:00Z'
  },
  {
    id: 2,
    studentCode: 'SV2026002',
    fullName: 'Dương Minh Hưng',
    email: 'hung.dm@gmail.com',
    phone: '0987654322',
    phoneNumber: '0987654322',
    university: 'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN',
    major: 'Kỹ thuật Phần mềm',
    status: 'ACTIVE',
    mentorId: 1,
    assignedMentor: {
      id: 1,
      fullName: 'Nguyễn Khánh Tùng',
      department: 'Kỹ thuật phần mềm',
      specialization: 'Full-stack Web và Cloud Native'
    },
    mentor: {
      id: 1,
      fullName: 'Nguyễn Khánh Tùng',
      department: 'Kỹ thuật phần mềm',
      specialization: 'Full-stack Web và Cloud Native'
    },
    createdAt: '2026-09-11T11:20:00Z'
  },
  {
    id: 3,
    studentCode: 'SV2026003',
    fullName: 'Hoàng Thanh Hùng',
    email: 'hung.ht@gmail.com',
    phone: '0987654323',
    phoneNumber: '0987654323',
    university: 'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN',
    major: 'Kỹ thuật Phần mềm',
    status: 'ACTIVE',
    mentorId: 1,
    assignedMentor: {
      id: 1,
      fullName: 'Nguyễn Khánh Tùng',
      department: 'Kỹ thuật phần mềm',
      specialization: 'Full-stack Web và Cloud Native'
    },
    mentor: {
      id: 1,
      fullName: 'Nguyễn Khánh Tùng',
      department: 'Kỹ thuật phần mềm',
      specialization: 'Full-stack Web và Cloud Native'
    },
    createdAt: '2026-09-12T13:45:00Z'
  },
  {
    id: 4,
    studentCode: 'SV2026004',
    fullName: 'Trương Đình Giang',
    email: 'giang.td@gmail.com',
    phone: '0987654324',
    phoneNumber: '0987654324',
    university: 'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN',
    major: 'Kỹ thuật Phần mềm',
    status: 'ACTIVE',
    mentorId: 1,
    assignedMentor: {
      id: 1,
      fullName: 'Nguyễn Khánh Tùng',
      department: 'Kỹ thuật phần mềm',
      specialization: 'Full-stack Web và Cloud Native'
    },
    mentor: {
      id: 1,
      fullName: 'Nguyễn Khánh Tùng',
      department: 'Kỹ thuật phần mềm',
      specialization: 'Full-stack Web và Cloud Native'
    },
    createdAt: '2026-09-14T09:15:00Z'
  },
  {
    id: 5,
    studentCode: 'SV2026005',
    fullName: 'Dương Hải Dương',
    email: 'duong.dh@gmail.com',
    phone: '0987654325',
    phoneNumber: '0987654325',
    university: 'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN',
    major: 'Kỹ thuật Phần mềm',
    status: 'ACTIVE',
    mentorId: 1,
    assignedMentor: {
      id: 1,
      fullName: 'Nguyễn Khánh Tùng',
      department: 'Kỹ thuật phần mềm',
      specialization: 'Full-stack Web và Cloud Native'
    },
    mentor: {
      id: 1,
      fullName: 'Nguyễn Khánh Tùng',
      department: 'Kỹ thuật phần mềm',
      specialization: 'Full-stack Web và Cloud Native'
    },
    createdAt: '2026-09-16T15:30:00Z'
  },
  {
    id: 6,
    studentCode: 'SV2026006',
    fullName: 'Diệp Đình Hân',
    email: 'han.dd@gmail.com',
    phone: '0987654326',
    phoneNumber: '0987654326',
    university: 'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN',
    major: 'Kỹ thuật Phần mềm',
    status: 'ACTIVE',
    mentorId: 1,
    assignedMentor: {
      id: 1,
      fullName: 'Nguyễn Khánh Tùng',
      department: 'Kỹ thuật phần mềm',
      specialization: 'Full-stack Web và Cloud Native'
    },
    mentor: {
      id: 1,
      fullName: 'Nguyễn Khánh Tùng',
      department: 'Kỹ thuật phần mềm',
      specialization: 'Full-stack Web và Cloud Native'
    },
    createdAt: '2026-09-18T10:00:00Z'
  },
  {
    id: 7,
    studentCode: 'SV2026007',
    fullName: 'Bùi Ngọc Huân',
    email: 'huan.bn@gmail.com',
    phone: '0987654327',
    phoneNumber: '0987654327',
    university: 'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN',
    major: 'Kỹ thuật Phần mềm',
    status: 'ACTIVE',
    mentorId: 1,
    assignedMentor: {
      id: 1,
      fullName: 'Nguyễn Khánh Tùng',
      department: 'Kỹ thuật phần mềm',
      specialization: 'Full-stack Web và Cloud Native'
    },
    mentor: {
      id: 1,
      fullName: 'Nguyễn Khánh Tùng',
      department: 'Kỹ thuật phần mềm',
      specialization: 'Full-stack Web và Cloud Native'
    },
    createdAt: '2026-09-19T10:00:00Z'
  },
  {
    id: 8,
    studentCode: 'SV2026008',
    fullName: 'Nguyễn Hồng Hải',
    email: 'hai.nh@gmail.com',
    phone: '0987654328',
    phoneNumber: '0987654328',
    university: 'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN',
    major: 'Kỹ thuật Phần mềm',
    status: 'ACTIVE',
    mentorId: 1,
    assignedMentor: {
      id: 1,
      fullName: 'Nguyễn Khánh Tùng',
      department: 'Kỹ thuật phần mềm',
      specialization: 'Full-stack Web và Cloud Native'
    },
    mentor: {
      id: 1,
      fullName: 'Nguyễn Khánh Tùng',
      department: 'Kỹ thuật phần mềm',
      specialization: 'Full-stack Web và Cloud Native'
    },
    createdAt: '2026-09-20T10:00:00Z'
  },
  {
    id: 9,
    studentCode: 'SV2026009',
    fullName: 'Phạm Hải Hướng',
    email: 'huong.ph@gmail.com',
    phone: '0987654329',
    phoneNumber: '0987654329',
    university: 'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN',
    major: 'Kỹ thuật Phần mềm',
    status: 'ACTIVE',
    mentorId: 1,
    assignedMentor: {
      id: 1,
      fullName: 'Nguyễn Khánh Tùng',
      department: 'Kỹ thuật phần mềm',
      specialization: 'Full-stack Web và Cloud Native'
    },
    mentor: {
      id: 1,
      fullName: 'Nguyễn Khánh Tùng',
      department: 'Kỹ thuật phần mềm',
      specialization: 'Full-stack Web và Cloud Native'
    },
    createdAt: '2026-09-21T10:00:00Z'
  },
  {
    id: 10,
    studentCode: 'SV2026010',
    fullName: 'Nguyễn Thị Giang',
    email: 'giang.nt@gmail.com',
    phone: '0987654330',
    phoneNumber: '0987654330',
    university: 'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN',
    major: 'Kỹ thuật Phần mềm',
    status: 'ACTIVE',
    mentorId: 1,
    assignedMentor: {
      id: 1,
      fullName: 'Nguyễn Khánh Tùng',
      department: 'Kỹ thuật phần mềm',
      specialization: 'Full-stack Web và Cloud Native'
    },
    mentor: {
      id: 1,
      fullName: 'Nguyễn Khánh Tùng',
      department: 'Kỹ thuật phần mềm',
      specialization: 'Full-stack Web và Cloud Native'
    },
    createdAt: '2026-09-22T10:00:00Z'
  }
];

// Helper functions for persistent mock simulation in localStorage
export const getStoredUsers = () => {
  const data = localStorage.getItem('mock_users');
  if (!data) {
    localStorage.setItem('mock_users', JSON.stringify(INITIAL_USERS));
    return INITIAL_USERS;
  }
  return JSON.parse(data);
};

export const saveStoredUsers = (users) => {
  localStorage.setItem('mock_users', JSON.stringify(users));
};

export const getStoredMentors = () => {
  const data = localStorage.getItem('mock_mentors');
  if (!data) {
    localStorage.setItem('mock_mentors', JSON.stringify(INITIAL_MENTORS));
    return INITIAL_MENTORS;
  }
  return JSON.parse(data);
};

export const saveStoredMentors = (mentors) => {
  localStorage.setItem('mock_mentors', JSON.stringify(mentors));
};

export const getStoredStudents = () => {
  const data = localStorage.getItem('mock_students');
  if (!data) {
    localStorage.setItem('mock_students', JSON.stringify(INITIAL_STUDENTS));
    return INITIAL_STUDENTS;
  }
  return JSON.parse(data);
};

export const saveStoredStudents = (students) => {
  localStorage.setItem('mock_students', JSON.stringify(students));
};
