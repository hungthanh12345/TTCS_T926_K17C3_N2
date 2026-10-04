import React, { useEffect, useMemo, useState } from 'react';
import { Award, BarChart3, Loader2, Search, Users } from 'lucide-react';
import DashboardLayout from '../../components/layout/DashboardLayout';
import hrInternshipSummaryService from '../../services/hrInternshipSummaryService';

const formatWeek = (value) => value ? new Date(`${value}T00:00:00`).toLocaleDateString('vi-VN') : '—';

export const HrInternshipSummaryView = () => {
  const [summary, setSummary] = useState(null);
  const [search, setSearch] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    let isActive = true;
    hrInternshipSummaryService.getSummary()
      .then((data) => { if (isActive) setSummary(data); })
      .catch((loadError) => { if (isActive) setError(loadError.message || 'Không thể tải báo cáo tổng hợp.'); })
      .finally(() => { if (isActive) setIsLoading(false); });
    return () => { isActive = false; };
  }, []);

  const rows = useMemo(() => {
    const query = search.trim().toLocaleLowerCase('vi');
    if (!query) return summary?.items || [];
    return (summary?.items || []).filter((item) => [
      item.studentName, item.studentCode, item.university, item.major, item.mentorName, item.mentorDepartment,
    ].filter(Boolean).some((value) => value.toLocaleLowerCase('vi').includes(query)));
  }, [search, summary]);

  return (
    <DashboardLayout title="Tổng hợp kết quả thực tập" subtitle="Theo dõi đánh giá cuối kỳ, báo cáo tuần và phản hồi Mentor.">
      <div className="space-y-6">
        {isLoading ? <div role="status" className="flex items-center gap-2 rounded-2xl border border-slate-200 bg-white p-6 text-sm text-slate-500"><Loader2 className="h-5 w-5 animate-spin" /> Đang tổng hợp dữ liệu...</div> : error ? (
          <div role="alert" className="rounded-2xl border border-rose-200 bg-rose-50 p-5 text-sm text-rose-800">{error}</div>
        ) : (
          <>
            <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
              <SummaryCard icon={Users} label="Thực tập sinh" value={summary?.totalStudents ?? 0} color="indigo" />
              <SummaryCard icon={Award} label="Đã đánh giá" value={summary?.evaluatedStudents ?? 0} color="emerald" />
              <SummaryCard icon={BarChart3} label="Chờ đánh giá" value={summary?.pendingEvaluations ?? 0} color="amber" />
              <SummaryCard icon={Award} label="Điểm trung bình" value={summary?.averageOverallScore == null ? '—' : `${summary.averageOverallScore.toFixed(1)}/10`} color="sky" />
            </div>

            <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
              <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 p-5">
                <div><h2 className="text-sm font-bold text-slate-900">Kết quả theo thực tập sinh</h2><p className="mt-1 text-xs text-slate-500">Dữ liệu tổng hợp trực tiếp từ hồ sơ, báo cáo tuần và đánh giá hiện có.</p></div>
                <label className="relative block w-full sm:max-w-xs">
                  <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                  <input type="search" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Tìm sinh viên, trường, Mentor..." className="w-full rounded-lg border border-slate-300 py-2.5 pl-9 pr-3 text-xs outline-none focus:border-indigo-500 focus:ring-4 focus:ring-indigo-100" />
                </label>
              </div>
              {rows.length === 0 ? <p role="status" className="p-8 text-center text-sm text-slate-500">{summary?.items?.length ? 'Không tìm thấy kết quả phù hợp.' : 'Chưa có hồ sơ thực tập sinh để tổng hợp.'}</p> : (
                <div className="overflow-x-auto">
                  <table className="min-w-[1050px] w-full text-left text-xs">
                    <thead className="bg-slate-50 text-[10px] uppercase tracking-wide text-slate-500">
                      <tr><th className="px-4 py-3">Thực tập sinh</th><th className="px-4 py-3">Mentor</th><th className="px-4 py-3">Báo cáo tuần</th><th className="px-4 py-3">Feedback</th><th className="px-4 py-3">Kỹ năng</th><th className="px-4 py-3">Thái độ</th><th className="px-4 py-3">Tổng điểm</th><th className="px-4 py-3">Ngày đánh giá</th><th className="px-4 py-3">Trạng thái</th></tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {rows.map((item) => (
                        <tr key={item.studentId} className="align-top hover:bg-slate-50/70">
                          <td className="px-4 py-4"><p className="font-bold text-slate-900">{item.studentName}</p><p className="mt-1 text-slate-500">{item.studentCode} · {item.major}</p><p className="mt-1 text-slate-400">{item.university}</p></td>
                          <td className="px-4 py-4 text-slate-700">{item.mentorName || 'Chưa phân công'}<p className="mt-1 text-slate-400">{item.mentorDepartment || ''}</p></td>
                          <td className="px-4 py-4 text-slate-700">{item.weeklyReportCount}<p className="mt-1 text-slate-400">Tuần mới nhất: {formatWeek(item.latestReportWeek)}</p></td>
                          <td className="px-4 py-4 text-slate-700">{item.mentorFeedbackCount}</td>
                          <td className="px-4 py-4 text-slate-700">{item.skillsScore ?? '—'}</td>
                          <td className="px-4 py-4 text-slate-700">{item.attitudeScore ?? '—'}</td>
                          <td className="px-4 py-4 font-bold text-slate-900">{item.overallScore == null ? '—' : `${item.overallScore}/10`}{item.evaluatedBy && <p className="mt-1 font-normal text-slate-400">{item.evaluatedBy}</p>}</td>
                          <td className="px-4 py-4 text-slate-600">{item.evaluatedAt ? new Date(item.evaluatedAt).toLocaleDateString('vi-VN') : '—'}</td>
                          <td className="px-4 py-4"><span className={`rounded-full px-2 py-1 text-[10px] font-bold ${item.evaluationStatus === 'EVALUATED' ? 'bg-emerald-50 text-emerald-700' : 'bg-amber-50 text-amber-700'}`}>{item.evaluationStatus === 'EVALUATED' ? 'Đã đánh giá' : 'Chờ đánh giá'}</span>{item.evaluationComments && <p className="mt-2 max-w-56 whitespace-pre-wrap text-slate-500">{item.evaluationComments}</p>}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
              <div className="border-t border-slate-100 px-5 py-3 text-[11px] text-slate-500">Hiển thị {rows.length} / {summary?.totalStudents ?? 0} hồ sơ</div>
            </section>
          </>
        )}
      </div>
    </DashboardLayout>
  );
};

const colorClasses = {
  indigo: 'bg-indigo-50 text-indigo-700',
  emerald: 'bg-emerald-50 text-emerald-700',
  amber: 'bg-amber-50 text-amber-700',
  sky: 'bg-sky-50 text-sky-700',
};

const SummaryCard = ({ icon: Icon, label, value, color }) => (
  <article className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
    <div className="flex items-center justify-between"><p className="text-xs font-semibold text-slate-500">{label}</p><span className={`rounded-xl p-2 ${colorClasses[color]}`}><Icon className="h-4 w-4" /></span></div>
    <p className="mt-3 text-2xl font-extrabold text-slate-900">{value}</p>
  </article>
);

export default HrInternshipSummaryView;
