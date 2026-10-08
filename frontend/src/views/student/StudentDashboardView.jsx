import React, { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  AlertCircle,
  Award,
  BookOpen,
  Building2,
  CalendarDays,
  FileText,
  GraduationCap,
  Mail,
  Phone,
  RefreshCw,
  School,
  ShieldCheck,
  UserRound,
} from 'lucide-react';
import DashboardLayout from '../../components/layout/DashboardLayout';
import WorkspaceTabs from '../../components/common/WorkspaceTabs';
import Badge from '../../components/common/Badge';
import StudentDocumentsPanel from '../../components/student/StudentDocumentsPanel';
import StudentTasksPanel from '../../components/student/StudentTasksPanel';
import StudentWeeklyReportsPanel from '../../components/student/StudentWeeklyReportsPanel';
import StudentEvaluationPanel from '../../components/student/StudentEvaluationPanel';
import studentService from '../../services/studentService';
import StudentScheduleView from './StudentScheduleView';

const ACCOUNT_STATUS_LABELS = {
  ACTIVE: 'Đang hoạt động',
  PENDING_APPROVAL: 'Đang chờ duyệt',
  INACTIVE: 'Không hoạt động',
  LOCKED: 'Đã khóa',
  REJECTED: 'Đã từ chối',
};

const TABS = [
  { id: 'overview', label: 'Tổng quan', icon: GraduationCap },
  { id: 'documents', label: 'Hồ sơ & tài liệu', icon: FileText },
  { id: 'schedule', label: 'Lịch thực tập', icon: CalendarDays },
  { id: 'tasks', label: 'Công việc', icon: ShieldCheck },
  { id: 'reports', label: 'Báo cáo tuần', icon: BookOpen },
  { id: 'evaluation', label: 'Đánh giá', icon: Award },
];

const InfoCard = ({ icon: Icon, label, value }) => (
  <div className="flex min-w-0 items-start gap-3 rounded-xl border border-slate-100 bg-slate-50 p-3.5">
    <span className="mt-0.5 rounded-lg bg-white p-2 text-indigo-600 shadow-sm">
      <Icon className="h-4 w-4" aria-hidden="true" />
    </span>
    <div className="min-w-0">
      <p className="text-[10px] font-bold uppercase tracking-wide text-slate-400">{label}</p>
      <p className="mt-1 break-words text-sm font-medium text-slate-800">{value || 'Chưa cập nhật'}</p>
    </div>
  </div>
);

export const StudentDashboardView = () => {
  const [activeTab, setActiveTab] = useState('overview');
  const [profile, setProfile] = useState(null);
  const [profileError, setProfileError] = useState('');
  const [isLoading, setIsLoading] = useState(true);

  const loadStudentData = useCallback(async () => {
    try {
      const studentProfile = await studentService.getMyProfile();
      setProfile(studentProfile);
      setProfileError('');
    } catch (error) {
      setProfileError(error.message || 'Không thể tải hồ sơ sinh viên của bạn.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadStudentData();
  }, [loadStudentData]);

  const refreshStudentData = () => {
    setIsLoading(true);
    setProfileError('');
    void loadStudentData();
  };

  const accountStatus = profile?.user?.status;

  return (
    <DashboardLayout
      title="Không gian sinh viên"
      subtitle="Hồ sơ, lịch thực tập, nhiệm vụ và trao đổi với mentor"
    >
      <div className="space-y-5">
        <section className="relative overflow-hidden rounded-2xl bg-gradient-to-r from-indigo-950 via-indigo-900 to-slate-900 p-5 text-white shadow-lg sm:p-7">
          <div className="pointer-events-none absolute -right-12 -top-16 h-56 w-56 rounded-full bg-indigo-400/20 blur-3xl" />
          <div className="relative z-10 flex flex-wrap items-end justify-between gap-4">
            <div className="max-w-2xl">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-indigo-200">Cổng thông tin thực tập</p>
              <h2 className="mt-2 text-2xl font-bold">Xin chào, {profile?.fullName || 'Sinh viên'}!</h2>
              <p className="mt-1 text-sm leading-6 text-indigo-100">
                Theo dõi thông tin hồ sơ và các hoạt động thực tập trong cùng một không gian.
              </p>
            </div>
            {profile?.programName && (
              <div className="max-w-full rounded-xl border border-white/15 bg-white/10 px-3.5 py-2.5 backdrop-blur-sm">
                <p className="text-[10px] font-bold uppercase tracking-wide text-indigo-200">Chương trình</p>
                <p className="mt-0.5 truncate text-sm font-semibold text-white">{profile.programName}</p>
              </div>
            )}
          </div>
        </section>

        <WorkspaceTabs
          label="Các mục trong không gian sinh viên"
          tabs={TABS}
          activeTab={activeTab}
          onChange={setActiveTab}
        />

        {activeTab === 'overview' && (
          <section className="space-y-5" aria-label="Tổng quan hồ sơ sinh viên">
            {isLoading ? (
              <div role="status" className="rounded-2xl border border-slate-200 bg-white p-8 text-center text-sm text-slate-500 shadow-sm">
                <RefreshCw className="mx-auto mb-2 h-5 w-5 animate-spin text-indigo-600" />
                Đang tải hồ sơ...
              </div>
            ) : profileError ? (
              <div role="alert" className="rounded-2xl border border-rose-200 bg-rose-50 p-5 text-sm text-rose-800">
                <div className="flex items-start gap-3">
                  <AlertCircle className="mt-0.5 h-5 w-5 shrink-0" />
                  <div className="min-w-0 flex-1">
                    <h3 className="font-bold">Không thể tải hồ sơ</h3>
                    <p className="mt-1 break-words">{profileError}</p>
                    <button type="button" onClick={refreshStudentData} className="mt-3 inline-flex items-center gap-2 rounded-lg border border-rose-300 px-3 py-2 text-xs font-semibold hover:bg-rose-100">
                      <RefreshCw className="h-3.5 w-3.5" /> Thử lại
                    </button>
                  </div>
                </div>
              </div>
            ) : profile ? (
              <div className="grid gap-5 xl:grid-cols-[1.45fr_0.85fr]">
                <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
                  <div className="flex flex-wrap items-start justify-between gap-3 border-b border-slate-100 pb-4">
                    <div className="flex min-w-0 items-center gap-3">
                      <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-indigo-50 text-indigo-700">
                        <GraduationCap className="h-5 w-5" />
                      </span>
                      <div className="min-w-0">
                        <h3 className="truncate text-base font-bold text-slate-900">{profile.fullName}</h3>
                        <p className="mt-1 text-xs text-slate-500">Mã sinh viên <span className="font-mono font-bold text-indigo-700">{profile.studentCode || 'Chưa cập nhật'}</span></p>
                      </div>
                    </div>
                    {accountStatus && (
                      <Badge variant={accountStatus} className="shrink-0" dot>
                        {ACCOUNT_STATUS_LABELS[accountStatus] || accountStatus}
                      </Badge>
                    )}
                  </div>

                  <div className="mt-4 grid gap-3 sm:grid-cols-2">
                    <InfoCard icon={Mail} label="Email tài khoản" value={profile.email} />
                    <InfoCard icon={Phone} label="Điện thoại" value={profile.phoneNumber} />
                    <InfoCard icon={School} label="Trường đại học" value={profile.university} />
                    <InfoCard icon={BookOpen} label="Chuyên ngành" value={profile.major} />
                    <InfoCard icon={Building2} label="Chương trình thực tập" value={profile.programName} />
                  </div>
                </section>

                <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
                  <div className="flex items-center gap-3 border-b border-slate-100 pb-4">
                    <span className="rounded-xl bg-violet-50 p-2.5 text-violet-700"><UserRound className="h-5 w-5" /></span>
                    <div>
                      <h3 className="text-sm font-bold text-slate-900">Mentor hướng dẫn</h3>
                      <p className="mt-0.5 text-xs text-slate-500">Thông tin được liên kết với hồ sơ hiện tại</p>
                    </div>
                  </div>
                  {profile.assignedMentor ? (
                    <div className="mt-4 space-y-3">
                      <div>
                        <p className="text-base font-bold text-slate-900">{profile.assignedMentor.fullName}</p>
                        <p className="mt-1 text-sm text-slate-600">{profile.assignedMentor.department || profile.assignedMentor.specialization || 'Chưa cập nhật bộ phận'}</p>
                      </div>
                      {profile.assignedMentor.phoneNumber && <InfoCard icon={Phone} label="Điện thoại mentor" value={profile.assignedMentor.phoneNumber} />}
                    </div>
                  ) : (
                    <div role="status" className="mt-4 rounded-xl border border-dashed border-slate-300 px-4 py-7 text-center">
                      <UserRound className="mx-auto h-7 w-7 text-slate-300" />
                      <p className="mt-2 text-sm font-semibold text-slate-700">Chưa có mentor được phân công</p>
                      <p className="mt-1 text-xs leading-5 text-slate-500">Thông tin sẽ hiển thị sau khi HR liên kết mentor với hồ sơ của bạn.</p>
                    </div>
                  )}
                </section>
              </div>
            ) : (
              <div role="status" className="rounded-2xl border border-dashed border-slate-300 bg-white p-8 text-center">
                <GraduationCap className="mx-auto h-8 w-8 text-slate-300" />
                <h3 className="mt-3 text-sm font-bold text-slate-800">Tài khoản chưa có hồ sơ sinh viên</h3>
                <p className="mt-1 text-sm text-slate-500">Vui lòng liên hệ HR để kiểm tra việc liên kết tài khoản với hồ sơ.</p>
              </div>
            )}

            <div className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-indigo-100 bg-indigo-50/70 p-4">
              <div className="flex items-start gap-3">
                <CalendarDays className="mt-0.5 h-4 w-4 shrink-0 text-indigo-700" />
                <p className="text-xs leading-5 text-indigo-900">Xem các mốc chương trình và hạn công việc đã được giao trong lịch cá nhân.</p>
              </div>
              <Link to="/student/schedule" className="shrink-0 rounded-lg bg-white px-3 py-2 text-xs font-semibold text-indigo-700 shadow-sm hover:bg-indigo-100">
                Mở lịch thực tập
              </Link>
            </div>
          </section>
        )}

        {activeTab === 'documents' && <StudentDocumentsPanel />}
        {activeTab === 'schedule' && <StudentScheduleView embedded />}
        {activeTab === 'tasks' && <StudentTasksPanel />}
        {activeTab === 'reports' && <StudentWeeklyReportsPanel />}
        {activeTab === 'evaluation' && <StudentEvaluationPanel />}
      </div>
    </DashboardLayout>
  );
};

export default StudentDashboardView;
