import React, { useCallback, useEffect, useState } from 'react';
import { Loader2, RefreshCw } from 'lucide-react';
import toast from 'react-hot-toast';
import DashboardLayout from '../../components/layout/DashboardLayout';
import hrWorkforceService from '../../services/hrWorkforceService';

const transitions = { PENDING: 'IN_PROGRESS', IN_PROGRESS: 'RESOLVED' };
const statusLabels = { PENDING: 'Chờ xử lý', IN_PROGRESS: 'Đang xử lý', RESOLVED: 'Đã giải quyết' };
const categoryLabels = { GENERAL: 'Hỗ trợ chung', DOCUMENT: 'Giấy tờ', CERTIFICATE: 'Chứng nhận', INTERNSHIP: 'Thực tập' };
const nextStatusLabels = { IN_PROGRESS: 'Đang xử lý', RESOLVED: 'Đã giải quyết' };

export const SupportRequestsManagementView = () => {
  const [status, setStatus] = useState('PENDING');
  const [result, setResult] = useState(null);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [busyId, setBusyId] = useState(null);
  const [responses, setResponses] = useState({});
  const [error, setError] = useState('');

  const load = useCallback(async (nextPage = 1) => {
    setLoading(true);
    setResult(null);
    setError('');
    try {
      const data = await hrWorkforceService.getSupportRequests({ status: status || undefined, page: nextPage, pageSize: 20 });
      if (!data || !Array.isArray(data.items) || !Number.isFinite(data.totalItems)) throw new Error('Dữ liệu yêu cầu hỗ trợ không đúng định dạng.');
      setResult(data);
      setPage(nextPage);
    } catch (loadError) {
      setError(loadError.message || 'Không thể tải danh sách yêu cầu hỗ trợ.');
    } finally {
      setLoading(false);
    }
  }, [status]);

  useEffect(() => { void load(1); }, [load]);

  const advance = async (item) => {
    const nextStatus = transitions[item.status];
    if (!nextStatus || busyId !== null) return;
    setBusyId(item.id);
    setError('');
    try {
      await hrWorkforceService.updateSupportRequest(item.id, { status: nextStatus, response: responses[item.id] ?? item.response ?? null });
      toast.success('Đã chuyển yêu cầu sang trạng thái ' + nextStatusLabels[nextStatus] + '.');
      await load(page);
    } catch (requestError) {
      setResult(null);
      setError(requestError.message || 'Không thể cập nhật yêu cầu hỗ trợ.');
    } finally {
      setBusyId(null);
    }
  };

  return (
    <DashboardLayout title="Xử lý yêu cầu hỗ trợ" subtitle="Tiếp nhận và xử lý theo thứ tự: Chờ xử lý → Đang xử lý → Đã giải quyết. Yêu cầu đã giải quyết không thể mở lại.">
      <div className="space-y-4">
        {error && <div role="alert" className="flex items-center justify-between gap-3 rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800"><span>{error}</span><button type="button" onClick={() => void load(page)} disabled={loading} className="inline-flex items-center gap-1 font-semibold disabled:opacity-50"><RefreshCw className="h-4 w-4" /> Thử lại</button></div>}
        <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
          <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 p-5"><div><h2 className="text-sm font-bold text-slate-900">Danh sách yêu cầu hỗ trợ</h2><p className="mt-1 text-xs text-slate-500">{result ? result.totalItems + ' yêu cầu' : 'Chưa có dữ liệu'}</p></div><div className="flex gap-2"><select aria-label="Lọc theo trạng thái" value={status} onChange={(event) => setStatus(event.target.value)} className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-xs"><option value="PENDING">Chờ xử lý</option><option value="IN_PROGRESS">Đang xử lý</option><option value="RESOLVED">Đã giải quyết</option><option value="">Tất cả trạng thái</option></select><button type="button" disabled={loading} onClick={() => void load(page)} className="rounded-lg border border-slate-300 p-2 disabled:opacity-50" aria-label="Tải lại yêu cầu"><RefreshCw className="h-4 w-4" /></button></div></div>
          {loading ? <div role="status" className="flex items-center justify-center gap-2 p-8 text-sm text-slate-500"><Loader2 className="h-4 w-4 animate-spin" /> Đang tải yêu cầu...</div> : error ? null : result.items.length === 0 ? <p role="status" className="p-8 text-center text-sm text-slate-500">Không có yêu cầu nào ở trạng thái này.</p> : <div className="divide-y divide-slate-100">{result.items.map((item) => <article key={item.id} className="grid gap-4 p-4 lg:grid-cols-[1fr_auto]"><div><div className="flex flex-wrap items-center gap-2"><h3 className="text-sm font-bold text-slate-900">{item.subject}</h3><span className="rounded-full bg-indigo-50 px-2 py-1 text-[10px] font-bold text-indigo-800">{categoryLabels[item.category] || 'Khác'}</span><span className="rounded-full bg-slate-100 px-2 py-1 text-[10px] font-bold">{statusLabels[item.status] || 'Chưa xác định'}</span></div><p className="mt-1 text-xs text-slate-500">{item.studentName} · {item.studentCode} · {new Date(item.createdAtUtc).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' })}</p><p className="mt-3 whitespace-pre-wrap text-sm text-slate-700">{item.description}</p><label className="mt-3 block text-xs font-semibold text-slate-600">Phản hồi (không bắt buộc)<textarea maxLength={4000} rows={2} value={responses[item.id] ?? item.response ?? ''} onChange={(event) => setResponses({ ...responses, [item.id]: event.target.value })} className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 font-normal" /></label>{item.handledAtUtc && <p className="mt-2 text-[11px] text-slate-500">Cập nhật gần nhất: {new Date(item.handledAtUtc).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' })}</p>}</div>{transitions[item.status] && <button type="button" disabled={busyId !== null} onClick={() => void advance(item)} className="h-fit rounded-lg bg-indigo-600 px-4 py-2.5 text-xs font-semibold text-white disabled:opacity-50">{busyId === item.id ? 'Đang lưu...' : 'Chuyển sang ' + nextStatusLabels[transitions[item.status]]}</button>}</article>)}</div>}
          {result && !error && <div className="flex items-center justify-between border-t border-slate-100 px-5 py-3 text-xs text-slate-500"><span>Trang {page} / {result.totalPages || 1}</span><div className="flex gap-2"><button type="button" disabled={loading || page <= 1} onClick={() => void load(page - 1)} className="rounded border px-3 py-1.5 disabled:opacity-40">Trước</button><button type="button" disabled={loading || page >= result.totalPages} onClick={() => void load(page + 1)} className="rounded border px-3 py-1.5 disabled:opacity-40">Sau</button></div></div>}
        </section>
      </div>
    </DashboardLayout>
  );
};

export default SupportRequestsManagementView;
