import React, { useCallback, useEffect, useState } from 'react';
import { Clock3, LogIn, LogOut, RefreshCw } from 'lucide-react';
import toast from 'react-hot-toast';
import DashboardLayout from '../../components/layout/DashboardLayout';
import studentWorkforceService from '../../services/studentWorkforceService';

const formatDate = (value) => value
  ? new Date(String(value).slice(0, 10) + 'T00:00:00').toLocaleDateString('vi-VN')
  : '—';

const formatDateTime = (value) => value
  ? new Date(value).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' })
  : '—';

const attendanceStatusLabel = (status) => {
  if (status === 'CHECKED_IN') return 'Đang làm việc';
  if (status === 'COMPLETED') return 'Đã hoàn tất';
  return 'Chưa xác định';
};

export const StudentAttendanceView = () => {
  const today = new Date().toLocaleDateString('en-CA', { timeZone: 'Asia/Ho_Chi_Minh' });
  const [attendance, setAttendance] = useState(null);
  const [todayLoaded, setTodayLoaded] = useState(false);
  const [todayLoading, setTodayLoading] = useState(true);
  const [todayError, setTodayError] = useState('');
  const [history, setHistory] = useState(null);
  const [historyLoading, setHistoryLoading] = useState(true);
  const [historyError, setHistoryError] = useState('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [notes, setNotes] = useState('');
  const [sending, setSending] = useState(false);

  const loadToday = useCallback(async () => {
    setTodayLoading(true);
    setTodayLoaded(false);
    setTodayError('');
    setAttendance(null);
    try {
      const current = await studentWorkforceService.getTodayAttendance();
      setAttendance(current ?? null);
      setTodayLoaded(true);
    } catch (loadError) {
      setTodayError(loadError.message || 'Không thể tải trạng thái chấm công hôm nay.');
    } finally {
      setTodayLoading(false);
    }
  }, []);

  const loadHistory = useCallback(async (page = 1) => {
    setHistoryLoading(true);
    setHistory(null);
    setHistoryError('');
    try {
      if (fromDate && toDate && fromDate > toDate) {
        throw new Error('Ngày bắt đầu không được sau ngày kết thúc.');
      }
      const rows = await studentWorkforceService.getAttendanceHistory({
        fromDate: fromDate || undefined,
        toDate: toDate || undefined,
        page,
        pageSize: 20,
      });
      if (!rows || !Array.isArray(rows.items)) {
        throw new Error('Dữ liệu lịch sử chấm công không đúng định dạng.');
      }
      setHistory(rows);
    } catch (loadError) {
      setHistoryError(loadError.message || 'Không thể tải lịch sử chấm công.');
    } finally {
      setHistoryLoading(false);
    }
  }, [fromDate, toDate]);

  useEffect(() => {
    void loadToday();
    void loadHistory(1);
  }, [loadHistory, loadToday]);

  const submit = async (action) => {
    if (!todayLoaded || todayError || sending) return;
    setSending(true);
    try {
      const result = action === 'check-in'
        ? await studentWorkforceService.checkIn(notes)
        : await studentWorkforceService.checkOut();
      setAttendance(result ?? null);
      setTodayLoaded(true);
      setNotes('');
      toast.success(action === 'check-in' ? 'Đã ghi nhận giờ vào.' : 'Đã ghi nhận giờ ra.');
      await Promise.all([loadToday(), loadHistory(1)]);
    } catch (requestError) {
      toast.error(requestError.message || 'Không thể cập nhật chấm công.');
    } finally {
      setSending(false);
    }
  };

  const checkedIn = todayLoaded && attendance?.status === 'CHECKED_IN';
  const completed = todayLoaded && attendance?.status === 'COMPLETED';

  return (
    <DashboardLayout title="Chấm công của tôi" subtitle="Ghi nhận giờ vào, giờ ra và xem lịch sử chấm công cá nhân.">
      <div className="space-y-5">
        <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-7">
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div>
              <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">Ngày làm việc tại Việt Nam</p>
              <h2 className="mt-1 text-xl font-bold text-slate-900">{formatDate(today)}</h2>
              <p className="mt-2 text-sm text-slate-600">
                Trạng thái:{' '}
                <span className="font-semibold">
                  {todayLoading ? 'Đang tải...' : todayError ? 'Chưa xác định' : attendance ? attendanceStatusLabel(attendance.status) : 'Chưa có lượt chấm công'}
                </span>
              </p>
            </div>
            <div className="rounded-xl bg-indigo-50 p-3 text-indigo-700"><Clock3 className="h-6 w-6" /></div>
          </div>

          {todayError && (
            <div role="alert" className="mt-4 flex items-center justify-between gap-3 rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800">
              <span>{todayError}</span>
              <button type="button" onClick={() => void loadToday()} disabled={todayLoading} className="inline-flex shrink-0 items-center gap-2 font-semibold disabled:opacity-50">
                <RefreshCw className="h-4 w-4" /> Thử lại
              </button>
            </div>
          )}

          {todayLoading && <div role="status" className="mt-4 flex items-center gap-2 text-sm text-slate-500">Đang tải trạng thái chấm công...</div>}

          <div className="mt-5 grid gap-3 sm:grid-cols-3">
            <Info label="Giờ vào" value={attendance ? formatDateTime(attendance.checkInTimeUtc) : '—'} />
            <Info label="Giờ ra" value={attendance ? formatDateTime(attendance.checkOutTimeUtc) : '—'} />
            <Info label="Thời lượng" value={attendance?.durationMinutes == null ? '—' : attendance.durationMinutes + ' phút'} />
          </div>

          {todayLoaded && !attendance && (
            <div className="mt-5 flex flex-col gap-3 sm:flex-row sm:items-end">
              <label className="flex-1 text-xs font-semibold text-slate-600">
                Ghi chú (tối đa 500 ký tự)
                <textarea maxLength={500} value={notes} onChange={(event) => setNotes(event.target.value)} rows={2} className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm font-normal" />
              </label>
              <button type="button" disabled={sending || todayLoading} onClick={() => void submit('check-in')} className="inline-flex items-center justify-center gap-2 rounded-lg bg-indigo-600 px-4 py-3 text-sm font-semibold text-white disabled:opacity-50">
                <LogIn className="h-4 w-4" />{sending ? 'Đang gửi...' : 'Chấm công vào'}
              </button>
            </div>
          )}

          {checkedIn && (
            <div className="mt-5 flex justify-end">
              <button type="button" disabled={sending || todayLoading} onClick={() => void submit('check-out')} className="inline-flex items-center justify-center gap-2 rounded-lg bg-emerald-600 px-4 py-3 text-sm font-semibold text-white disabled:opacity-50">
                <LogOut className="h-4 w-4" />{sending ? 'Đang gửi...' : 'Chấm công ra'}
              </button>
            </div>
          )}

          {completed && <p className="mt-5 rounded-lg bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-800">Phiên chấm công hôm nay đã hoàn tất.</p>}
        </section>

        <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
          <div className="flex flex-wrap items-end justify-between gap-3 border-b border-slate-100 p-5">
            <div>
              <h2 className="text-sm font-bold text-slate-900">Lịch sử chấm công</h2>
              <p className="mt-1 text-xs text-slate-500">Chỉ hiển thị dữ liệu của tài khoản đang đăng nhập.</p>
            </div>
            <div className="flex flex-wrap gap-2">
              <label className="text-[11px] text-slate-500">Từ ngày<input type="date" max={toDate || undefined} value={fromDate} onChange={(event) => setFromDate(event.target.value)} className="mt-1 block rounded-lg border border-slate-300 px-2 py-2 text-xs" /></label>
              <label className="text-[11px] text-slate-500">Đến ngày<input type="date" min={fromDate || undefined} value={toDate} onChange={(event) => setToDate(event.target.value)} className="mt-1 block rounded-lg border border-slate-300 px-2 py-2 text-xs" /></label>
              <button type="button" onClick={() => void loadHistory(1)} disabled={historyLoading} className="rounded-lg border border-slate-300 px-3 py-2 text-xs font-semibold disabled:opacity-50">Lọc</button>
            </div>
          </div>

          {historyError && (
            <div role="alert" className="m-5 flex items-center justify-between gap-3 rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800">
              <span>{historyError}</span>
              <button type="button" onClick={() => void loadHistory(history?.pageNumber || 1)} disabled={historyLoading} className="inline-flex shrink-0 items-center gap-1 font-semibold disabled:opacity-50">
                <RefreshCw className="h-4 w-4" /> Thử lại
              </button>
            </div>
          )}

          {historyLoading ? (
            <div role="status" className="p-8 text-center text-sm text-slate-500">Đang tải lịch sử chấm công...</div>
          ) : historyError ? null : history?.items.length ? (
            <div className="overflow-x-auto">
              <table className="w-full min-w-[700px] text-left text-xs">
                <thead className="bg-slate-50 text-[10px] uppercase text-slate-500">
                  <tr><th className="p-3">Ngày</th><th className="p-3">Giờ vào</th><th className="p-3">Giờ ra</th><th className="p-3">Thời lượng</th><th className="p-3">Trạng thái phiên</th><th className="p-3">Ghi chú</th></tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {history.items.map((row) => (
                    <tr key={row.id}>
                      <td className="p-3">{formatDate(row.date)}</td>
                      <td className="p-3">{formatDateTime(row.checkInTimeUtc)}</td>
                      <td className="p-3">{formatDateTime(row.checkOutTimeUtc)}</td>
                      <td className="p-3">{row.durationMinutes == null ? '—' : row.durationMinutes + ' phút'}</td>
                      <td className="p-3">{attendanceStatusLabel(row.status)}</td>
                      <td className="max-w-64 whitespace-pre-wrap p-3">{row.notes || '—'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : history ? (
            <p role="status" className="p-8 text-center text-sm text-slate-500">Chưa có bản ghi chấm công trong khoảng thời gian này.</p>
          ) : null}

          {history && (
            <div className="flex items-center justify-between border-t border-slate-100 px-5 py-3 text-xs text-slate-500">
              <span>{history.totalItems} bản ghi</span>
              <div className="flex gap-2">
                <button type="button" disabled={historyLoading || history.pageNumber <= 1} onClick={() => void loadHistory(history.pageNumber - 1)} className="rounded border px-3 py-1.5 disabled:opacity-40">Trước</button>
                <span className="py-1.5">Trang {history.pageNumber} / {history.totalPages || 1}</span>
                <button type="button" disabled={historyLoading || history.pageNumber >= history.totalPages} onClick={() => void loadHistory(history.pageNumber + 1)} className="rounded border px-3 py-1.5 disabled:opacity-40">Sau</button>
              </div>
            </div>
          )}
        </section>
      </div>
    </DashboardLayout>
  );
};

const Info = ({ label, value }) => (
  <div className="rounded-xl border border-slate-200 bg-slate-50 p-3">
    <p className="text-[10px] font-bold uppercase tracking-wide text-slate-500">{label}</p>
    <p className="mt-1 break-words text-sm font-semibold text-slate-800">{value}</p>
  </div>
);

export default StudentAttendanceView;
