import React from 'react';
import DashboardLayout from '../../components/layout/DashboardLayout';
import StudentAttendancePanel from '../../components/student/StudentAttendancePanel';

export const StudentAttendanceView = () => {
  return (
    <DashboardLayout
      title="Chấm công thực tập"
      subtitle="Ghi nhận thời gian làm việc hàng ngày và theo dõi lịch sử chấm công"
    >
      <StudentAttendancePanel />
    </DashboardLayout>
  );
};

export default StudentAttendanceView;
