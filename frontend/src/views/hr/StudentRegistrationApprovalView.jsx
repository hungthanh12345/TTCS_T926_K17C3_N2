import React, { useEffect, useState } from 'react';
import { Check, ClipboardCheck, Download, FileText, GraduationCap, Loader2, RefreshCw, X } from 'lucide-react';
import toast from 'react-hot-toast';
import DashboardLayout from '../../components/layout/DashboardLayout';
import Modal from '../../components/common/Modal';
import { useAuth } from '../../context/AuthContext';
import studentRegistrationService from '../../services/studentRegistrationService';

const documentStatusLabels = {
  PENDING: 'Chờ xét duyệt',
  APPROVED: 'Đã duyệt',
  REJECTED: 'Đã từ chối',
};

const documentStatusClasses = {
  PENDING: 'bg-amber-100 text-amber-800',
  APPROVED: 'bg-emerald-100 text-emerald-800',
  REJECTED: 'bg-rose-100 text-rose-800',
};

export const StudentRegistrationApprovalView = () => {
  const { user } = useAuth();
  const canReviewDocuments = user?.role === 'ROLE_HR';
  const [registrations, setRegistrations] = useState([]);
  const [selected, setSelected] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [action, setAction] = useState('');
  const [documents, setDocuments] = useState([]);
  const [documentsLoading, setDocumentsLoading] = useState(false);
  const [downloadingId, setDownloadingId] = useState(null);
  const [documentActionId, setDocumentActionId] = useState(null);
  const [documentReview, setDocumentReview] = useState(null);
  const [confirmationKind, setConfirmationKind] = useState('');

  const statusLabels = {
    PENDING: 'Đang chờ duyệt',
    PENDING_APPROVAL: 'Đang chờ duyệt',
    APPROVED: 'Đã được duyệt',
    ACTIVE: 'Đang hoạt động',
    REJECTED: 'Đã từ chối',
    INACTIVE: 'Không hoạt động',
    LOCKED: 'Đã khóa',
  };

  const loadPending = async (quiet = false) => {
    if (!quiet) setIsRefreshing(true);
    try {
      const items = await studentRegistrationService.getPending();
      const list = Array.isArray(items) ? items : [];
      setRegistrations(list);
      setSelected((current) => current ? list.find((item) => item.studentId === current.studentId) || null : null);
    } catch (error) {
      toast.error(error.message || 'Không thể tải hồ sơ đang chờ duyệt.');
    } finally {
      setIsLoading(false);
      setIsRefreshing(false);
    }
  };

  useEffect(() => {
    let mounted = true;
    studentRegistrationService.getPending()
      .then((items) => {
        if (mounted) setRegistrations(Array.isArray(items) ? items : []);
      })
      .catch((error) => {
        if (mounted) toast.error(error.message || 'Không thể tải hồ sơ đang chờ duyệt.');
      })
      .finally(() => {
        if (mounted) setIsLoading(false);
      });
    return () => { mounted = false; };
  }, []);

  const showDetails = async (item) => {
    setSelected(item);
    setDocuments([]);
    setDocumentsLoading(true);
    try {
      const [details, files] = await Promise.all([
        studentRegistrationService.getPendingDetails(item.studentId),
        studentRegistrationService.getRegistrationDocuments(item.studentId),
      ]);
      setSelected(details);
      setDocuments(files);
    } catch (error) {
      toast.error(error.message || 'Không thể tải chi tiết hồ sơ.');
    } finally {
      setDocumentsLoading(false);
    }
  };

  const downloadDocument = async (document) => {
    if (!selected) return;
    setDownloadingId(document.id);
    try {
      const response = await studentRegistrationService.downloadRegistrationDocument(selected.studentId, document.id);
      const url = URL.createObjectURL(response.data);
      const link = window.document.createElement('a');
      link.href = url;
      link.download = document.originalFileName;
      link.click();
      URL.revokeObjectURL(url);
    } catch (error) {
      toast.error(error.message || 'Không thể tải tài liệu.');
    } finally {
      setDownloadingId(null);
    }
  };

  const submitDocumentReview = async () => {
    if (!selected || !documentReview || !canReviewDocuments) return;
    const { document, kind, reason } = documentReview;
    setDocumentActionId(document.id);
    try {
      const updatedDocument = kind === 'approve'
        ? await studentRegistrationService.approveRegistrationDocument(selected.studentId, document.id)
        : await studentRegistrationService.rejectRegistrationDocument(
          selected.studentId,
          document.id,
          reason.trim(),
        );
      setDocuments((current) => current.map((item) => (
        item.id === updatedDocument.id ? updatedDocument : item
      )));
      setDocumentReview(null);
      toast.success(kind === 'approve' ? 'Đã duyệt tài liệu.' : 'Đã từ chối tài liệu.');
    } catch (error) {
      toast.error(error.message || 'Không thể cập nhật kết quả tài liệu.');
    } finally {
      setDocumentActionId(null);
    }
  };

  const review = async () => {
    if (!selected) return;
    const kind = confirmationKind;
    if (!kind) return;
    setAction(kind);
    try {
      if (kind === 'approve') await studentRegistrationService.approve(selected.studentId);
      else await studentRegistrationService.reject(selected.studentId);
      toast.success(kind === 'approve' ? 'Đã duyệt hồ sơ thành công.' : 'Đã từ chối hồ sơ.');
      setConfirmationKind('');
      setSelected(null);
      await loadPending(true);
    } catch {
      toast.error(kind === 'approve'
        ? 'Không thể duyệt hồ sơ. Vui lòng thử lại.'
        : 'Không thể từ chối hồ sơ. Vui lòng thử lại.');
    } finally {
      setAction('');
    }
  };

  return (
    <DashboardLayout title="Xét duyệt đăng ký" subtitle="Kiểm tra hồ sơ sinh viên mới trước khi cấp quyền sử dụng hệ thống">
      <div className="grid gap-5 xl:grid-cols-[minmax(0,1.15fr)_minmax(340px,0.85fr)]">
        <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
          <div className="flex items-center justify-between border-b border-slate-200 px-5 py-4">
            <div>
              <h2 className="font-bold text-slate-900">Hồ sơ chờ duyệt</h2>
              <p className="mt-1 text-xs text-slate-500">{registrations.length} hồ sơ đang chờ HR xem xét</p>
            </div>
            <button type="button" onClick={() => loadPending()} disabled={isRefreshing} className="rounded-lg p-2 text-slate-500 hover:bg-slate-100 hover:text-indigo-700 disabled:opacity-50" aria-label="Làm mới">
              <RefreshCw className={`h-4 w-4 ${isRefreshing ? 'animate-spin' : ''}`} />
            </button>
          </div>
          {isLoading ? (
            <div className="flex items-center justify-center gap-2 p-12 text-sm text-slate-500"><Loader2 className="h-4 w-4 animate-spin" /> Đang tải hồ sơ...</div>
          ) : registrations.length === 0 ? (
            <div className="p-10 text-center text-sm text-slate-500">Hiện không có hồ sơ nào đang chờ duyệt.</div>
          ) : (
            <div className="divide-y divide-slate-100">
              {registrations.map((item) => (
                <button
                  type="button"
                  key={item.studentId}
                  onClick={() => showDetails(item)}
                  className={`flex w-full items-center gap-3 px-5 py-4 text-left transition hover:bg-indigo-50/60 ${selected?.studentId === item.studentId ? 'bg-indigo-50' : 'bg-white'}`}
                >
                  <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-indigo-100 text-indigo-700"><GraduationCap className="h-5 w-5" /></span>
                  <span className="min-w-0 flex-1">
                    <span className="block truncate text-sm font-semibold text-slate-900">{item.fullName}</span>
                    <span className="mt-1 block truncate text-xs text-slate-500">{item.email} · {item.studentCode}</span>
                  </span>
                  <span className="shrink-0 rounded-full bg-amber-100 px-2.5 py-1 text-[11px] font-bold text-amber-800">Đang chờ duyệt</span>
                </button>
              ))}
            </div>
          )}
        </section>

        <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          {!selected ? (
            <div className="flex min-h-64 flex-col items-center justify-center text-center text-slate-400">
              <ClipboardCheck className="mb-3 h-9 w-9" />
              <p className="text-sm font-medium">Chọn một hồ sơ để xem thông tin chi tiết.</p>
            </div>
          ) : (
            <>
              <div className="flex items-start justify-between gap-3 border-b border-slate-100 pb-4">
                <div>
                  <h2 className="text-lg font-bold text-slate-900">{selected.fullName}</h2>
                  <p className="mt-1 text-sm text-slate-500">Mã sinh viên: {selected.studentCode}</p>
                </div>
                <span className="rounded-full bg-amber-100 px-2.5 py-1 text-[11px] font-bold text-amber-800">{statusLabels[selected.status] || 'Không xác định'}</span>
              </div>
              <dl className="grid gap-4 py-5 sm:grid-cols-2">
                <Detail label="Email" value={selected.email} />
                <Detail label="Số điện thoại" value={selected.phoneNumber || 'Chưa cung cấp'} />
                <Detail label="Trường" value={selected.university} />
                <Detail label="Chuyên ngành" value={selected.major} />
                <Detail label="Chương trình" value={selected.programName || 'Chưa gắn chương trình'} />
                <Detail label="Ngày gửi" value={selected.submittedAt ? new Date(selected.submittedAt).toLocaleString('vi-VN') : '—'} />
              </dl>
              <section className="border-t border-slate-100 py-4">
                <h3 className="text-sm font-bold text-slate-900">Tài liệu hồ sơ</h3>
                {documentsLoading ? <p role="status" className="mt-3 text-xs text-slate-500">Đang tải danh sách tài liệu...</p> : documents.length === 0 ? <p className="mt-3 text-xs text-slate-500">Hồ sơ chưa có tài liệu đính kèm.</p> : (
                  <ul className="mt-3 space-y-2">
                    {documents.map((document) => (
                      <li key={document.id} className="rounded-xl border border-slate-200 px-3 py-3">
                        <div className="flex flex-wrap items-start justify-between gap-3">
                          <span className="flex min-w-0 flex-1 items-center gap-2 text-xs text-slate-700">
                            <FileText className="h-4 w-4 shrink-0 text-indigo-500" />
                            <span className="min-w-0">
                              <b className="block">{document.documentType === 'CV' ? 'CV' : document.documentType === 'INTERNSHIP_LETTER' ? 'Đơn xin thực tập' : document.documentType}</b>
                              <span className="block truncate text-slate-500">{document.originalFileName}</span>
                            </span>
                          </span>
                          <span className={`shrink-0 rounded-full px-2.5 py-1 text-[11px] font-bold ${documentStatusClasses[document.reviewStatus] || 'bg-slate-100 text-slate-700'}`}>
                            {documentStatusLabels[document.reviewStatus] || 'Trạng thái không xác định'}
                          </span>
                        </div>
                        {(document.reviewedAt || document.rejectionReason) && (
                          <div className="mt-2 space-y-1 text-xs text-slate-500">
                            {document.reviewedAt && (
                              <p>
                                Người xét duyệt: {document.reviewedByEmail || `#${document.reviewedByUserId || '—'}`}
                                {' · '}{new Date(document.reviewedAt).toLocaleString('vi-VN')}
                              </p>
                            )}
                            {document.reviewStatus === 'REJECTED' && document.rejectionReason && (
                              <p className="break-words">Lý do từ chối: {document.rejectionReason}</p>
                            )}
                          </div>
                        )}
                        <div className="mt-2 flex flex-wrap items-center gap-2">
                          <button type="button" disabled={downloadingId === document.id} onClick={() => void downloadDocument(document)} className="inline-flex items-center gap-1 rounded-lg px-2.5 py-1.5 text-xs font-semibold text-indigo-700 hover:bg-indigo-50 disabled:opacity-50">
                            <Download className="h-3.5 w-3.5" />{downloadingId === document.id ? 'Đang tải' : 'Tải xuống'}
                          </button>
                          {canReviewDocuments && (
                            <>
                              <button
                                type="button"
                                disabled={documentActionId === document.id}
                                onClick={() => setDocumentReview({ document, kind: 'approve', reason: '' })}
                                className="inline-flex items-center gap-1 rounded-lg px-2.5 py-1.5 text-xs font-semibold text-emerald-700 hover:bg-emerald-50 disabled:opacity-50"
                              >
                                <Check className="h-3.5 w-3.5" />Duyệt tài liệu
                              </button>
                              <button
                                type="button"
                                disabled={documentActionId === document.id}
                                onClick={() => setDocumentReview({ document, kind: 'reject', reason: '' })}
                                className="inline-flex items-center gap-1 rounded-lg px-2.5 py-1.5 text-xs font-semibold text-rose-700 hover:bg-rose-50 disabled:opacity-50"
                              >
                                <X className="h-3.5 w-3.5" />Từ chối tài liệu
                              </button>
                            </>
                          )}
                        </div>
                      </li>
                    ))}
                  </ul>
                )}
              </section>
              <div className="flex flex-col gap-2 border-t border-slate-100 pt-4 sm:flex-row">
                <button type="button" disabled={Boolean(action)} onClick={() => setConfirmationKind('approve')} className="inline-flex flex-1 items-center justify-center gap-2 rounded-xl bg-emerald-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-emerald-500 disabled:opacity-50">
                  {action === 'approve' ? <Loader2 className="h-4 w-4 animate-spin" /> : <Check className="h-4 w-4" />} Duyệt hồ sơ
                </button>
                <button type="button" disabled={Boolean(action)} onClick={() => setConfirmationKind('reject')} className="inline-flex flex-1 items-center justify-center gap-2 rounded-xl border border-rose-200 bg-white px-4 py-2.5 text-sm font-semibold text-rose-700 transition hover:bg-rose-50 disabled:opacity-50">
                  {action === 'reject' ? <Loader2 className="h-4 w-4 animate-spin" /> : <X className="h-4 w-4" />} Từ chối
                </button>
              </div>
            </>
          )}
        </section>
      </div>
      <Modal
        isOpen={Boolean(confirmationKind) && Boolean(selected)}
        onClose={() => { if (!action) setConfirmationKind(''); }}
        title={confirmationKind === 'approve' ? 'Xác nhận duyệt hồ sơ' : 'Xác nhận từ chối hồ sơ'}
        subtitle="Vui lòng kiểm tra lại trước khi cập nhật trạng thái đăng ký."
        icon={ClipboardCheck}
      >
        <p className="text-sm leading-6 text-slate-700">
          Bạn có chắc chắn muốn {confirmationKind === 'approve' ? 'duyệt' : 'từ chối'} hồ sơ đăng ký thực tập của <strong>{selected?.fullName}</strong> không?
        </p>
        <div className="mt-6 flex justify-end gap-3">
          <button type="button" disabled={Boolean(action)} onClick={() => setConfirmationKind('')} className="rounded-xl border border-slate-200 px-4 py-2.5 text-sm font-semibold text-slate-700 hover:bg-slate-50 disabled:opacity-50">Hủy</button>
          <button type="button" disabled={Boolean(action)} onClick={() => void review()} className={`inline-flex min-w-32 items-center justify-center gap-2 rounded-xl px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-60 ${confirmationKind === 'approve' ? 'bg-emerald-600 hover:bg-emerald-500' : 'bg-rose-600 hover:bg-rose-500'}`}>
            {action ? <Loader2 className="h-4 w-4 animate-spin" /> : confirmationKind === 'approve' ? <Check className="h-4 w-4" /> : <X className="h-4 w-4" />}
            {confirmationKind === 'approve' ? 'Duyệt hồ sơ' : 'Từ chối hồ sơ'}
          </button>
        </div>
      </Modal>
      <Modal
        isOpen={Boolean(documentReview)}
        onClose={() => { if (!documentActionId) setDocumentReview(null); }}
        title={documentReview?.kind === 'approve' ? 'Xác nhận duyệt tài liệu' : 'Xác nhận từ chối tài liệu'}
        subtitle="Bạn có thể thay đổi kết quả xét duyệt tài liệu sau này."
        icon={ClipboardCheck}
      >
        <p className="text-sm leading-6 text-slate-700">
          {documentReview?.document?.reviewStatus !== 'PENDING' && (
            <span className="mb-2 block rounded-lg bg-amber-50 px-3 py-2 text-amber-800">
              Tài liệu hiện có trạng thái {documentStatusLabels[documentReview?.document?.reviewStatus] || 'không xác định'}.
            </span>
          )}
          Xác nhận {documentReview?.kind === 'approve' ? 'duyệt' : 'từ chối'} tài liệu{' '}
          <strong>{documentReview?.document?.originalFileName}</strong>?
        </p>
        {documentReview?.kind === 'reject' && (
          <label className="mt-4 block space-y-1.5">
            <span className="text-sm font-semibold text-slate-700">
              Lý do từ chối <span className="font-normal text-slate-400">(không bắt buộc)</span>
            </span>
            <textarea
              value={documentReview.reason}
              onChange={(event) => setDocumentReview((current) => ({
                ...current,
                reason: event.target.value.slice(0, 1000),
              }))}
              rows={4}
              maxLength={1000}
              className="w-full resize-y rounded-xl border border-slate-200 px-3.5 py-2.5 text-sm text-slate-800 outline-none focus:border-indigo-400 focus:ring-4 focus:ring-indigo-100"
              placeholder="Nhập lý do (tối đa 1000 ký tự)"
            />
            <span className="block text-right text-xs text-slate-400">{documentReview.reason.length}/1000</span>
          </label>
        )}
        <div className="mt-6 flex justify-end gap-3">
          <button type="button" disabled={Boolean(documentActionId)} onClick={() => setDocumentReview(null)} className="rounded-xl border border-slate-200 px-4 py-2.5 text-sm font-semibold text-slate-700 hover:bg-slate-50 disabled:opacity-50">
            Hủy
          </button>
          <button
            type="button"
            disabled={Boolean(documentActionId) || !canReviewDocuments}
            onClick={() => void submitDocumentReview()}
            className={`inline-flex min-w-36 items-center justify-center gap-2 rounded-xl px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-60 ${documentReview?.kind === 'approve' ? 'bg-emerald-600 hover:bg-emerald-500' : 'bg-rose-600 hover:bg-rose-500'}`}
          >
            {documentActionId ? <Loader2 className="h-4 w-4 animate-spin" /> : documentReview?.kind === 'approve' ? <Check className="h-4 w-4" /> : <X className="h-4 w-4" />}
            {documentActionId ? 'Đang lưu...' : documentReview?.kind === 'approve' ? 'Duyệt tài liệu' : 'Từ chối tài liệu'}
          </button>
        </div>
      </Modal>
    </DashboardLayout>
  );
};

const Detail = ({ label, value }) => (
  <div>
    <dt className="text-[11px] font-bold uppercase tracking-wide text-slate-400">{label}</dt>
    <dd className="mt-1 break-words text-sm font-medium text-slate-800">{value}</dd>
  </div>
);

export default StudentRegistrationApprovalView;
