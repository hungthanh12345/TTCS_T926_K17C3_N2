import React, { useCallback, useEffect, useState } from 'react';
import { Loader2, RefreshCw } from 'lucide-react';
import DashboardLayout from '../../components/layout/DashboardLayout';
import studentWorkforceService from '../../services/studentWorkforceService';

const money = (value) => new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 2 }).format(Number(value)) + ' đ';
const date = (value) => value ? new Date(String(value).slice(0, 10) + 'T00:00:00').toLocaleDateString('vi-VN') : '—';

export const StudentAllowanceHistoryView = () => {
  const [filters, setFilters] = useState({ fromDate: '', toDate: '' });
  const [result, setResult] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = useCallback(async (page = 1) => {
    setLoading(true);
    setResult(null);
    setError('');
    try {
      if (filters.fromDate && filters.toDate && filters.fromDate > filters.toDate) throw new Error('Ngày bắt đầu không được sau ngày kết thúc.');
      const data = await studentWorkforceService.getAllowanceHistory({ ...filters, fromDate: filters.fromDate || undefined, toDate: filters.toDate || undefined, page, pageSize: 20 });
      if (!data || !Array.isArray(data.items) || !Number.isFinite(data.totalReceived) || !Number.isFinite(data.totalItems) || !Number.isFinite(data.pageNumber) || !Number.isFinite(data.totalPages)) throw new Error('Dữ liệu lịch sử thanh toán không đúng định dạng.');
      setResult(data);
    } catch (loadError) {
      setError(loadError.message || 'Không thể tải lịch sử nhận phụ cấp.');
    } finally {
      setLoading(false);
    }
  }, [filters]);

  useEffect(() => { void load(1); }, [load]);

  return (
    <DashboardLayout title="Lịch sử nhận phụ cấp" subtitle="Chỉ hiển thị các khoản thanh toán đã ghi nhận cho tài khoản của bạn.">
      <div className="space-y-5">
        <section className="grid gap-3 rounded-2xl border border-slate-200 bg-white p-4 shadow-sm sm:grid-cols-[1fr_1fr_auto]">
          <label className="text-xs font-semibold text-slate-600">Từ ngày<input type="date" max={filters.toDate || undefined} value={filters.fromDate} onChange={(event) => setFilters({ ...filters, fromDate: event.target.value })} className="mt-1 block w-full rounded-lg border border-slate-300 px-3 py-2" /></label>
          <label className="text-xs font-semibold text-slate-600">Đến ngày<input type="date" min={filters.fromDate || undefined} value={filters.toDate} onChange={(event) => setFilters({ ...filters, toDate: event.target.value })} className="mt-1 block w-full rounded-lg border border-slate-300 px-3 py-2" /></label>
          <button type="button" disabled={loading} onClick={() => void load(1)} className="self-end rounded-lg bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-50">Áp dụng bộ lọc</button>
        </section>
        {error && <div role="alert" className="flex items-center justify-between gap-3 rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800"><span>{error}</span><button type="button" onClick={() => void load(1)} disabled={loading} className="inline-flex shrink-0 items-center gap-1 font-semibold disabled:opacity-50"><RefreshCw className="h-4 w-4" /> Thử lại</button></div>}
        <section className="grid gap-3 sm:grid-cols-2"><article className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm"><p className="text-xs font-semibold text-slate-500">Tổng tiền đã nhận trong bộ lọc</p><p className="mt-2 text-2xl font-extrabold text-emerald-700">{result ? money(result.totalReceived) : '—'}</p></article><article className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm"><p className="text-xs font-semibold text-slate-500">Số giao dịch</p><p className="mt-2 text-2xl font-extrabold text-slate-900">{result ? result.totalItems : '—'}</p></article></section>
        <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
          <div className="border-b border-slate-100 p-5"><h2 className="text-sm font-bold text-slate-900">Lịch sử thanh toán</h2></div>
          {loading ? <div role="status" className="flex items-center justify-center gap-2 p-8 text-sm text-slate-500"><Loader2 className="h-4 w-4 animate-spin" /> Đang tải giao dịch...</div> : error ? null : result.items.length === 0 ? <p role="status" className="p-8 text-center text-sm text-slate-500">Không có giao dịch nào trong khoảng thời gian này.</p> : <div className="overflow-x-auto"><table className="w-full min-w-[650px] text-left text-xs"><thead className="bg-slate-50 text-[10px] uppercase text-slate-500"><tr><th className="p-3">Thời điểm thanh toán</th><th className="p-3">Khoản phụ cấp</th><th className="p-3">Kỳ áp dụng</th><th className="p-3">Số tiền đã nhận</th><th className="p-3">Mã tham chiếu</th></tr></thead><tbody className="divide-y divide-slate-100">{result.items.map((item) => <tr key={item.id}><td className="p-3">{new Date(item.paidAtUtc).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' })}</td><td className="p-3 font-semibold">{item.allowanceType}</td><td className="p-3">{date(item.periodStart)} – {date(item.periodEnd)}</td><td className="p-3 font-semibold text-emerald-700">{money(item.amount)}</td><td className="p-3">{item.reference || '—'}</td></tr>)}</tbody></table></div>}
          {result && !error && <div className="flex items-center justify-between border-t border-slate-100 px-5 py-3 text-xs text-slate-500"><span>Trang {result.pageNumber} / {result.totalPages || 1}</span><div className="flex gap-2"><button type="button" disabled={loading || result.pageNumber <= 1} onClick={() => void load(result.pageNumber - 1)} className="rounded border px-3 py-1.5 disabled:opacity-40">Trước</button><button type="button" disabled={loading || result.pageNumber >= result.totalPages} onClick={() => void load(result.pageNumber + 1)} className="rounded border px-3 py-1.5 disabled:opacity-40">Sau</button></div></div>}
        </section>
      </div>
    </DashboardLayout>
  );
};

export default StudentAllowanceHistoryView;
