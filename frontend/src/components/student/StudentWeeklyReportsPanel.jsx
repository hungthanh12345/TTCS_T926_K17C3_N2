import React, { useCallback, useEffect, useState } from 'react';
import { CalendarDays, ExternalLink, FileText, Loader2, MessageSquareText, Pencil, RefreshCw, Send, X } from 'lucide-react';
import toast from 'react-hot-toast';
import weeklyReportService from '../../services/weeklyReportService';

const getLocalDateInput = (date) => {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

const getCurrentWeekStart = () => {
  const date = new Date();
  date.setHours(0, 0, 0, 0);
  date.setDate(date.getDate() - ((date.getDay() + 6) % 7));
  return getLocalDateInput(date);
};

const createEmptyForm = () => ({
  weekStartDate: getCurrentWeekStart(),
  workSummary: '',
  results: '',
  challenges: '',
  nextWeekPlan: '',
  attachmentUrl: '',
});

const formatWeek = (dateValue) => {
  if (!dateValue) return 'Chưa có tuần';
  const start = new Date(`${dateValue}T00:00:00`);
  const end = new Date(start);
  end.setDate(end.getDate() + 6);
  return `${start.toLocaleDateString('vi-VN')} – ${end.toLocaleDateString('vi-VN')}`;
};

const STATUS_LABELS = { SUBMITTED: 'Đã nộp', REVIEWED: 'Đã được Mentor phản hồi' };

export const StudentWeeklyReportsPanel = () => {
  const [reports, setReports] = useState([]);
  const [form, setForm] = useState(createEmptyForm);
  const [editingId, setEditingId] = useState(null);
  const [formError, setFormError] = useState('');
  const [loadError, setLoadError] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);

  const refreshReports = useCallback(async () => {
    setIsLoading(true);
    setLoadError('');
    try {
      setReports(await weeklyReportService.getMine());
    } catch (error) {
      setLoadError(error.message || 'Không thể tải báo cáo tuần.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    let isActive = true;
    weeklyReportService.getMine()
      .then((items) => { if (isActive) setReports(items); })
      .catch((error) => { if (isActive) setLoadError(error.message || 'Không thể tải báo cáo tuần.'); })
      .finally(() => { if (isActive) setIsLoading(false); });
    return () => { isActive = false; };
  }, []);

  const beginEdit = (report) => {
    setEditingId(report.id);
    setForm({
      weekStartDate: report.weekStartDate,
      workSummary: report.workSummary || '',
      results: report.results || '',
      challenges: report.challenges || '',
      nextWeekPlan: report.nextWeekPlan || '',
      attachmentUrl: report.attachmentUrl || '',
    });
    setFormError('');
  };

  const cancelEdit = () => {
    setEditingId(null);
    setForm(createEmptyForm());
    setFormError('');
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    setFormError('');
    if (form.workSummary.trim().length < 5) {
      setFormError('Nhập ít nhất 5 ký tự cho phần công việc đã thực hiện.');
      return;
    }

    const selectedDate = new Date(`${form.weekStartDate}T00:00:00`);
    if (selectedDate.getDay() !== 1 || form.weekStartDate > getCurrentWeekStart()) {
      setFormError('Ngày bắt đầu tuần phải là thứ Hai và không được nằm trong tương lai.');
      return;
    }

    if (form.attachmentUrl.trim()) {
      try {
        const attachment = new URL(form.attachmentUrl.trim());
        if (!['http:', 'https:'].includes(attachment.protocol) || attachment.username || attachment.password) {
          throw new Error('invalid');
        }
      } catch {
        setFormError('Đường dẫn đính kèm phải là URL HTTP hoặc HTTPS hợp lệ.');
        return;
      }
    }

    const payload = {
      ...form,
      workSummary: form.workSummary.trim(),
      results: form.results.trim() || null,
      challenges: form.challenges.trim() || null,
      nextWeekPlan: form.nextWeekPlan.trim() || null,
      attachmentUrl: form.attachmentUrl.trim() || null,
    };
    setIsSaving(true);
    try {
      if (editingId) {
        await weeklyReportService.update(editingId, payload);
        toast.success('Đã cập nhật báo cáo tuần.');
      } else {
        await weeklyReportService.create(payload);
        toast.success('Đã nộp báo cáo tuần.');
      }
      cancelEdit();
      await refreshReports();
    } catch (error) {
      setFormError(error.message || 'Không thể lưu báo cáo tuần.');
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <section className="space-y-5 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h3 className="flex items-center gap-2 text-base font-bold text-slate-900"><FileText className="h-5 w-5 text-indigo-600" /> Báo cáo thực tập hàng tuần</h3>
          <p className="mt-1 text-xs text-slate-500">Báo cáo công việc theo tuần và theo dõi phản hồi của Mentor.</p>
        </div>
        <button type="button" onClick={() => void refreshReports()} disabled={isLoading} className="inline-flex items-center gap-2 rounded-lg border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-600 hover:bg-slate-50 disabled:opacity-50">
          <RefreshCw className={`h-3.5 w-3.5 ${isLoading ? 'animate-spin' : ''}`} /> Làm mới
        </button>
      </div>

      <form onSubmit={handleSubmit} className="grid gap-3 rounded-xl border border-indigo-100 bg-indigo-50/50 p-4 md:grid-cols-2">
        <div className="md:col-span-2 flex items-center justify-between">
          <h4 className="text-sm font-bold text-slate-800">{editingId ? 'Chỉnh sửa báo cáo' : 'Nộp báo cáo tuần'}</h4>
          {editingId && <button type="button" onClick={cancelEdit} className="rounded-md p-1 text-slate-500 hover:bg-white" aria-label="Hủy chỉnh sửa"><X className="h-4 w-4" /></button>}
        </div>
        <label className="space-y-1 text-xs font-semibold text-slate-600">
          <span>Ngày bắt đầu tuần (thứ Hai)</span>
          <input type="date" required max={getCurrentWeekStart()} value={form.weekStartDate} onChange={(event) => setForm({ ...form, weekStartDate: event.target.value })} className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-indigo-500 focus:ring-4 focus:ring-indigo-100" />
        </label>
        <label className="space-y-1 text-xs font-semibold text-slate-600">
          <span>Link báo cáo/đính kèm (không bắt buộc)</span>
          <input type="url" maxLength={2048} value={form.attachmentUrl} onChange={(event) => setForm({ ...form, attachmentUrl: event.target.value })} placeholder="https://..." className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-indigo-500 focus:ring-4 focus:ring-indigo-100" />
        </label>
        <label className="space-y-1 text-xs font-semibold text-slate-600 md:col-span-2">
          <span>Công việc đã thực hiện *</span>
          <textarea required minLength={5} maxLength={4000} rows={3} value={form.workSummary} onChange={(event) => setForm({ ...form, workSummary: event.target.value })} className="w-full resize-y rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-indigo-500 focus:ring-4 focus:ring-indigo-100" />
        </label>
        <label className="space-y-1 text-xs font-semibold text-slate-600">
          <span>Kết quả đạt được</span>
          <textarea maxLength={4000} rows={3} value={form.results} onChange={(event) => setForm({ ...form, results: event.target.value })} className="w-full resize-y rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-indigo-500 focus:ring-4 focus:ring-indigo-100" />
        </label>
        <label className="space-y-1 text-xs font-semibold text-slate-600">
          <span>Khó khăn/vướng mắc</span>
          <textarea maxLength={4000} rows={3} value={form.challenges} onChange={(event) => setForm({ ...form, challenges: event.target.value })} className="w-full resize-y rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-indigo-500 focus:ring-4 focus:ring-indigo-100" />
        </label>
        <label className="space-y-1 text-xs font-semibold text-slate-600 md:col-span-2">
          <span>Kế hoạch tuần tiếp theo</span>
          <textarea maxLength={4000} rows={2} value={form.nextWeekPlan} onChange={(event) => setForm({ ...form, nextWeekPlan: event.target.value })} className="w-full resize-y rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-indigo-500 focus:ring-4 focus:ring-indigo-100" />
        </label>
        {formError && <p role="alert" className="text-sm text-rose-700 md:col-span-2">{formError}</p>}
        <div className="flex justify-end md:col-span-2">
          <button type="submit" disabled={isSaving} className="inline-flex items-center gap-2 rounded-lg bg-indigo-600 px-4 py-2.5 text-xs font-bold text-white hover:bg-indigo-500 disabled:cursor-not-allowed disabled:opacity-50">
            {isSaving ? <Loader2 className="h-4 w-4 animate-spin" /> : editingId ? <Pencil className="h-4 w-4" /> : <Send className="h-4 w-4" />}
            {editingId ? 'Lưu thay đổi' : 'Nộp báo cáo'}
          </button>
        </div>
      </form>

      <div>
        <h4 className="mb-3 text-sm font-bold text-slate-800">Báo cáo đã nộp <span className="ml-1 rounded-full bg-slate-100 px-2 py-0.5 text-xs text-slate-600">{reports.length}</span></h4>
        {isLoading ? <div role="status" className="flex items-center gap-2 py-6 text-sm text-slate-500"><Loader2 className="h-4 w-4 animate-spin" /> Đang tải báo cáo...</div> : loadError ? (
          <div role="alert" className="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800">{loadError}</div>
        ) : reports.length === 0 ? (
          <p role="status" className="rounded-xl border border-dashed border-slate-300 px-4 py-8 text-center text-sm text-slate-500">Bạn chưa nộp báo cáo tuần nào.</p>
        ) : (
          <div className="space-y-3">
            {reports.map((report) => (
              <article key={report.id} className="rounded-xl border border-slate-200 p-4 sm:p-5">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <p className="flex items-center gap-2 text-xs font-semibold text-indigo-700"><CalendarDays className="h-4 w-4" /> Tuần {formatWeek(report.weekStartDate)}</p>
                    <span className={`mt-2 inline-flex rounded-full px-2.5 py-1 text-[10px] font-bold ${report.isLate ? 'bg-rose-50 text-rose-700' : report.status === 'REVIEWED' ? 'bg-emerald-50 text-emerald-700' : 'bg-amber-50 text-amber-700'}`}>{STATUS_LABELS[report.status] || report.status}{report.isLate ? ' · Nộp trễ' : ' · Đúng hạn'}</span>
                  </div>
                  {report.status === 'SUBMITTED' && <button type="button" onClick={() => beginEdit(report)} className="inline-flex items-center gap-1.5 rounded-lg px-2.5 py-2 text-xs font-semibold text-indigo-700 hover:bg-indigo-50"><Pencil className="h-3.5 w-3.5" /> Chỉnh sửa</button>}
                </div>
                <h5 className="mt-3 text-sm font-bold text-slate-900">Công việc đã thực hiện</h5>
                <p className="mt-1 whitespace-pre-wrap text-sm leading-6 text-slate-700">{report.workSummary}</p>
                {report.results && <p className="mt-3 whitespace-pre-wrap text-sm text-slate-600"><strong>Kết quả:</strong> {report.results}</p>}
                {report.challenges && <p className="mt-2 whitespace-pre-wrap text-sm text-slate-600"><strong>Khó khăn:</strong> {report.challenges}</p>}
                {report.nextWeekPlan && <p className="mt-2 whitespace-pre-wrap text-sm text-slate-600"><strong>Kế hoạch tiếp theo:</strong> {report.nextWeekPlan}</p>}
                {report.attachmentUrl && <a href={report.attachmentUrl} target="_blank" rel="noopener noreferrer" className="mt-3 inline-flex items-center gap-1.5 text-xs font-semibold text-indigo-700 underline underline-offset-2"><ExternalLink className="h-3.5 w-3.5" /> Mở link báo cáo</a>}
                {report.mentorFeedback ? (
                  <div className="mt-4 rounded-xl border border-emerald-100 bg-emerald-50/70 p-4">
                    <h5 className="flex items-center gap-2 text-xs font-bold text-emerald-900"><MessageSquareText className="h-4 w-4" /> Phản hồi từ {report.mentorFeedback.mentorName}</h5>
                    <p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-emerald-950">{report.mentorFeedback.content}</p>
                  </div>
                ) : <p className="mt-4 border-t border-slate-100 pt-3 text-xs text-slate-500">Đang chờ Mentor phản hồi.</p>}
              </article>
            ))}
          </div>
        )}
      </div>
    </section>
  );
};

export default StudentWeeklyReportsPanel;
