import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { toast } from 'react-hot-toast';
import { Check, FileSpreadsheet, FileText, History, Loader2, RefreshCw, RotateCcw } from 'lucide-react';
import DashboardLayout from '../../components/layout/DashboardLayout';
import programCompletionService from '../../services/programCompletionService';
import reportExportService from '../../services/reportExportService';

const participationLabels = {
  UNVERIFIED: 'Chưa được HR xác minh',
  ACTIVE: 'Đang tham gia',
  ENDED: 'Đã kết thúc',
  CANCELLED: 'Đã hủy tham gia',
};

const eventLabels = {
  PARTICIPATION_STATUS_CHANGED: 'Cập nhật trạng thái tham gia',
  COMPLETION_CONFIRMED: 'HR xác nhận hoàn thành',
  COMPLETION_REVOKED: 'HR thu hồi xác nhận hoàn thành',
};

const formatRate = (value) => value == null ? 'Chưa có dữ liệu' : `${Number(value).toLocaleString('vi-VN', { maximumFractionDigits: 2 })}%`;
const formatDateTime = (value) => value ? new Date(value).toLocaleString('vi-VN') : '—';

const ProgramCompletionView = () => {
  const [dashboard, setDashboard] = useState(null);
  const [selectedProgramId, setSelectedProgramId] = useState('');
  const [draftStatuses, setDraftStatuses] = useState({});
  const [reasons, setReasons] = useState({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [busyStudentId, setBusyStudentId] = useState(null);
  const [pendingAction, setPendingAction] = useState(null);
  const [actionReason, setActionReason] = useState('');
  const [history, setHistory] = useState(null);
  const [historyTitle, setHistoryTitle] = useState('');
  const [historyLoading, setHistoryLoading] = useState(false);
  const [historyError, setHistoryError] = useState('');
  const [exporting, setExporting] = useState('');
  const [exportError, setExportError] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const result = await programCompletionService.getDashboard(
        selectedProgramId ? Number(selectedProgramId) : undefined,
      );
      setDashboard(result);
      setDraftStatuses({});
      setReasons({});
    } catch (loadError) {
      setDashboard(null);
      setError(loadError.message || 'Không thể tải dữ liệu hoàn thành chương trình.');
    } finally {
      setLoading(false);
    }
  }, [selectedProgramId]);

  useEffect(() => { void load(); }, [load]);

  const effectiveProgramId = selectedProgramId || (dashboard?.selectedProgramId ? String(dashboard.selectedProgramId) : '');

  const selectedSummary = useMemo(
    () => dashboard?.programs?.find((program) => String(program.programId) === effectiveProgramId),
    [dashboard, effectiveProgramId],
  );

  const handleStatusUpdate = async (student) => {
    const status = draftStatuses[student.studentId] || student.participationStatus;
    if (status === student.participationStatus || busyStudentId) return;
    setBusyStudentId(student.studentId);
    try {
      await programCompletionService.updateParticipation(student.studentId, {
        programId: Number(effectiveProgramId),
        status,
        reason: reasons[student.studentId] || null,
      });
      toast.success('Đã cập nhật trạng thái tham gia.');
      await load();
    } catch (actionError) {
      toast.error(actionError.message || 'Không thể cập nhật trạng thái tham gia.');
    } finally {
      setBusyStudentId(null);
    }
  };

  const submitAction = async () => {
    if (!pendingAction || busyStudentId) return;
    const { kind, student } = pendingAction;
    setBusyStudentId(student.studentId);
    try {
      if (kind === 'confirm') {
        await programCompletionService.confirmCompletion(student.studentId, Number(effectiveProgramId), actionReason);
        toast.success('Đã xác nhận hoàn thành chương trình.');
      } else {
        await programCompletionService.revokeCompletion(student.studentId, Number(effectiveProgramId), actionReason);
        toast.success('Đã thu hồi xác nhận hoàn thành.');
      }
      setPendingAction(null);
      setActionReason('');
      await load();
    } catch (actionError) {
      toast.error(actionError.message || 'Không thể cập nhật xác nhận hoàn thành.');
    } finally {
      setBusyStudentId(null);
    }
  };

  const openHistory = async (student) => {
    setHistory(null);
    setHistoryError('');
    setHistoryTitle(`Lịch sử xử lý — ${student.fullName}`);
    setHistoryLoading(true);
    try {
      const response = await programCompletionService.getHistory(student.studentId, Number(effectiveProgramId));
      setHistory(Array.isArray(response) ? response : []);
    } catch (historyLoadError) {
      setHistoryError(historyLoadError.message || 'Không thể tải lịch sử xử lý.');
    } finally {
      setHistoryLoading(false);
    }
  };

  const handleExport = async (format) => {
    if (!dashboard || loading || exporting) return;
    setExporting(format);
    setExportError('');
    try {
      const blob = await reportExportService.download(format, {
        programId: effectiveProgramId ? Number(effectiveProgramId) : undefined,
      });
      const objectUrl = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = objectUrl;
      link.download = `bao-cao-thuc-tap-${new Date().toISOString().replaceAll(':', '-')}.${format === 'excel' ? 'xlsx' : 'pdf'}`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.setTimeout(() => URL.revokeObjectURL(objectUrl), 1000);
    } catch (exportFailure) {
      setExportError(exportFailure.message || 'Không thể xuất báo cáo. Vui lòng thử lại sau.');
    } finally {
      setExporting('');
    }
  };

  return (
    <DashboardLayout
      title="Tỷ lệ hoàn thành chương trình"
      subtitle="HR phân loại trạng thái tham gia và xác nhận hoàn thành; hệ thống không tự suy luận từ ngày, chấm công hoặc hợp đồng."
    >
      <div className="space-y-5">
        {error && (
          <div role="alert" className="flex items-center justify-between gap-3 rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800">
            <span>{error}</span>
            <button type="button" onClick={() => void load()} disabled={loading} className="inline-flex items-center gap-1 font-semibold disabled:opacity-50">
              <RefreshCw className="h-4 w-4" /> Thử lại
            </button>
          </div>
        )}

        {loading ? (
          <div role="status" className="flex items-center gap-2 rounded-2xl border border-slate-200 bg-white p-6 text-sm text-slate-500">
            <Loader2 className="h-5 w-5 animate-spin" /> Đang tải thống kê...
          </div>
        ) : error ? null : !dashboard?.programs?.length ? (
          <p role="status" className="rounded-2xl border border-slate-200 bg-white p-8 text-center text-sm text-slate-500">Chưa có chương trình thực tập để thống kê.</p>
        ) : (
          <>
            <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
              <label className="block max-w-xl text-xs font-semibold text-slate-600">
                Chương trình thực tập
                <select
                  value={effectiveProgramId}
                  onChange={(event) => setSelectedProgramId(event.target.value)}
                  className="mt-1 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 font-normal"
                >
                  {dashboard.programs.map((program) => (
                    <option key={program.programId} value={program.programId}>{program.programName}</option>
                  ))}
                </select>
              </label>
            </section>

            {selectedSummary && (
              <>
                <section className="grid gap-4 sm:grid-cols-3">
                  <SummaryCard label="Sinh viên đủ điều kiện" value={selectedSummary.eligibleCount} />
                  <SummaryCard label="Đã được HR xác nhận" value={selectedSummary.completedCount} />
                  <SummaryCard label="Tỷ lệ hoàn thành" value={formatRate(selectedSummary.completionRate)} />
                </section>
                <div className="flex flex-wrap justify-end gap-2">
                  <button type="button" onClick={() => void handleExport('excel')} disabled={Boolean(exporting)} className="inline-flex items-center gap-2 rounded-lg bg-emerald-700 px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-50">
                    <FileSpreadsheet className="h-4 w-4" /> {exporting === 'excel' ? 'Đang xuất...' : 'Xuất báo cáo Excel'}
                  </button>
                  <button type="button" onClick={() => void handleExport('pdf')} disabled={Boolean(exporting)} className="inline-flex items-center gap-2 rounded-lg bg-indigo-700 px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-50">
                    <FileText className="h-4 w-4" /> {exporting === 'pdf' ? 'Đang xuất...' : 'Xuất báo cáo PDF'}
                  </button>
                </div>
                {exportError && <p role="alert" className="rounded-xl border border-rose-200 bg-rose-50 p-3 text-sm text-rose-800">{exportError}</p>}
              </>
            )}

            {!dashboard.students.length ? (
              <p role="status" className="rounded-2xl border border-slate-200 bg-white p-8 text-center text-sm text-slate-500">Chưa có hồ sơ sinh viên được gán vào chương trình này.</p>
            ) : (
              <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
                <div className="border-b border-slate-100 p-5">
                  <h2 className="text-sm font-bold text-slate-900">Trạng thái tham gia và xác nhận</h2>
                  <p className="mt-1 text-xs text-slate-500">Chỉ hồ sơ đã được duyệt và trạng thái Đang tham gia/Đã kết thúc mới được tính vào tỷ lệ.</p>
                </div>
                <div className="overflow-x-auto">
                  <table className="w-full min-w-[1050px] text-left text-sm">
                    <thead className="bg-slate-50 text-xs text-slate-500">
                      <tr>
                        <th className="px-4 py-3">Sinh viên</th>
                        <th className="px-4 py-3">Hồ sơ</th>
                        <th className="px-4 py-3">Trạng thái tham gia</th>
                        <th className="px-4 py-3">Đủ điều kiện</th>
                        <th className="px-4 py-3">Hoàn thành</th>
                        <th className="px-4 py-3">Thao tác</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {dashboard.students.map((student) => {
                        const selectedStatus = draftStatuses[student.studentId] || student.participationStatus;
                        return (
                          <tr key={student.studentId}>
                            <td className="px-4 py-3">
                              <div className="font-semibold text-slate-800">{student.fullName}</div>
                              <div className="text-xs text-slate-500">{student.studentCode}</div>
                            </td>
                            <td className="px-4 py-3">
                              <span className={student.applicationApproved ? 'text-emerald-700' : 'text-amber-700'}>
                                {student.applicationApproved ? 'Đã duyệt' : 'Chưa được duyệt'}
                              </span>
                            </td>
                            <td className="px-4 py-3">
                              <div className="flex min-w-64 gap-2">
                                <select
                                  value={selectedStatus}
                                  disabled={!student.applicationApproved || Boolean(busyStudentId)}
                                  onChange={(event) => setDraftStatuses((current) => ({ ...current, [student.studentId]: event.target.value }))}
                                  className="min-w-0 flex-1 rounded-lg border border-slate-300 bg-white px-2 py-2 text-xs disabled:bg-slate-100"
                                  aria-label={`Trạng thái tham gia ${student.fullName}`}
                                >
                                  {Object.entries(participationLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
                                </select>
                                <button
                                  type="button"
                                  disabled={!student.applicationApproved || selectedStatus === student.participationStatus || Boolean(busyStudentId)}
                                  onClick={() => void handleStatusUpdate(student)}
                                  className="rounded-lg bg-slate-800 px-3 py-2 text-xs font-semibold text-white disabled:cursor-not-allowed disabled:opacity-40"
                                >
                                  Lưu
                                </button>
                              </div>
                              <input
                                value={reasons[student.studentId] || ''}
                                onChange={(event) => setReasons((current) => ({ ...current, [student.studentId]: event.target.value }))}
                                disabled={!student.applicationApproved || Boolean(busyStudentId)}
                                maxLength={500}
                                placeholder="Lý do (không bắt buộc)"
                                className="mt-2 w-full rounded-lg border border-slate-200 px-2 py-1.5 text-xs disabled:bg-slate-100"
                              />
                            </td>
                            <td className="px-4 py-3">{student.isEligible ? 'Có' : 'Không'}</td>
                            <td className="px-4 py-3">
                              {student.completionConfirmed ? 'Đã xác nhận' : student.hasCurrentConfirmation ? 'Cần rà soát' : 'Chưa xác nhận'}
                            </td>
                            <td className="px-4 py-3">
                              <div className="flex flex-wrap gap-2">
                                {student.isEligible && !student.hasCurrentConfirmation && (
                                  <button type="button" disabled={Boolean(busyStudentId)} onClick={() => { setActionReason(''); setPendingAction({ kind: 'confirm', student }); }} className="inline-flex items-center gap-1 rounded-lg bg-emerald-700 px-3 py-2 text-xs font-semibold text-white disabled:opacity-50">
                                    <Check className="h-3.5 w-3.5" /> Xác nhận
                                  </button>
                                )}
                                {student.hasCurrentConfirmation && (
                                  <button type="button" disabled={Boolean(busyStudentId)} onClick={() => { setActionReason(''); setPendingAction({ kind: 'revoke', student }); }} className="inline-flex items-center gap-1 rounded-lg bg-amber-700 px-3 py-2 text-xs font-semibold text-white disabled:opacity-50">
                                    <RotateCcw className="h-3.5 w-3.5" /> Thu hồi
                                  </button>
                                )}
                                <button type="button" onClick={() => void openHistory(student)} className="inline-flex items-center gap-1 rounded-lg border border-slate-300 px-3 py-2 text-xs font-semibold text-slate-700">
                                  <History className="h-3.5 w-3.5" /> Lịch sử
                                </button>
                              </div>
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              </section>
            )}
          </>
        )}
      </div>

      {pendingAction && (
        <Modal title={pendingAction.kind === 'confirm' ? 'Xác nhận hoàn thành' : 'Thu hồi xác nhận'} onClose={() => setPendingAction(null)}>
          <p className="text-sm text-slate-600">
            {pendingAction.kind === 'confirm'
              ? `Xác nhận ${pendingAction.student.fullName} đã hoàn thành chương trình?`
              : `Thu hồi xác nhận hoàn thành của ${pendingAction.student.fullName}? Lịch sử trước đó vẫn được lưu.`}
          </p>
          <label className="mt-4 block text-xs font-semibold text-slate-600">
            Lý do (không bắt buộc)
            <textarea value={actionReason} onChange={(event) => setActionReason(event.target.value)} maxLength={500} rows={3} className="mt-1 block w-full rounded-lg border border-slate-300 p-3 font-normal" />
          </label>
          <div className="mt-5 flex justify-end gap-2">
            <button type="button" onClick={() => setPendingAction(null)} className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700">Hủy</button>
            <button type="button" onClick={() => void submitAction()} disabled={Boolean(busyStudentId)} className="rounded-lg bg-indigo-700 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50">
              {busyStudentId ? 'Đang lưu...' : 'Xác nhận'}
            </button>
          </div>
        </Modal>
      )}

      {(historyLoading || historyError || history) && (
        <Modal title={historyTitle} onClose={() => { setHistory(null); setHistoryError(''); }}>
          {historyLoading ? <p role="status" className="flex items-center gap-2 text-sm text-slate-600"><Loader2 className="h-4 w-4 animate-spin" /> Đang tải lịch sử...</p> : null}
          {historyError ? <p role="alert" className="text-sm text-rose-700">{historyError}</p> : null}
          {history && !historyLoading && !history.length ? <p role="status" className="text-sm text-slate-500">Chưa có lịch sử xử lý.</p> : null}
          {history?.length > 0 && !historyLoading && (
            <ol className="max-h-[60vh] space-y-3 overflow-y-auto">
              {history.map((event) => (
                <li key={event.id} className="rounded-xl border border-slate-200 p-3 text-sm">
                  <div className="font-semibold text-slate-800">{eventLabels[event.eventType] || event.eventType}</div>
                  {event.eventType === 'PARTICIPATION_STATUS_CHANGED' && (
                    <div className="mt-1 text-slate-600">{participationLabels[event.previousStatus] || 'Chưa xác định'} → {participationLabels[event.newStatus] || event.newStatus}</div>
                  )}
                  <div className="mt-1 text-xs text-slate-500">HR #{event.actorUserId} · {formatDateTime(event.occurredAtUtc)}</div>
                  {event.reason && <p className="mt-2 text-slate-600">{event.reason}</p>}
                </li>
              ))}
            </ol>
          )}
        </Modal>
      )}
    </DashboardLayout>
  );
};

const SummaryCard = ({ label, value }) => (
  <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
    <p className="text-xs font-semibold text-slate-500">{label}</p>
    <p className="mt-2 text-2xl font-bold text-slate-900">{value}</p>
  </section>
);

const Modal = ({ title, children, onClose }) => (
  <div className="fixed inset-0 z-[100] flex items-center justify-center bg-slate-950/50 p-4" role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) onClose(); }}>
    <section role="dialog" aria-modal="true" aria-label={title} className="w-full max-w-lg rounded-2xl bg-white p-5 shadow-2xl">
      <div className="mb-4 flex items-center justify-between gap-3">
        <h2 className="text-base font-bold text-slate-900">{title}</h2>
        <button type="button" onClick={onClose} aria-label="Đóng" className="rounded-lg px-2 py-1 text-slate-500 hover:bg-slate-100">×</button>
      </div>
      {children}
    </section>
  </div>
);

export default ProgramCompletionView;
