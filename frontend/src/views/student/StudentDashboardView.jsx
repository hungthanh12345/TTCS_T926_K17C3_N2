import React, { useState, useEffect } from 'react';
import DashboardLayout from '../../components/layout/DashboardLayout';
import { useAuth } from '../../context/AuthContext';
import studentService from '../../services/studentService';
import {
  GraduationCap,
  Building,
  Phone,
  School,
  BookOpen,
  Calendar,
  CheckCircle2,
  Clock,
  ShieldCheck,
} from 'lucide-react';
import TableSkeleton from '../../components/common/TableSkeleton';

export const StudentDashboardView = () => {
  const { user } = useAuth();
  const [profile, setProfile] = useState(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    loadStudentData();
  }, []);

  const loadStudentData = async () => {
    setIsLoading(true);
    try {
      const allStudents = await studentService.getStudents();
      const matched =
        allStudents.find((s) => s.email?.toLowerCase() === user?.email?.toLowerCase()) ||
        allStudents[0];
      setProfile(matched);
    } catch (err) {
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <DashboardLayout
      title="Cổng Thông Tin Thực Tập Sinh"
      subtitle="Theo dõi tiến độ tham gia thực tập, thông tin Mentor phụ trách và lịch trình đào tạo"
    >
      <div className="space-y-6">
        {/* Banner Chào Mừng Sinh Viên */}
        <div className="rounded-3xl bg-gradient-to-r from-indigo-900 via-slate-900 to-slate-950 p-6 sm:p-8 text-white relative overflow-hidden shadow-xl border border-indigo-500/20">
          <div className="absolute top-0 right-0 -mt-10 -mr-10 w-64 h-64 bg-indigo-500/20 rounded-full blur-3xl pointer-events-none" />
          <div className="relative z-10 max-w-2xl">
            <span className="inline-block px-3 py-1 rounded-full text-xs font-semibold bg-emerald-500/20 text-emerald-300 border border-emerald-500/30 mb-3 backdrop-blur-xs">
              Ứng Viên Thực Tập • Đang Hoạt Động
            </span>
            <h2 className="text-2xl sm:text-3xl font-extrabold tracking-tight">
              Xin chào, {profile?.fullName || user?.fullName || 'Sinh viên'}!
            </h2>
            <p className="text-slate-300 text-sm mt-2 leading-relaxed">
              Chào mừng bạn tham gia chương trình đào tạo Thực tập sinh. Dưới đây là thông tin chi tiết về hồ sơ học tập và Mentor chuyên môn được phân công đồng hành cùng bạn.
            </p>
          </div>
        </div>

        {isLoading ? (
          <TableSkeleton rows={2} cols={3} />
        ) : (
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* Hồ Sơ Sinh Viên (2 cols) */}
            <div className="lg:col-span-2 bg-white rounded-2xl border border-slate-200/80 p-6 shadow-xs space-y-6">
              <div className="flex items-center justify-between border-b border-slate-100 pb-4">
                <div className="flex items-center gap-3">
                  <div className="w-12 h-12 rounded-2xl bg-indigo-50 border border-indigo-100 text-indigo-600 font-bold text-lg flex items-center justify-center shadow-xs">
                    <GraduationCap className="w-6 h-6" />
                  </div>
                  <div>
                    <h3 className="text-lg font-bold text-slate-900">
                      {profile?.fullName}
                    </h3>
                    <p className="text-xs text-slate-500">
                      Mã Sinh Viên: <span className="font-mono font-bold text-indigo-600">{profile?.studentCode}</span>
                    </p>
                  </div>
                </div>

                <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-emerald-50 text-emerald-700 border border-emerald-200">
                  <CheckCircle2 className="w-3.5 h-3.5" />
                  Hồ Sơ Hợp Lệ
                </span>
              </div>

              {/* Lưới Thông Tin */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 text-xs">
                <div className="p-3.5 rounded-2xl bg-slate-50 border border-slate-100">
                  <span className="text-slate-400 font-bold uppercase tracking-wider block mb-1">
                    Trường Đại Học
                  </span>
                  <div className="flex items-start gap-1.5 text-slate-800 font-medium leading-relaxed">
                    <School className="w-4 h-4 text-slate-400 shrink-0 mt-0.5" />
                    <span>{profile?.university}</span>
                  </div>
                </div>

                <div className="p-3.5 rounded-2xl bg-slate-50 border border-slate-100">
                  <span className="text-slate-400 font-bold uppercase tracking-wider block mb-1">
                    Chuyên Ngành Đào Tạo
                  </span>
                  <div className="flex items-center gap-1.5 text-slate-800 font-semibold">
                    <BookOpen className="w-4 h-4 text-indigo-500 shrink-0" />
                    <span>{profile?.major}</span>
                  </div>
                </div>

                <div className="p-3.5 rounded-2xl bg-slate-50 border border-slate-100">
                  <span className="text-slate-400 font-bold uppercase tracking-wider block mb-1">
                    Điện Thoại Liên Hệ
                  </span>
                  <div className="flex items-center gap-1.5 text-slate-800 font-medium">
                    <Phone className="w-4 h-4 text-slate-400 shrink-0" />
                    <span>{profile?.phone}</span>
                  </div>
                </div>

                <div className="p-3.5 rounded-2xl bg-slate-50 border border-slate-100">
                  <span className="text-slate-400 font-bold uppercase tracking-wider block mb-1">
                    Kỳ Thực Tập Ghi Nhận
                  </span>
                  <div className="flex items-center gap-1.5 text-slate-800 font-medium">
                    <Calendar className="w-4 h-4 text-slate-400 shrink-0" />
                    <span>{profile?.internshipPeriod || 'Kỳ Thu 2026 (09/2026 - 12/2026)'}</span>
                  </div>
                </div>
              </div>

              {/* Thông báo Onboarding */}
              <div className="p-4 rounded-2xl bg-indigo-50/70 border border-indigo-100 flex items-start gap-3">
                <ShieldCheck className="w-5 h-5 text-indigo-600 shrink-0 mt-0.5" />
                <div>
                  <h4 className="text-xs font-bold text-indigo-950 uppercase tracking-wide">
                    Tiến Độ Onboarding Sprint 1 Hoàn Tất
                  </h4>
                  <p className="text-xs text-indigo-800/90 mt-0.5 leading-relaxed">
                    Hồ sơ của bạn đã được Phòng Nhân sự (HR) đồng bộ và ghép nối Mentor trực tiếp. Vui lòng kết nối với Mentor để nhận lịch họp bàn giao đồ án.
                  </p>
                </div>
              </div>
            </div>

            {/* Thẻ Mentor Phụ Trách (1 col) */}
            <div className="bg-white rounded-2xl border border-slate-200/80 p-6 shadow-xs flex flex-col justify-between">
              <div>
                <div className="flex items-center justify-between border-b border-slate-100 pb-3 mb-4">
                  <h3 className="text-sm font-bold text-slate-900 uppercase tracking-wider">
                    Mentor Hướng Dẫn
                  </h3>
                  <span className="text-[11px] font-bold text-purple-700 bg-purple-50 px-2 py-0.5 rounded-full border border-purple-200">
                    Chuyên Gia Phụ Trách
                  </span>
                </div>

                {profile?.assignedMentor ? (
                  <div className="space-y-4">
                    <div className="flex items-center gap-3">
                      <div className="w-12 h-12 rounded-2xl bg-gradient-to-tr from-purple-600 to-indigo-600 text-white font-bold text-lg flex items-center justify-center shadow-xs">
                        {profile.assignedMentor.fullName.charAt(0)}
                      </div>
                      <div>
                        <h4 className="text-base font-bold text-slate-900">
                          {profile.assignedMentor.fullName}
                        </h4>
                        <span className="text-xs text-purple-700 font-semibold flex items-center gap-1 mt-0.5">
                          <Building className="w-3.5 h-3.5 text-purple-500" />
                          {profile.assignedMentor.department}
                        </span>
                      </div>
                    </div>

                    <div className="p-3.5 rounded-2xl bg-slate-50 border border-slate-100 text-xs text-slate-600 space-y-1">
                      <span className="text-[10px] font-bold uppercase text-slate-400 block">
                        Trách Nhiệm Của Mentor
                      </span>
                      <p className="leading-relaxed">
                        Hướng dẫn đồ án thực tế, kiểm duyệt mã nguồn, chấm điểm chuyên cần và đánh giá kết quả thực tập cuối kỳ.
                      </p>
                    </div>
                  </div>
                ) : (
                  <div className="py-8 text-center">
                    <div className="w-10 h-10 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center mx-auto mb-2">
                      <Clock className="w-5 h-5" />
                    </div>
                    <h4 className="text-xs font-bold text-slate-800">
                      Đang Trong Quy Trình Phân Công
                    </h4>
                    <p className="text-[11px] text-slate-500 mt-1">
                      Phòng Nhân sự đang xem xét chuyên ngành của bạn để ghép nối với Mentor phù hợp nhất.
                    </p>
                  </div>
                )}
              </div>

              <div className="mt-6 pt-4 border-t border-slate-100 text-center">
                <span className="text-[11px] text-slate-400">
                  Cần hỗ trợ? Hãy liên hệ Chuyên viên HR qua Cổng thông tin Quản lý Thực tập sinh.
                </span>
              </div>
            </div>
          </div>
        )}
      </div>
    </DashboardLayout>
  );
};

export default StudentDashboardView;
