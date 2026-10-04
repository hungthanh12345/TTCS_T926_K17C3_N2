import React, { useCallback, useEffect, useState } from 'react';
import { CalendarDays, ExternalLink, FileText, Loader2, MessageSquareText, RefreshCw, Send } from 'lucide-react';
import toast from 'react-hot-toast';
import mentorWeeklyReportService from '../../services/mentorWeeklyReportService';

const formatWeek = (dateValue) => {
  if (!dateValue) return 'Chưa có tuần';
  const start = new Date(`${dateValue}T00:00:00`);
  const end = new Date(start);
  end.setDate(end.getDate() + 6);
  return `${start.toLocaleDateString('vi-VN')} – ${end.toLocaleDateString('vi-VN')}`;
};

const STATUS_LABELS = { SUBMITTED: 'Chờ phản hồi', REVIEWED: 'Đã phản hồi' };

export const MentorWeeklyReportsPanel = () => {
  const [reports, setReports] = useState([]);
  const [selectedReport, setSelectedReport] = useState(null);
  const [feedbackDraft, setFeedbackDraft] = useState('');
  const [loadError, setLoadError] = useState('');
  const [detailError, setDetailError] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingDetail, setIsLoadingDetail] = useState(false);
  const [isSaving, setIsSaving] = useState(false);

  const refreshReports = useCallback(async () => {
    setIsLoading(true);
    setLoadError('');
    try {
      setReports(await mentorWeeklyReportService.getAssigned());
    } catch (error) {
      setLoadError(error.message || 'Không thể tải báo cáo của sinh viên được phân công.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    let isActive = true;
    mentorWeeklyReportService.getAssigned()
      .then((items) => { if (isActive) setReports(items); })
      .catch((error) => { if (isActive) setLoadError(error.message || 'Không thể tải báo cáo.'); })
      .finally(() => { if (isActive) setIsLoading(false); });
    return () => { isActive = false; };
  }, []);

  const openReport = async (reportId) => {
    setIsLoadingDetail(true);
    setDetailError('');
    try {
      const report = await mentorWeeklyReportService.getById(reportId);
      setSelectedReport(report);
      setFeedbackDraft(report.canEditFeedback ? report.mentorFeedback?.content || '' : '');
    } catch (error) {
      setSelectedReport(null);
      setDetailError(error.message || 'Không thể tải chi tiết báo cáo.');
    } finally {
      setIsLoadingDetail(false);
    }
  };

  const submitFeedback = async (event) => {
    event.preventDefault();
    if (!selectedReport || feedbackDraft.trim().length < 2) return;
    setIsSaving(true);
    try {
      if (selectedReport.canEditFeedback) {
        await mentorWeeklyReportService.updateFeedback(selectedReport.id, feedbackDraft.trim());
        toast.success('Đã cập nhật phản hồi.');
      } else {
        await mentorWeeklyReportService.createFeedback(selectedReport.id, feedbackDraft.trim());
        toast.success('Đã gửi phản hồi cho sinh viên.');
      }
      await openReport(selectedReport.id);
      await refreshReports();
    } catch (error) {
      setDetailError(error.message || 'Không thể lưu phản hồi.');
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <section className="space-y-5 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h3 className="flex items-center gap-2 text-base font-bold text-slate-900"><FileText className="h-5 w-5 text-indigo-600" /> Báo cáo tuần của sinh viên</h3>
          <p className="mt-1 text-xs text-slate-500">Chỉ hiển thị báo cáo từ các sinh viên do bạn phụ trách.</p>
        </div>
        <button type="button" onClick={() => void refreshReports()} disabled={isLoading} className="inline-flex items-center gap-2 rounded-lg border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-600 hover:bg-slate-50 disabled:opacity-50">
          <RefreshCw className={`h-3.5 w-3.5 ${isLoading ? 'animate-spin' : ''}`} /> Làm mới
        </button>
      </div>

      {isLoading ? <div role="status" className="flex items-center gap-2 py-6 text-sm text-slate-500"><Loader2 className="h-4 w-4 animate-spin" /> Đang tải báo cáo...</div> : loadError ? (
        <div role="alert" className="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800">{loadError}</div>
      ) : reports.length === 0 ? (
        <p role="status" className="rounded-xl border border-dashed border-slate-300 px-4 py-8 text-center text-sm text-slate-500">Chưa có báo cáo tuần từ sinh viên được phân công.</p>
      ) : (
        <div className="grid gap-4 lg:grid-cols-[minmax(0,0.9fr)_minmax(0,1.1fr)]">
          <div className="space-y-2">
            {reports.map((report) => (
              <button key={report.id} type="button" onClick={() => void openReport(report.id)} className={`w-full rounded-xl border p-4 text-left transition-colors ${selectedReport?.id === report.id ? 'border-indigo-300 bg-indigo-50' : 'border-slate-200 hover:bg-slate-50'}`}>
                <div className="flex items-start justify-between gap-2">
                  <div className="min-w-0">
                    <p className="truncate text-sm font-bold text-slate-900">{report.studentName}</p>
                    <p className="mt-0.5 text-xs text-slate-500">{report.studentCode}</p>
                  </div>
                  <span className={`shrink-0 rounded-full px-2 py-1 text-[10px] font-bold ${report.status === 'REVIEWED' ? 'bg-emerald-50 text-emerald-700' : 'bg-amber-50 text-amber-700'}`}>{STATUS_LABELS[report.status] || report.status}</span>
                </div>
                <p className="mt-2 flex items-center gap-1.5 text-xs font-medium text-indigo-700"><CalendarDays className="h-3.5 w-3.5" /> Tuần {formatWeek(report.weekStartDate)}</p>
                <p className="mt-2 line-clamp-2 text-xs leading-5 text-slate-600">{report.workSummary}</p>
              </button>
            ))}
          </div>

          <div className="min-w-0">
            {isLoadingDetail ? <div role="status" className="flex items-center gap-2 rounded-xl border border-slate-200 p-5 text-sm text-slate-500"><Loader2 className="h-4 w-4 animate-spin" /> Đang tải chi tiết...</div> : detailError && !selectedReport ? (
              <div role="alert" className="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800">{detailError}</div>
            ) : selectedReport ? (
              <article className="space-y-4 rounded-xl border border-slate-200 p-4 sm:p-5">
                <div>
                  <p className="text-xs font-semibold text-indigo-700">{selectedReport.studentName} · {selectedReport.studentCode}</p>
                  <h4 className="mt-1 text-sm font-bold text-slate-900">Báo cáo tuần {formatWeek(selectedReport.weekStartDate)}</h4>
                </div>
                <div className="space-y-3 text-sm text-slate-700">
                  <div><h5 className="text-xs font-bold text-slate-500">CÔNG VIỆC ĐÃ THỰC HIỆN</h5><p className="mt-1 whitespace-pre-wrap leading-6">{selectedReport.workSummary}</p></div>
                  {selectedReport.results && <div><h5 className="text-xs font-bold text-slate-500">KẾT QUẢ</h5><p className="mt-1 whitespace-pre-wrap leading-6">{selectedReport.results}</p></div>}
                  {selectedReport.challenges && <div><h5 className="text-xs font-bold text-slate-500">KHÓ KHĂN</h5><p className="mt-1 whitespace-pre-wrap leading-6">{selectedReport.challenges}</p></div>}
                  {selectedReport.nextWeekPlan && <div><h5 className="text-xs font-bold text-slate-500">KẾ HOẠCH TUẦN TỚI</h5><p className="mt-1 whitespace-pre-wrap leading-6">{selectedReport.nextWeekPlan}</p></div>}
                  {selectedReport.attachmentUrl && <a href={selectedReport.attachmentUrl} target="_blank" rel="noopener noreferrer" className="inline-flex items-center gap-1.5 text-xs font-semibold text-indigo-700 underline underline-offset-2"><ExternalLink className="h-3.5 w-3.5" /> Mở link báo cáo</a>}
                </div>

                {selectedReport.mentorFeedback && !selectedReport.canEditFeedback && (
                  <div className="rounded-xl border border-emerald-100 bg-emerald-50/70 p-4">
                    <h5 className="flex items-center gap-2 text-xs font-bold text-emerald-900"><MessageSquareText className="h-4 w-4" /> Phản hồi của {selectedReport.mentorFeedback.mentorName}</h5>
                    <p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-emerald-950">{selectedReport.mentorFeedback.content}</p>
                    <p className="mt-2 text-[11px] text-emerald-800">Phản hồi này do Mentor khác gửi và không thể chỉnh sửa từ tài khoản hiện tại.</p>
                  </div>
                )}

                {(selectedReport.canEditFeedback || !selectedReport.mentorFeedback) && (
                  <form onSubmit={submitFeedback} className="space-y-2 rounded-xl border border-indigo-100 bg-indigo-50/50 p-4">
                    <label className="block space-y-1 text-xs font-semibold text-slate-700">
                      <span>{selectedReport.canEditFeedback ? 'Chỉnh sửa phản hồi' : 'Gửi phản hồi'}</span>
                      <textarea required minLength={2} maxLength={4000} rows={4} value={feedbackDraft} onChange={(event) => setFeedbackDraft(event.target.value)} className="w-full resize-y rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm font-normal outline-none focus:border-indigo-500 focus:ring-4 focus:ring-indigo-100" />
                    </label>
                    {detailError && <p role="alert" className="text-xs text-rose-700">{detailError}</p>}
                    <button type="submit" disabled={isSaving || feedbackDraft.trim().length < 2} className="inline-flex items-center gap-2 rounded-lg bg-indigo-600 px-3.5 py-2 text-xs font-bold text-white hover:bg-indigo-500 disabled:cursor-not-allowed disabled:opacity-50">
                      {isSaving ? <Loader2 className="h-4 w-4 animate-spin" /> : <Send className="h-4 w-4" />}
                      {selectedReport.canEditFeedback ? 'Lưu phản hồi' : 'Gửi phản hồi'}
                    </button>
                  </form>
                )}
              </article>
            ) : <p role="status" className="rounded-xl border border-dashed border-slate-300 px-4 py-10 text-center text-sm text-slate-500">Chọn một báo cáo để xem chi tiết và phản hồi.</p>}
          </div>
        </div>
      )}
    </section>
  );
};

export default MentorWeeklyReportsPanel;
