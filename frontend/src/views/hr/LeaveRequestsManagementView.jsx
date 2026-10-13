import React, { useCallback, useEffect, useState } from 'react';
import { Check, Loader2, RefreshCw, X } from 'lucide-react';
import toast from 'react-hot-toast';
import DashboardLayout from '../../components/layout/DashboardLayout';
import hrWorkforceService from '../../services/hrWorkforceService';

const statusLabels = { PENDING: 'Chờ duyệt', APPROVED: 'Đã duyệt', REJECTED: 'Đã từ chối' };
const formatDate = (value) => value ? new Date(String(value).slice(0, 10) + 'T00:00:00').toLocaleDateString('vi-VN') : '—';

export const LeaveRequestsManagementView = () => {
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState('PENDING');
  const [result, setResult] = useState(null);
  const [loading, setLoading] = useState(true);
  const [busyId, setBusyId] = useState(null);
  const [error, setError] = useState('');

  const load = useCallback(async (nextPage = 1) => {
    setLoading(true);
    setResult(null);
    setError('');
    try {
      const data = await hrWorkforceService.getLeaveRequests({ status: status || undefined, page: nextPage, pageSize: 20 });
      if (!data || !Array.isArray(data.items) || !Number.isFinite(data.totalItems)) throw new Error('Dữ liệu đơn xin nghỉ phép không đúng định dạng.');
      setResult(data);
      setPage(nextPage);
    } catch (loadError) {
      setError(loadError.message || 'Không thể tải đơn xin nghỉ phép.');
    } finally {
      setLoading(false);
    }
  }, [status]);

  useEffect(() => { void load(1); }, [load]);

  const review = async (id, approve) => {
    if (busyId !== null) return;
    setBusyId(id);
    setError('');
    try {
      await hrWorkforceService.reviewLeave(id, { approve, rejectionReason: null });
      toast.success(approve ? 'Đã duyệt đơn nghỉ phép.' : 'Đã từ chối đơn nghỉ phép.');
      await load(page);
    } catch (requestError) {
      setResult(null);
      setError(requestError.message || 'Không thể xử lý đơn nghỉ phép.');
    } finally {
      setBusyId(null);
    }
  };

  return (
    <DashboardLayout title="Quản lý đơn xin nghỉ phép" subtitle="Xem và xử lý các yêu cầu nghỉ phép của sinh viên.">
      <div className="space-y-4">
        {error && <div role="alert" className="flex justify-between gap-3 rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800"><span>{error}</span><button type="button" onClick={() => void load(page)} disabled={loading} className="inline-flex items-center gap-1 font-semibold disabled:opacity-50"><RefreshCw className="h-4 w-4" /> Thử lại</button></div>}
        <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
          <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 p-5"><div><h2 className="text-sm font-bold text-slate-900">Đơn xin nghỉ phép</h2><p className="mt-1 text-xs text-slate-500">{result ? result.totalItems + ' yêu cầu' : 'Chưa có dữ liệu'}</p></div><div className="flex gap-2"><select aria-label="Lọc theo trạng thái" value={status} onChange={(event) => setStatus(event.target.value)} className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-xs"><option value="PENDING">Chờ duyệt</option><option value="APPROVED">Đã duyệt</option><option value="REJECTED">Đã từ chối</option><option value="">Tất cả</option></select><button type="button" disabled={loading} onClick={() => void load(page)} className="rounded-lg border border-slate-300 p-2 disabled:opacity-50" aria-label="Tải lại đơn"><RefreshCw className="h-4 w-4" /></button></div></div>
          {loading ? <div role="status" className="flex items-center justify-center gap-2 p-8 text-sm text-slate-500"><Loader2 className="h-4 w-4 animate-spin" /> Đang tải đơn...</div> : error ? null : result && result.items.length > 0 ? <div className="divide-y divide-slate-100">{result.items.map((item) => <article key={item.id} className="grid gap-3 p-4 lg:grid-cols-[1fr_auto]"><div><div className="flex flex-wrap items-center gap-2"><h3 className="text-sm font-bold text-slate-900">{item.studentName}</h3><span className="text-xs text-slate-500">{item.studentCode}</span><span className="rounded-full bg-slate-100 px-2 py-1 text-[10px] font-bold">{statusLabels[item.status] || 'Chưa xác định'}</span></div><p className="mt-2 text-xs font-semibold text-indigo-700">{formatDate(item.startDate)} – {formatDate(item.endDate)}</p><p className="mt-2 whitespace-pre-wrap text-sm text-slate-700">{item.reason}</p>{item.rejectionReason && <p className="mt-2 text-xs text-rose-700">Lý do từ chối: {item.rejectionReason}</p>}</div>{item.status === 'PENDING' && <div className="flex items-start gap-2"><button type="button" disabled={busyId !== null} onClick={() => void review(item.id, true)} className="inline-flex items-center gap-1 rounded-lg bg-emerald-600 px-3 py-2 text-xs font-semibold text-white disabled:opacity-50"><Check className="h-4 w-4" /> Duyệt</button><button type="button" disabled={busyId !== null} onClick={() => void review(item.id, false)} className="inline-flex items-center gap-1 rounded-lg border border-rose-300 px-3 py-2 text-xs font-semibold text-rose-700 disabled:opacity-50"><X className="h-4 w-4" /> Từ chối</button></div>}</article>)}</div> : result ? <p role="status" className="p-8 text-center text-sm text-slate-500">Không có đơn nghỉ phép trong bộ lọc.</p> : null}
          {result && !error && <div className="flex items-center justify-between border-t border-slate-100 px-5 py-3 text-xs text-slate-500"><span>Trang {page} / {result.totalPages || 1}</span><div className="flex gap-2"><button type="button" disabled={loading || page <= 1} onClick={() => void load(page - 1)} className="rounded border px-3 py-1.5 disabled:opacity-40">Trước</button><button type="button" disabled={loading || page >= result.totalPages} onClick={() => void load(page + 1)} className="rounded border px-3 py-1.5 disabled:opacity-40">Sau</button></div></div>}
        </section>
      </div>
    </DashboardLayout>
  );
};

export default LeaveRequestsManagementView;
