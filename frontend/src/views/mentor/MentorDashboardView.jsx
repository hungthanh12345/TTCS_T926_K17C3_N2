import React, { useCallback, useEffect, useState } from 'react';
import { useAuth } from '../../context/AuthContext';
import {
  AlertCircle,
  Award,
  BookOpen,
  Building2,
  ClipboardList,
  Mail,
  Phone,
  RefreshCw,
  School,
  UserRound,
} from 'lucide-react';
import DashboardLayout from '../../components/layout/DashboardLayout';
import WorkspaceTabs from '../../components/common/WorkspaceTabs';
import MentorTaskManagement from '../../components/mentor/MentorTaskManagement';
import MentorWeeklyReportsPanel from '../../components/mentor/MentorWeeklyReportsPanel';
import MentorEvaluationPanel from '../../components/mentor/MentorEvaluationPanel';
import TableSkeleton from '../../components/common/TableSkeleton';
import mentorTaskService from '../../services/mentorTaskService';

const TABS = [
  { id: 'students', label: 'Sinh viên', icon: UserRound },
  { id: 'tasks', label: 'Công việc', icon: ClipboardList },
  { id: 'reports', label: 'Báo cáo tuần', icon: BookOpen },
  { id: 'evaluations', label: 'Đánh giá', icon: Award },
];

export const MentorDashboardView = () => {
  const { user } = useAuth();
  const [activeTab, setActiveTab] = useState('students');
  const [students, setStudents] = useState([]);
  const [loadError, setLoadError] = useState('');
  const [isLoading, setIsLoading] = useState(true);

  const loadMentees = useCallback(async () => {
    try {
      const assignedStudents = await mentorTaskService.getAssignedStudents();
      setStudents(assignedStudents);
      setLoadError('');
    } catch (error) {
      setLoadError(error.message || 'Không thể tải danh sách sinh viên được phân công.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadMentees();
  }, [loadMentees]);

  const refreshMentees = () => {
    setIsLoading(true);
    setLoadError('');
    void loadMentees();
  };

  return (
    <DashboardLayout
      title="Không gian Mentor"
      subtitle="Sinh viên được phân công, công việc, báo cáo và đánh giá"
    >
      <div className="space-y-5">
        <section className="relative overflow-hidden rounded-2xl bg-gradient-to-r from-purple-950 via-indigo-950 to-slate-900 p-5 text-white shadow-lg sm:p-7">
          <div className="pointer-events-none absolute -right-12 -top-16 h-56 w-56 rounded-full bg-purple-400/20 blur-3xl" />
          <div className="relative z-10 max-w-2xl">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-purple-200">Không gian hướng dẫn</p>
            <h2 className="mt-2 text-2xl font-bold">Xin chào, {user?.fullName || 'Mentor'}!</h2>
            <p className="mt-1 text-sm leading-6 text-slate-300">
              Xem danh sách sinh viên được HR phân công, giao nhiệm vụ, phản hồi báo cáo tuần và ghi nhận đánh giá.
            </p>
          </div>
        </section>

        <WorkspaceTabs
          label="Các mục trong không gian mentor"
          tabs={TABS.map((tab) => ({ ...tab, count: tab.id === 'students' && !isLoading && !loadError ? students.length : undefined }))}
          activeTab={activeTab}
          onChange={setActiveTab}
        />

        {activeTab === 'students' && (
          <section className="space-y-4" aria-label="Sinh viên được phân công">
            <div className="flex flex-wrap items-end justify-between gap-3">
              <div>
                <h3 className="text-base font-bold text-slate-900">Sinh viên được phân công</h3>
                <p className="mt-1 text-xs text-slate-500">Danh sách lấy từ hồ sơ mentor trong hệ thống.</p>
              </div>
              <button
                type="button"
                onClick={refreshMentees}
                disabled={isLoading}
                className="inline-flex items-center gap-2 rounded-lg border border-slate-200 bg-white px-3 py-2 text-xs font-semibold text-slate-600 shadow-sm hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
              >
                <RefreshCw className={`h-3.5 w-3.5 ${isLoading ? 'animate-spin' : ''}`} /> Làm mới
              </button>
            </div>

            {isLoading ? (
              <TableSkeleton rows={3} cols={3} />
            ) : loadError ? (
              <div role="alert" className="rounded-2xl border border-rose-200 bg-rose-50 p-5 text-sm text-rose-800">
                <div className="flex items-start gap-3">
                  <AlertCircle className="mt-0.5 h-5 w-5 shrink-0" />
                  <div className="min-w-0 flex-1">
                    <p className="font-semibold">Không thể tải danh sách sinh viên</p>
                    <p className="mt-1 break-words">{loadError}</p>
                    <button type="button" onClick={refreshMentees} className="mt-3 rounded-lg border border-rose-300 px-3 py-2 text-xs font-semibold hover:bg-rose-100">Thử lại</button>
                  </div>
                </div>
              </div>
            ) : students.length === 0 ? (
              <div role="status" className="rounded-2xl border border-dashed border-slate-300 bg-white px-5 py-10 text-center">
                <UserRound className="mx-auto h-8 w-8 text-slate-300" />
                <h4 className="mt-3 text-sm font-bold text-slate-800">Chưa có sinh viên được phân công</h4>
                <p className="mt-1 text-sm text-slate-500">Khi HR liên kết sinh viên với tài khoản mentor này, hồ sơ sẽ xuất hiện tại đây.</p>
              </div>
            ) : (
              <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                {students.map((student) => (
                  <article key={student.id} className="flex min-w-0 flex-col rounded-2xl border border-slate-200 bg-white p-4 shadow-sm transition-shadow hover:shadow-md">
                    <div className="flex items-start justify-between gap-3">
                      <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-indigo-50 font-bold text-indigo-700">
                        {student.fullName?.trim()?.charAt(0)?.toUpperCase() || '?'}
                      </span>
                      <span className="max-w-[65%] truncate rounded-md bg-slate-100 px-2 py-1 font-mono text-[10px] font-bold text-slate-600">
                        {student.studentCode || 'Chưa có mã'}
                      </span>
                    </div>
                    <h4 className="mt-3 truncate text-sm font-bold text-slate-900">{student.fullName || 'Chưa có họ tên'}</h4>
                    <div className="mt-3 space-y-2 text-xs text-slate-600">
                      {student.university && <p className="flex items-start gap-2"><School className="mt-0.5 h-3.5 w-3.5 shrink-0 text-slate-400" /><span>{student.university}</span></p>}
                      {student.major && <p className="flex items-start gap-2"><Building2 className="mt-0.5 h-3.5 w-3.5 shrink-0 text-indigo-500" /><span>{student.major}</span></p>}
                      {student.email && <p className="flex items-start gap-2"><Mail className="mt-0.5 h-3.5 w-3.5 shrink-0 text-slate-400" /><span className="break-all">{student.email}</span></p>}
                      {(student.phoneNumber || student.phone) && <p className="flex items-start gap-2"><Phone className="mt-0.5 h-3.5 w-3.5 shrink-0 text-slate-400" /><span>{student.phoneNumber || student.phone}</span></p>}
                      {student.programName && <p className="border-t border-slate-100 pt-2 text-slate-500">Chương trình: {student.programName}</p>}
                    </div>
                    <button
                      type="button"
                      onClick={() => setActiveTab('tasks')}
                      className="mt-4 rounded-lg border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700 hover:border-indigo-200 hover:bg-indigo-50 hover:text-indigo-700"
                    >
                      Mở công việc
                    </button>
                  </article>
                ))}
              </div>
            )}
          </section>
        )}

        {activeTab === 'tasks' && <MentorTaskManagement />}
        {activeTab === 'reports' && <MentorWeeklyReportsPanel />}
        {activeTab === 'evaluations' && <MentorEvaluationPanel />}
      </div>
    </DashboardLayout>
  );
};

export default MentorDashboardView;
