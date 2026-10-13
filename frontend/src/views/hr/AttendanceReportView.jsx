import React, { useCallback, useEffect, useState } from 'react';
import { AlertTriangle, Loader2, RefreshCw } from 'lucide-react';
import DashboardLayout from '../../components/layout/DashboardLayout';
import hrWorkforceService from '../../services/hrWorkforceService';

const vnDate = (value) => value ? new Date(String(value).slice(0, 10) + 'T00:00:00').toLocaleDateString('vi-VN') : '—';
const vnDateTime = (value) => value ? new Date(value).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' }) : '—';

export const AttendanceReportView = () => {
  const localToday = new Date().toLocaleDateString('en-CA', { timeZone: 'Asia/Ho_Chi_Minh' });
  const [filters, setFilters] = useState({ fromDate: localToday.slice(0, 7) + '-01', toDate: localToday, studentId: '' });
  const [students, setStudents] = useState(null);
  const [studentsLoading, setStudentsLoading] = useState(true);
  const [studentsError, setStudentsError] = useState('');
  const [report, setReport] = useState(null);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const loadStudents = useCallback(async () => {
    setStudentsLoading(true);
    setStudents(null);
    setStudentsError('');
    try {
      const data = await hrWorkforceService.getStudents();
      const rows = Array.isArray(data) ? data : Array.isArray(data?.items) ? data.items : null;
      if (!rows) throw new Error('Dữ liệu sinh viên không đúng định dạng.');
      setStudents(rows);
    } catch (loadError) {
      setStudentsError(loadError.message || 'Không thể tải danh sách sinh viên.');
    } finally {
      setStudentsLoading(false);
    }
  }, []);

  const load = useCallback(async (nextPage = 1) => {
    setLoading(true);
    setReport(null);
    setError('');
    try {
      if (filters.fromDate && filters.toDate && filters.fromDate > filters.toDate) throw new Error('Ngày bắt đầu không được sau ngày kết thúc.');
      const data = await hrWorkforceService.getAttendanceReport({ ...filters, studentId: filters.studentId || undefined, page: nextPage, pageSize: 20 });
      if (!data || !Array.isArray(data.items) || !Number.isFinite(data.totalItems)) throw new Error('Dữ liệu báo cáo chấm công không đúng định dạng.');
      setReport(data);
      setPage(nextPage);
    } catch (loadError) {
      setError(loadError.message || 'Không thể tải báo cáo chấm công.');
    } finally {
      setLoading(false);
    }
  }, [filters]);

  useEffect(() => { void loadStudents(); }, [loadStudents]);
  useEffect(() => { void load(1); }, [load]);

  return (
    <DashboardLayout title="Báo cáo chấm công" subtitle="Lọc phiên chấm công theo sinh viên và ngày nghiệp vụ tại Việt Nam.">
      <div className="space-y-5">
        {(error || studentsError) && <div role="alert" className="flex flex-col gap-2 rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800">{error && <div className="flex items-center justify-between gap-3"><span>{error}</span><button type="button" onClick={() => void load(page)} disabled={loading} className="inline-flex shrink-0 items-center gap-1 font-semibold disabled:opacity-50"><RefreshCw className="h-4 w-4" /> Thử tải báo cáo</button></div>}{studentsError && <div className="flex items-center justify-between gap-3"><span>{studentsError}</span><button type="button" onClick={() => void loadStudents()} disabled={studentsLoading} className="inline-flex shrink-0 items-center gap-1 font-semibold disabled:opacity-50"><RefreshCw className="h-4 w-4" /> Thử tải sinh viên</button></div>}</div>}
        <section className="grid gap-3 rounded-2xl border border-slate-200 bg-white p-4 shadow-sm sm:grid-cols-2 xl:grid-cols-4">
          <label className="text-xs font-semibold text-slate-600">Từ ngày<input type="date" max={filters.toDate || undefined} value={filters.fromDate} onChange={(event) => setFilters({ ...filters, fromDate: event.target.value })} className="mt-1 block w-full rounded-lg border border-slate-300 px-3 py-2" /></label>
          <label className="text-xs font-semibold text-slate-600">Đến ngày<input type="date" min={filters.fromDate || undefined} value={filters.toDate} onChange={(event) => setFilters({ ...filters, toDate: event.target.value })} className="mt-1 block w-full rounded-lg border border-slate-300 px-3 py-2" /></label>
          <label className="text-xs font-semibold text-slate-600">Thực tập sinh<select disabled={studentsLoading || !students} value={filters.studentId} onChange={(event) => setFilters({ ...filters, studentId: event.target.value })} className="mt-1 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2"><option value="">{studentsLoading ? 'Đang tải sinh viên...' : 'Tất cả sinh viên'}</option>{(students || []).map((student) => <option key={student.id} value={student.id}>{student.fullName} · {student.studentCode}</option>)}</select></label>
          <button type="button" onClick={() => void load(1)} disabled={loading || studentsLoading || !students} className="self-end rounded-lg bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-50">Áp dụng bộ lọc</button>
        </section>
        {loading ? <div role="status" className="flex items-center justify-center gap-2 rounded-xl border border-slate-200 bg-white p-6 text-sm text-slate-500"><Loader2 className="h-4 w-4 animate-spin" /> Đang tải báo cáo...</div> : error ? null : report && <>
          <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-5">
            <Summary label="Phiên chưa phân loại" value={report.unclassifiedSessions ?? '—'} tone="amber" />
            <Summary label="Ngày công đã phân loại" value={report.attendanceClassificationConfigured ? report.presentDays : '—'} tone="emerald" />
            <Summary label="Đi muộn đã phân loại" value={report.attendanceClassificationConfigured ? report.lateDays : '—'} tone="rose" />
            <Summary label="Vắng mặt đã phân loại" value={report.attendanceClassificationConfigured ? report.absentDays : '—'} tone="slate" />
            <Summary label="Ngày nghỉ được duyệt*" value={report.approvedLeaveCalendarDays ?? '—'} tone="sky" />
          </div>
          {!report.attendanceClassificationConfigured && <div className="flex gap-3 rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-900"><AlertTriangle className="h-5 w-5 shrink-0" /><p><strong>Chưa cấu hình quy tắc phân loại chuyên cần.</strong> Các phiên chấm công được giữ ở trạng thái chưa phân loại; hệ thống không tự suy ra ngày công, đi muộn hoặc vắng mặt. *Ngày nghỉ là số ngày lịch trong đơn đã duyệt; chưa trừ ngày lễ hoặc lịch làm việc.</p></div>}
          <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm"><div className="border-b border-slate-100 p-5"><h2 className="text-sm font-bold text-slate-900">Chi tiết phiên chấm công</h2></div>
            {report.items.length ? <div className="overflow-x-auto"><table className="w-full min-w-[850px] text-left text-xs"><thead className="bg-slate-50 text-[10px] uppercase text-slate-500"><tr><th className="p-3">Thực tập sinh</th><th className="p-3">Ngày</th><th className="p-3">Giờ vào (giờ Việt Nam)</th><th className="p-3">Giờ ra (giờ Việt Nam)</th><th className="p-3">Thời lượng</th><th className="p-3">Trạng thái phiên</th></tr></thead><tbody className="divide-y divide-slate-100">{report.items.map((row) => <tr key={row.id}><td className="p-3"><p className="font-semibold text-slate-800">{row.studentName}</p><p className="text-slate-500">{row.studentCode}</p></td><td className="p-3">{vnDate(row.date)}</td><td className="p-3">{vnDateTime(row.checkInTimeUtc)}</td><td className="p-3">{vnDateTime(row.checkOutTimeUtc)}</td><td className="p-3">{row.durationMinutes == null ? '—' : row.durationMinutes + ' phút'}</td><td className="p-3"><span className="rounded-full bg-amber-50 px-2 py-1 font-semibold text-amber-800">{row.status === 'CHECKED_IN' ? 'Đang làm việc · chưa phân loại' : row.status === 'COMPLETED' ? 'Đã hoàn tất · chưa phân loại' : 'Chưa phân loại'}</span></td></tr>)}</tbody></table></div> : <p role="status" className="p-8 text-center text-sm text-slate-500">Không có phiên chấm công trong khoảng lọc.</p>}
            <div className="flex items-center justify-between border-t border-slate-100 px-5 py-3 text-xs text-slate-500"><span>{report.totalItems} phiên</span><div className="flex items-center gap-2"><button type="button" disabled={loading || page <= 1} onClick={() => void load(page - 1)} className="rounded border px-3 py-1.5 disabled:opacity-40">Trước</button><span>Trang {page} / {report.totalPages || 1}</span><button type="button" disabled={loading || page >= report.totalPages} onClick={() => void load(page + 1)} className="rounded border px-3 py-1.5 disabled:opacity-40">Sau</button></div></div>
          </section>
        </>}
      </div>
    </DashboardLayout>
  );
};

const tones = { amber: 'bg-amber-50 text-amber-800', emerald: 'bg-emerald-50 text-emerald-800', rose: 'bg-rose-50 text-rose-800', slate: 'bg-slate-100 text-slate-700', sky: 'bg-sky-50 text-sky-800' };
const Summary = ({ label, value, tone }) => <article className={'rounded-xl border border-slate-200 p-4 ' + tones[tone]}><p className="text-xs font-semibold">{label}</p><p className="mt-2 text-2xl font-extrabold">{value}</p></article>;
export default AttendanceReportView;
