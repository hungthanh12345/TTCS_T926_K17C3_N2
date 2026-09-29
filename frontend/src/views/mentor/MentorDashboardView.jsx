import React, { useState, useEffect } from 'react';
import DashboardLayout from '../../components/layout/DashboardLayout';
import { useAuth } from '../../context/AuthContext';
import studentService from '../../services/studentService';
import {
  Users,
  GraduationCap,
  Calendar,
  Phone,
  Mail,
  School,
  BookOpen,
  Award,
  CheckCircle2,
  Clock,
  Sparkles,
} from 'lucide-react';
import TableSkeleton from '../../components/common/TableSkeleton';

export const MentorDashboardView = () => {
  const { user } = useAuth();
  const [students, setStudents] = useState([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    loadMentees();
  }, []);

  const loadMentees = async () => {
    setIsLoading(true);
    try {
      const allStudents = await studentService.getStudents();
      const mentees = allStudents.filter(
        (s) => s.mentorId || s.assignedMentor?.fullName?.includes('Alex')
      );
      setStudents(mentees.length > 0 ? mentees : allStudents.slice(0, 3));
    } catch (err) {
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <DashboardLayout
      title="Cổng Thông Tin Hướng Dẫn Mentor"
      subtitle="Theo dõi ứng viên thực tập được phân công, hướng dẫn kỹ thuật và đánh giá tiến độ"
    >
      <div className="space-y-6">
        {/* Banner Chào Mừng Mentor */}
        <div className="rounded-3xl bg-gradient-to-r from-purple-900 via-indigo-900 to-slate-950 p-6 sm:p-8 text-white relative overflow-hidden shadow-xl border border-purple-500/20">
          <div className="absolute top-0 right-0 -mt-10 -mr-10 w-64 h-64 bg-purple-500/20 rounded-full blur-3xl pointer-events-none" />
          <div className="relative z-10 max-w-2xl">
            <span className="inline-block px-3 py-1 rounded-full text-xs font-semibold bg-white/10 text-purple-200 border border-white/15 mb-3 backdrop-blur-xs">
              Chuyên Gia Kỹ Thuật Hướng Dẫn
            </span>
            <h2 className="text-2xl sm:text-3xl font-extrabold tracking-tight">
              Xin chào, {user?.fullName || 'Mentor'}!
            </h2>
            <p className="text-slate-300 text-sm mt-2 leading-relaxed">
              Bạn đang phụ trách định hướng chuyên môn cho sinh viên thực tập thuộc Kỳ Thu 2026. Hãy duy trì trao đổi định kỳ, hỗ trợ review code và theo sát tiến độ các bài toán thực tế.
            </p>
          </div>
        </div>

        {/* Danh Sách Mentees */}
        <div>
          <div className="flex items-center justify-between mb-4">
            <div>
              <h3 className="text-base font-bold text-slate-900">
                Sinh Viên Đang Trực Tiếp Hướng Dẫn ({students.length})
              </h3>
              <p className="text-xs text-slate-500">
                Hồ sơ sinh viên do Phòng Nhân sự (HR) ghép nối với chuyên môn của bạn
              </p>
            </div>
          </div>

          {isLoading ? (
            <TableSkeleton rows={3} cols={4} />
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
              {students.map((student) => (
                <div
                  key={student.id}
                  className="bg-white rounded-2xl border border-slate-200/80 shadow-xs hover:shadow-md transition-all p-5 flex flex-col justify-between"
                >
                  <div>
                    <div className="flex items-start justify-between">
                      <div className="w-10 h-10 rounded-xl bg-indigo-50 text-indigo-700 font-bold text-sm flex items-center justify-center border border-indigo-100">
                        {student.fullName.charAt(0)}
                      </div>
                      <span className="font-mono text-xs font-bold px-2 py-0.5 rounded-md bg-indigo-50 text-indigo-700 border border-indigo-100">
                        {student.studentCode}
                      </span>
                    </div>

                    <h4 className="text-base font-bold text-slate-900 mt-3">
                      {student.fullName}
                    </h4>

                    <div className="mt-3 space-y-1.5 text-xs text-slate-600">
                      <div className="flex items-center gap-2">
                        <School className="w-3.5 h-3.5 text-slate-400 shrink-0" />
                        <span className="truncate">{student.university}</span>
                      </div>
                      <div className="flex items-center gap-2">
                        <BookOpen className="w-3.5 h-3.5 text-indigo-500 shrink-0" />
                        <span className="font-semibold text-slate-800">{student.major}</span>
                      </div>
                      <div className="flex items-center gap-2">
                        <Phone className="w-3.5 h-3.5 text-slate-400 shrink-0" />
                        <span>{student.phone}</span>
                      </div>
                      {student.email && (
                        <div className="flex items-center gap-2">
                          <Mail className="w-3.5 h-3.5 text-slate-400 shrink-0" />
                          <span className="truncate">{student.email}</span>
                        </div>
                      )}
                    </div>
                  </div>

                  <div className="mt-5 pt-3.5 border-t border-slate-100 flex items-center justify-between text-xs">
                    <span className="inline-flex items-center gap-1 text-emerald-600 font-bold">
                      <CheckCircle2 className="w-3.5 h-3.5" />
                      Tiến Độ Tốt
                    </span>

                    <span className="text-slate-500 text-[11px] font-medium">
                      {student.internshipPeriod || 'Kỳ Thu 2026'}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </DashboardLayout>
  );
};

export default MentorDashboardView;
