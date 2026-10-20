import React, { useCallback, useEffect, useState } from 'react';
import { Loader2, RefreshCw, Save, Trash2, Wallet } from 'lucide-react';
import toast from 'react-hot-toast';
import DashboardLayout from '../../components/layout/DashboardLayout';
import hrWorkforceService from '../../services/hrWorkforceService';

const blank = () => ({ studentId: '', allowanceType: '', description: '', amount: '', periodStart: '', periodEnd: '' });
const money = (value) => new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 2 }).format(Number(value)) + ' đ';
const statusLabel = { ACTIVE: 'Đang áp dụng', CANCELLED: 'Đã hủy' };
const asStudents = (data) => Array.isArray(data) ? data : Array.isArray(data?.items) ? data.items : null;
const asPage = (data) => data && Array.isArray(data.items) && Number.isFinite(data.totalItems) ? data : null;

export const AllowancesManagementView = () => {
  const [students, setStudents] = useState(null);
  const [studentError, setStudentError] = useState('');
  const [result, setResult] = useState(null);
  const [form, setForm] = useState(blank());
  const [editingId, setEditingId] = useState(null);
  const [payments, setPayments] = useState({});
  const [studentSearch, setStudentSearch] = useState('');
  const [filters, setFilters] = useState({ studentId: '', fromDate: '', toDate: '' });
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [loadingStudents, setLoadingStudents] = useState(true);
  const [sending, setSending] = useState(false);
  const [error, setError] = useState('');

  const load = useCallback(async (nextPage = 1) => {
    setLoading(true);
    setResult(null);
    setError('');
    try {
      if (filters.fromDate && filters.toDate && filters.fromDate > filters.toDate) throw new Error('Ngày bắt đầu không được sau ngày kết thúc.');
      const data = asPage(await hrWorkforceService.getAllowances({
        studentId: filters.studentId || undefined,
        fromDate: filters.fromDate || undefined,
        toDate: filters.toDate || undefined,
        page: nextPage,
        pageSize: 20,
      }));
      if (!data) throw new Error('Dữ liệu phụ cấp không đúng định dạng.');
      setResult(data);
      setPage(nextPage);
    } catch (loadError) {
      setError(loadError.message || 'Không thể tải danh sách phụ cấp.');
    } finally {
      setLoading(false);
    }
  }, [filters]);

  const loadStudents = useCallback(async () => {
    setLoadingStudents(true);
    setStudents(null);
    setStudentError('');
    try {
      const data = asStudents(await hrWorkforceService.getStudents());
      if (!data) throw new Error('Dữ liệu sinh viên không đúng định dạng.');
      setStudents(data);
    } catch (loadError) {
      setStudentError(loadError.message || 'Không thể tải danh sách sinh viên.');
    } finally {
      setLoadingStudents(false);
    }
  }, []);

  useEffect(() => { void load(1); }, [load]);
  useEffect(() => { void loadStudents(); }, [loadStudents]);

  const save = async (event) => {
    event.preventDefault();
    if (sending || students === null) return;
    if (!editingId && !form.studentId) { setError('Vui lòng chọn sinh viên.'); return; }
    if (!form.allowanceType.trim() || !Number.isFinite(Number(form.amount)) || Number(form.amount) <= 0 || !form.periodStart || !form.periodEnd || form.periodEnd < form.periodStart) {
      setError('Vui lòng nhập đầy đủ thông tin và kiểm tra số tiền, khoảng thời gian.');
      return;
    }
    setSending(true);
    setError('');
    const payload = {
      allowanceType: form.allowanceType.trim(),
      description: form.description.trim() || null,
      amount: Number(form.amount),
      periodStart: form.periodStart,
      periodEnd: form.periodEnd,
    };
    try {
      if (editingId) await hrWorkforceService.updateAllowance(editingId, payload);
      else await hrWorkforceService.createAllowance(Number(form.studentId), payload);
      toast.success(editingId ? 'Đã cập nhật khoản phụ cấp.' : 'Đã tạo khoản phụ cấp.');
      setForm(blank());
      setEditingId(null);
      await load(page);
    } catch (requestError) {
      setResult(null);
      setError(requestError.message || 'Không thể lưu khoản phụ cấp.');
    } finally {
      setSending(false);
    }
  };

  const edit = async (item) => {
    if (sending) return;
    setError('');
    try {
      const detail = await hrWorkforceService.getAllowance(item.id);
      setForm({ studentId: String(detail.studentId), allowanceType: detail.allowanceType, description: detail.description || '', amount: String(detail.amount), periodStart: detail.periodStart, periodEnd: detail.periodEnd });
      setEditingId(detail.id);
      window.scrollTo({ top: 0, behavior: 'smooth' });
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải chi tiết khoản phụ cấp.');
    }
  };

  const cancel = async (item) => {
    if (sending || !window.confirm('Hủy khoản phụ cấp "' + item.allowanceType + '" của ' + item.studentName + '? Lịch sử thanh toán sẽ được giữ lại.')) return;
    setSending(true);
    setError('');
    try {
      await hrWorkforceService.cancelAllowance(item.id);
      toast.success('Đã hủy khoản phụ cấp.');
      await load(page);
    } catch (requestError) {
      setResult(null);
      setError(requestError.message || 'Không thể hủy khoản phụ cấp.');
    } finally {
      setSending(false);
    }
  };

  const recordPayment = async (item) => {
    const payment = payments[item.id] || { amount: '', reference: '' };
    if (sending || !payment.amount || !Number.isFinite(Number(payment.amount)) || Number(payment.amount) <= 0) {
      setError('Số tiền thanh toán phải lớn hơn 0.');
      return;
    }
    setSending(true);
    setError('');
    try {
      await hrWorkforceService.recordPayment(item.id, { amount: Number(payment.amount), reference: payment.reference.trim() || null });
      setPayments((current) => ({ ...current, [item.id]: { amount: '', reference: '' } }));
      toast.success('Đã ghi nhận thanh toán vào lịch sử chi trả.');
      await load(page);
    } catch (requestError) {
      setResult(null);
      setError(requestError.message || 'Không thể ghi nhận thanh toán.');
    } finally {
      setSending(false);
    }
  };

  const visibleStudents = (students || []).filter((student) => (student.fullName + ' ' + student.studentCode + ' ' + (student.email || '')).toLowerCase().includes(studentSearch.trim().toLowerCase()));

  return (
    <DashboardLayout title="Quản lý phụ cấp" subtitle="Quản lý khoản phụ cấp dự kiến và ghi nhận riêng các khoản đã thanh toán.">
      <div className="space-y-5">
        {(error || studentError) && <div role="alert" className="flex flex-col gap-2 rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800">{error && <div className="flex items-center justify-between gap-3"><span>{error}</span><button type="button" onClick={() => void load(page)} disabled={loading} className="inline-flex items-center gap-1 font-semibold disabled:opacity-50"><RefreshCw className="h-4 w-4" /> Thử tải phụ cấp</button></div>}{studentError && <div className="flex items-center justify-between gap-3"><span>{studentError}</span><button type="button" onClick={() => void loadStudents()} disabled={loadingStudents} className="inline-flex items-center gap-1 font-semibold disabled:opacity-50"><RefreshCw className="h-4 w-4" /> Thử tải sinh viên</button></div>}</div>}
        <form onSubmit={save} className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
          <div className="mb-4 flex items-center gap-3"><span className="rounded-xl bg-indigo-50 p-2.5 text-indigo-700"><Wallet className="h-5 w-5" /></span><div><h2 className="text-sm font-bold text-slate-900">{editingId ? 'Chỉnh sửa khoản phụ cấp' : 'Tạo khoản phụ cấp'}</h2><p className="mt-1 text-xs text-slate-500">Khoản dự kiến chỉ được tính là đã nhận sau khi ghi nhận thanh toán.</p></div></div>
          <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
            <label className="text-xs font-semibold text-slate-600">Tìm sinh viên<input type="search" disabled={Boolean(editingId) || loadingStudents || !students} value={studentSearch} onChange={(event) => setStudentSearch(event.target.value)} placeholder="Họ tên, mã sinh viên hoặc email" className="mt-1 block w-full rounded-lg border border-slate-300 px-3 py-2.5 font-normal" /><select required disabled={Boolean(editingId) || loadingStudents || !students} value={form.studentId} onChange={(event) => setForm({ ...form, studentId: event.target.value })} className="mt-1 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 font-normal"><option value="">{loadingStudents ? 'Đang tải sinh viên...' : 'Chọn sinh viên'}</option>{visibleStudents.map((student) => <option key={student.id} value={student.id}>{student.fullName} · {student.studentCode}</option>)}</select></label>
            <label className="text-xs font-semibold text-slate-600">Loại phụ cấp<input required minLength={2} maxLength={100} value={form.allowanceType} onChange={(event) => setForm({ ...form, allowanceType: event.target.value })} className="mt-1 block w-full rounded-lg border border-slate-300 px-3 py-2.5 font-normal" /></label>
            <label className="text-xs font-semibold text-slate-600">Số tiền (VNĐ)<input required type="number" min="0.01" step="0.01" value={form.amount} onChange={(event) => setForm({ ...form, amount: event.target.value })} className="mt-1 block w-full rounded-lg border border-slate-300 px-3 py-2.5 font-normal" /></label>
            <label className="text-xs font-semibold text-slate-600">Ngày bắt đầu<input required type="date" value={form.periodStart} onChange={(event) => setForm({ ...form, periodStart: event.target.value })} className="mt-1 block w-full rounded-lg border border-slate-300 px-3 py-2.5 font-normal" /></label>
            <label className="text-xs font-semibold text-slate-600">Ngày kết thúc<input required type="date" min={form.periodStart || undefined} value={form.periodEnd} onChange={(event) => setForm({ ...form, periodEnd: event.target.value })} className="mt-1 block w-full rounded-lg border border-slate-300 px-3 py-2.5 font-normal" /></label>
            <label className="text-xs font-semibold text-slate-600">Mô tả<input maxLength={1000} value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} className="mt-1 block w-full rounded-lg border border-slate-300 px-3 py-2.5 font-normal" /></label>
          </div>
          <div className="mt-4 flex gap-2"><button type="submit" disabled={sending || loadingStudents || !students || (!editingId && !form.studentId)} className="inline-flex items-center gap-2 rounded-lg bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-50"><Save className="h-4 w-4" />{sending ? 'Đang lưu...' : 'Lưu khoản phụ cấp'}</button>{editingId && <button type="button" disabled={sending} onClick={() => { setEditingId(null); setForm(blank()); }} className="rounded-lg border px-4 py-2.5 text-sm font-semibold disabled:opacity-50">Hủy chỉnh sửa</button>}</div>
        </form>
        <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
          <div className="grid gap-3 border-b border-slate-100 p-5 sm:grid-cols-2 xl:grid-cols-4">
            <label className="text-xs font-semibold text-slate-600">Lọc theo sinh viên<select disabled={loadingStudents || !students} value={filters.studentId} onChange={(event) => setFilters({ ...filters, studentId: event.target.value })} className="mt-1 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2 font-normal"><option value="">{loadingStudents ? 'Đang tải...' : 'Tất cả sinh viên'}</option>{(students || []).map((student) => <option key={student.id} value={student.id}>{student.fullName} · {student.studentCode}</option>)}</select></label>
            <label className="text-xs font-semibold text-slate-600">Kỳ phụ cấp từ ngày<input type="date" max={filters.toDate || undefined} value={filters.fromDate} onChange={(event) => setFilters({ ...filters, fromDate: event.target.value })} className="mt-1 block w-full rounded-lg border border-slate-300 px-3 py-2 font-normal" /></label>
            <label className="text-xs font-semibold text-slate-600">Kỳ phụ cấp đến ngày<input type="date" min={filters.fromDate || undefined} value={filters.toDate} onChange={(event) => setFilters({ ...filters, toDate: event.target.value })} className="mt-1 block w-full rounded-lg border border-slate-300 px-3 py-2 font-normal" /></label>
            <div className="flex items-end gap-2"><button type="button" disabled={loading} onClick={() => setFilters({ studentId: '', fromDate: '', toDate: '' })} className="rounded-lg border px-3 py-2 text-xs font-semibold disabled:opacity-50">Xóa bộ lọc</button><button type="button" disabled={loading} onClick={() => void load(page)} className="rounded-lg border border-slate-300 p-2 disabled:opacity-50" aria-label="Tải lại danh sách phụ cấp"><RefreshCw className="h-4 w-4" /></button></div>
            <p className="text-xs text-slate-500 sm:col-span-2 xl:col-span-4">{result ? result.totalItems + ' khoản phụ cấp.' : 'Chưa có dữ liệu bộ lọc.'} Thay đổi bộ lọc sẽ tự tải lại danh sách.</p>
          </div>
          {loading ? <div role="status" className="flex items-center justify-center gap-2 p-8 text-sm text-slate-500"><Loader2 className="h-4 w-4 animate-spin" /> Đang tải phụ cấp...</div> : error ? null : result && result.items.length === 0 ? <p role="status" className="p-8 text-center text-sm text-slate-500">Chưa có khoản phụ cấp nào.</p> : result && <div className="divide-y divide-slate-100">{result.items.map((item) => {
            const payment = payments[item.id] || { amount: '', reference: '' };
            return <article key={item.id} className="space-y-3 p-4"><div className="flex flex-wrap items-start justify-between gap-3"><div><div className="flex flex-wrap items-center gap-2"><h3 className="text-sm font-bold text-slate-900">{item.allowanceType}</h3><span className="rounded-full bg-slate-100 px-2 py-1 text-[10px] font-bold">{statusLabel[item.status] || 'Chưa xác định'}</span></div><p className="mt-1 text-xs text-slate-500">{item.studentName} · {item.studentCode} · {new Date(String(item.periodStart).slice(0, 10) + 'T00:00:00').toLocaleDateString('vi-VN')} – {new Date(String(item.periodEnd).slice(0, 10) + 'T00:00:00').toLocaleDateString('vi-VN')}</p><p className="mt-2 text-sm">Khoản dự kiến <strong>{money(item.amount)}</strong> · Đã thanh toán <strong>{money(item.paidAmount)}</strong> · Còn lại <strong>{money(item.remainingAmount)}</strong></p></div><div className="flex gap-2"><button type="button" disabled={sending || item.status !== 'ACTIVE'} onClick={() => void edit(item)} className="rounded-lg border px-3 py-2 text-xs font-semibold disabled:opacity-40">Sửa</button><button type="button" disabled={sending || item.status !== 'ACTIVE'} onClick={() => void cancel(item)} className="inline-flex items-center gap-1 rounded-lg border border-rose-200 px-3 py-2 text-xs font-semibold text-rose-700 disabled:opacity-40"><Trash2 className="h-4 w-4" /> Hủy</button></div></div>{item.status === 'ACTIVE' && <div className="grid gap-2 rounded-xl bg-slate-50 p-3 sm:grid-cols-[1fr_1.5fr_auto]"><label className="text-[11px] font-semibold text-slate-600">Số tiền thanh toán<input type="number" min="0.01" step="0.01" value={payment.amount} onChange={(event) => setPayments({ ...payments, [item.id]: { ...payment, amount: event.target.value } })} className="mt-1 block w-full rounded border border-slate-300 px-2 py-2" /></label><label className="text-[11px] font-semibold text-slate-600">Mã tham chiếu<input maxLength={150} value={payment.reference} onChange={(event) => setPayments({ ...payments, [item.id]: { ...payment, reference: event.target.value } })} className="mt-1 block w-full rounded border border-slate-300 px-2 py-2" /></label><button type="button" disabled={sending || !payment.amount} onClick={() => void recordPayment(item)} className="self-end rounded-lg bg-emerald-600 px-3 py-2 text-xs font-semibold text-white disabled:opacity-50">Ghi nhận thanh toán</button></div>}</article>;
          })}</div>}
          {result && !error && <div className="flex items-center justify-between border-t border-slate-100 px-5 py-3 text-xs text-slate-500"><span>Trang {page} / {result.totalPages || 1}</span><div className="flex gap-2"><button type="button" disabled={loading || page <= 1} onClick={() => void load(page - 1)} className="rounded border px-3 py-1.5 disabled:opacity-40">Trước</button><button type="button" disabled={loading || page >= result.totalPages} onClick={() => void load(page + 1)} className="rounded border px-3 py-1.5 disabled:opacity-40">Sau</button></div></div>}
        </section>
      </div>
    </DashboardLayout>
  );
};

export default AllowancesManagementView;
