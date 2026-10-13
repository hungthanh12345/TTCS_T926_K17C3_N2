import React, { useCallback, useEffect, useState } from 'react';
import { CalendarOff, Loader2, RefreshCw, Send } from 'lucide-react';
import toast from 'react-hot-toast';
import DashboardLayout from '../../components/layout/DashboardLayout';
import studentWorkforceService from '../../services/studentWorkforceService';

const statusLabel = { PENDING: 'Chờ HR duyệt', APPROVED: 'Đã duyệt', REJECTED: 'Đã từ chối' };
const formatDate = (value) => value
  ? new Date(String(value).slice(0, 10) + 'T00:00:00').toLocaleDateString('vi-VN')
  : '—';

export const StudentLeaveRequestsView = () => {
  const [items, setItems] = useState(null);
  const [form, setForm] = useState({ startDate: '', endDate: '', reason: '' });
  const [loading, setLoading] = useState(true);
  const [sending, setSending] = useState(false);
  const [loadError, setLoadError] = useState('');
  const [formError, setFormError] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    setItems(null);
    setLoadError('');
    try {
      const data = await studentWorkforceService.getLeaveRequests();
      if (!Array.isArray(data)) throw new Error('Dữ liệu đơn nghỉ phép không đúng định dạng.');
      setItems(data);
    } catch (error) {
      setLoadError(error.message || 'Không thể tải đơn xin nghỉ phép.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { void load(); }, [load]);

  const submit = async (event) => {
    event.preventDefault();
    if (!form.startDate || !form.endDate || form.endDate < form.startDate || !form.reason.trim()) {
      setFormError('Chọn khoảng ngày hợp lệ và nhập lý do nghỉ.');
      return;
    }
    setSending(true);
    setFormError('');
    try {
      await studentWorkforceService.createLeaveRequest({ ...form, reason: form.reason.trim() });
      setForm({ startDate: '', endDate: '', reason: '' });
      toast.success('Đã gửi đơn xin nghỉ phép.');
      await load();
    } catch (error) {
      setFormError(error.message || 'Không thể gửi đơn xin nghỉ phép.');
    } finally {
      setSending(false);
    }
  };

  return (
    <DashboardLayout title="Đơn xin nghỉ phép" subtitle="Gửi yêu cầu nghỉ và theo dõi trạng thái xử lý.">
      <div className="space-y-5">
        {loadError && (
          <div role="alert" className="flex justify-between gap-3 rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800">
            <span>{loadError}</span>
            <button type="button" onClick={() => void load()} disabled={loading} className="inline-flex shrink-0 items-center gap-1 font-semibold disabled:opacity-50">
              <RefreshCw className="h-4 w-4" /> Thử lại
            </button>
          </div>
        )}

        <form onSubmit={submit} className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
          <div className="mb-4 flex items-center gap-3">
            <span className="rounded-xl bg-indigo-50 p-2.5 text-indigo-700"><CalendarOff className="h-5 w-5" /></span>
            <div><h2 className="text-sm font-bold text-slate-900">Tạo đơn nghỉ phép</h2><p className="mt-1 text-xs text-slate-500">Khoảng nghỉ trùng với đơn đang chờ hoặc đã duyệt sẽ bị từ chối.</p></div>
          </div>
          <div className="grid gap-3 sm:grid-cols-2">
            <label className="text-xs font-semibold text-slate-600">Ngày bắt đầu<input required type="date" max={form.endDate || undefined} value={form.startDate} onChange={(event) => setForm({ ...form, startDate: event.target.value })} className="mt-1 block w-full rounded-lg border border-slate-300 px-3 py-2.5 font-normal" /></label>
            <label className="text-xs font-semibold text-slate-600">Ngày kết thúc<input required type="date" min={form.startDate || undefined} value={form.endDate} onChange={(event) => setForm({ ...form, endDate: event.target.value })} className="mt-1 block w-full rounded-lg border border-slate-300 px-3 py-2.5 font-normal" /></label>
          </div>
          <label className="mt-3 block text-xs font-semibold text-slate-600">Lý do<textarea required minLength={2} maxLength={2000} value={form.reason} onChange={(event) => setForm({ ...form, reason: event.target.value })} rows={4} className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2.5 font-normal" /></label>
          {formError && <p role="alert" className="mt-3 text-sm text-rose-700">{formError}</p>}
          <button type="submit" disabled={sending} className="mt-4 inline-flex items-center gap-2 rounded-lg bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-50">
            <Send className="h-4 w-4" />{sending ? 'Đang gửi...' : 'Gửi đơn nghỉ phép'}
          </button>
        </form>

        <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
          <div className="flex items-center justify-between border-b border-slate-100 p-5">
            <div><h2 className="text-sm font-bold text-slate-900">Lịch sử đơn xin nghỉ</h2><p className="mt-1 text-xs text-slate-500">Chỉ hiển thị các yêu cầu gắn với tài khoản của bạn.</p></div>
            <button type="button" disabled={loading} onClick={() => void load()} className="rounded-lg border border-slate-300 p-2 text-slate-600 disabled:opacity-50" aria-label="Tải lại đơn xin nghỉ">
              <RefreshCw className="h-4 w-4" />
            </button>
          </div>
          {loading ? (
            <div role="status" className="flex items-center justify-center gap-2 p-8 text-sm text-slate-500"><Loader2 className="h-4 w-4 animate-spin" /> Đang tải...</div>
          ) : loadError ? null : items?.length ? (
            <div className="divide-y divide-slate-100">
              {items.map((item) => (
                <article key={item.id} className="grid gap-2 p-4 sm:grid-cols-[1fr_auto]">
                  <div>
                    <p className="text-sm font-bold text-slate-800">{formatDate(item.startDate)} – {formatDate(item.endDate)}</p>
                    <p className="mt-1 whitespace-pre-wrap text-sm text-slate-600">{item.reason}</p>
                    {item.rejectionReason && <p className="mt-2 text-xs text-rose-700">Lý do từ chối: {item.rejectionReason}</p>}
                  </div>
                  <span className={'h-fit rounded-full px-3 py-1 text-xs font-bold ' + (item.status === 'APPROVED' ? 'bg-emerald-50 text-emerald-700' : item.status === 'REJECTED' ? 'bg-rose-50 text-rose-700' : 'bg-amber-50 text-amber-700')}>
                    {statusLabel[item.status] || 'Chưa xác định'}
                  </span>
                </article>
              ))}
            </div>
          ) : items ? (
            <p role="status" className="p-8 text-center text-sm text-slate-500">Bạn chưa có đơn xin nghỉ phép.</p>
          ) : null}
        </section>
      </div>
    </DashboardLayout>
  );
};

export default StudentLeaveRequestsView;
