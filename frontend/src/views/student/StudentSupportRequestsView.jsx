import React, { useCallback, useEffect, useState } from 'react';
import { LifeBuoy, Loader2, RefreshCw, Send } from 'lucide-react';
import toast from 'react-hot-toast';
import DashboardLayout from '../../components/layout/DashboardLayout';
import studentWorkforceService from '../../services/studentWorkforceService';

const statusLabels = { PENDING: 'Chờ tiếp nhận', IN_PROGRESS: 'Đang xử lý', RESOLVED: 'Đã giải quyết' };
const categoryLabels = { GENERAL: 'Hỗ trợ chung', DOCUMENT: 'Giấy tờ', CERTIFICATE: 'Chứng nhận', INTERNSHIP: 'Thực tập' };

export const StudentSupportRequestsView = () => {
  const [items, setItems] = useState(null);
  const [form, setForm] = useState({ category: 'GENERAL', subject: '', description: '' });
  const [loading, setLoading] = useState(true);
  const [sending, setSending] = useState(false);
  const [loadError, setLoadError] = useState('');
  const [formError, setFormError] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    setItems(null);
    setLoadError('');
    try {
      const data = await studentWorkforceService.getSupportRequests();
      if (!Array.isArray(data)) throw new Error('Dữ liệu yêu cầu hỗ trợ không đúng định dạng.');
      setItems(data);
    } catch (error) {
      setLoadError(error.message || 'Không thể tải yêu cầu hỗ trợ.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { void load(); }, [load]);

  const submit = async (event) => {
    event.preventDefault();
    if (!form.subject.trim() || !form.description.trim()) {
      setFormError('Nhập tiêu đề và nội dung yêu cầu.');
      return;
    }
    setSending(true);
    setFormError('');
    try {
      await studentWorkforceService.createSupportRequest({
        ...form,
        subject: form.subject.trim(),
        description: form.description.trim(),
      });
      setForm({ category: 'GENERAL', subject: '', description: '' });
      toast.success('Đã gửi yêu cầu hỗ trợ.');
      await load();
    } catch (error) {
      setFormError(error.message || 'Không thể gửi yêu cầu hỗ trợ.');
    } finally {
      setSending(false);
    }
  };

  return (
    <DashboardLayout title="Yêu cầu hỗ trợ" subtitle="Gửi yêu cầu đến bộ phận hỗ trợ và theo dõi phản hồi.">
      <div className="space-y-5">
        <form onSubmit={submit} className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
          <div className="mb-4 flex items-center gap-3">
            <span className="rounded-xl bg-sky-50 p-2.5 text-sky-700"><LifeBuoy className="h-5 w-5" /></span>
            <div><h2 className="text-sm font-bold text-slate-900">Gửi yêu cầu mới</h2><p className="mt-1 text-xs text-slate-500">Nội dung sẽ được chuyển đến HR/Admin xử lý.</p></div>
          </div>
          <div className="grid gap-3 sm:grid-cols-2">
            <label className="text-xs font-semibold text-slate-600">Loại yêu cầu
              <select value={form.category} onChange={(event) => setForm({ ...form, category: event.target.value })} className="mt-1 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 font-normal">
                <option value="GENERAL">Hỗ trợ chung</option><option value="DOCUMENT">Giấy tờ</option><option value="CERTIFICATE">Chứng nhận</option><option value="INTERNSHIP">Thực tập</option>
              </select>
            </label>
            <label className="text-xs font-semibold text-slate-600">Tiêu đề
              <input required minLength={3} maxLength={160} value={form.subject} onChange={(event) => setForm({ ...form, subject: event.target.value })} className="mt-1 block w-full rounded-lg border border-slate-300 px-3 py-2.5 font-normal" />
            </label>
          </div>
          <label className="mt-3 block text-xs font-semibold text-slate-600">Nội dung
            <textarea required minLength={2} maxLength={5000} rows={4} value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2.5 font-normal" />
          </label>
          {formError && <p role="alert" className="mt-3 text-sm text-rose-700">{formError}</p>}
          <button type="submit" disabled={sending} className="mt-4 inline-flex items-center gap-2 rounded-lg bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-50">
            <Send className="h-4 w-4" />{sending ? 'Đang gửi...' : 'Gửi yêu cầu'}
          </button>
        </form>

        {loadError && (
          <div role="alert" className="flex justify-between gap-3 rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800">
            <span>{loadError}</span>
            <button type="button" onClick={() => void load()} disabled={loading} className="inline-flex shrink-0 items-center gap-1 font-semibold disabled:opacity-50">
              <RefreshCw className="h-4 w-4" /> Thử lại
            </button>
          </div>
        )}

        <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
          <div className="flex items-center justify-between border-b border-slate-100 p-5">
            <div><h2 className="text-sm font-bold text-slate-900">Yêu cầu của tôi</h2><p className="mt-1 text-xs text-slate-500">Lịch sử chỉ hiển thị yêu cầu của tài khoản đang đăng nhập.</p></div>
            <button type="button" disabled={loading} onClick={() => void load()} className="rounded-lg border border-slate-300 p-2 disabled:opacity-50" aria-label="Tải lại yêu cầu">
              <RefreshCw className="h-4 w-4" />
            </button>
          </div>
          {loading ? (
            <div role="status" className="flex items-center justify-center gap-2 p-8 text-sm text-slate-500"><Loader2 className="h-4 w-4 animate-spin" /> Đang tải...</div>
          ) : loadError ? null : items?.length ? (
            <div className="divide-y divide-slate-100">
              {items.map((item) => (
                <article key={item.id} className="p-4">
                  <div className="flex flex-wrap items-start justify-between gap-2">
                    <div>
                      <p className="text-sm font-bold text-slate-900">{item.subject}</p>
                      <p className="mt-1 text-xs text-slate-500">{categoryLabels[item.category] || 'Khác'} · {new Date(item.createdAtUtc).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' })}</p>
                    </div>
                    <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-bold text-slate-700">{statusLabels[item.status] || 'Chưa xác định'}</span>
                  </div>
                  <p className="mt-3 whitespace-pre-wrap text-sm text-slate-700">{item.description}</p>
                  {item.response && <div className="mt-3 rounded-lg bg-indigo-50 p-3 text-sm text-indigo-900"><p className="text-[10px] font-bold uppercase">Phản hồi</p><p className="mt-1 whitespace-pre-wrap">{item.response}</p></div>}
                </article>
              ))}
            </div>
          ) : items ? (
            <p role="status" className="p-8 text-center text-sm text-slate-500">Bạn chưa gửi yêu cầu hỗ trợ.</p>
          ) : null}
        </section>
      </div>
    </DashboardLayout>
  );
};

export default StudentSupportRequestsView;
