import React, { useCallback, useEffect, useState } from 'react';
import toast from 'react-hot-toast';
import {
  AlertTriangle,
  BadgeCheck,
  CalendarDays,
  Download,
  FileSignature,
  FileText,
  LoaderCircle,
  RefreshCw,
} from 'lucide-react';
import studentContractService from '../../services/studentContractService';
import Modal from '../common/Modal';
import Badge from '../common/Badge';

const STATUS_BADGE = {
  PENDING_CONFIRMATION: { variant: 'amber', label: 'Chờ xác nhận' },
  CONFIRMED: { variant: 'emerald', label: 'Đã xác nhận' },
  CANCELLED: { variant: 'rose', label: 'Đã hủy' },
};

const parseServerDate = (value) => {
  const text = String(value);
  const hasZone = /(Z|[+-]\d{2}:?\d{2})$/i.test(text);
  return new Date(text.includes('T') && !hasZone ? `${text}Z` : text);
};
const formatDate = (value) => (value ? parseServerDate(value).toLocaleDateString('vi-VN') : '—');
const formatDateTime = (value) => (value ? parseServerDate(value).toLocaleString('vi-VN') : '—');

const getErrorMessage = (error) =>
  error.response?.data?.message || error.message || 'Không thể xử lý hợp đồng.';

const Field = ({ label, value }) => (
  <div className="rounded-xl border border-slate-100 bg-slate-50 p-3.5">
    <p className="text-xs font-semibold text-slate-500">{label}</p>
    <p className="mt-1 break-words text-sm font-medium text-slate-800">{value || 'Chưa cập nhật'}</p>
  </div>
);

export const StudentContractPanel = () => {
  const [contract, setContract] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState('');
  const [agreed, setAgreed] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [isConfirming, setIsConfirming] = useState(false);

  const loadContract = useCallback(async () => {
    setIsLoading(true);
    setLoadError('');
    try {
      setContract(await studentContractService.getMyContract());
    } catch (error) {
      setLoadError(getErrorMessage(error));
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadContract();
  }, [loadContract]);

  const handleDownload = async () => {
    try {
      const response = await studentContractService.download(contract.id);
      const url = URL.createObjectURL(response.data);
      const anchor = window.document.createElement('a');
      anchor.href = url;
      anchor.download = contract.fileName || 'hop-dong-thuc-tap.pdf';
      anchor.click();
      window.setTimeout(() => URL.revokeObjectURL(url), 1000);
    } catch (error) {
      toast.error(getErrorMessage(error));
    }
  };

  const handleConfirm = async () => {
    setIsConfirming(true);
    try {
      const updated = await studentContractService.confirm(contract.id);
      setContract((current) => ({
        ...current,
        status: 'CONFIRMED',
        confirmedAt: new Date().toISOString(),
        ...(updated && typeof updated === 'object' ? updated : {}),
      }));
      setShowConfirm(false);
      setAgreed(false);
      toast.success('Xác nhận hợp đồng thành công.');
    } catch (error) {
      toast.error(getErrorMessage(error));
      // Trường hợp đã xác nhận trước đó / trạng thái thay đổi: tải lại để đồng bộ
      if ([400, 409].includes(error.response?.status)) {
        setShowConfirm(false);
        void loadContract();
      }
    } finally {
      setIsConfirming(false);
    }
  };

  const badge = contract ? STATUS_BADGE[contract.status] : null;
  const canConfirm = contract?.status === 'PENDING_CONFIRMATION';

  return (
    <section className="overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-xs">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 px-5 py-4 sm:px-6">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-xl border border-indigo-100 bg-indigo-50 text-indigo-600">
            <FileSignature className="h-5 w-5" />
          </div>
          <div>
            <h3 className="text-sm font-bold text-slate-900">Hợp đồng thực tập</h3>
            <p className="mt-0.5 text-xs text-slate-500">Xem nội dung hợp đồng và xác nhận để hoàn tất thủ tục.</p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          {badge && <Badge variant={badge.variant}>{badge.label}</Badge>}
          <button
            type="button"
            onClick={loadContract}
            disabled={isLoading}
            className="inline-flex items-center gap-1.5 rounded-lg px-2 py-1 text-xs font-medium text-slate-500 hover:bg-slate-100 hover:text-slate-800 disabled:opacity-50"
          >
            <RefreshCw className={`h-3.5 w-3.5 ${isLoading ? 'animate-spin' : ''}`} />
            Làm mới
          </button>
        </div>
      </div>

      <div className="p-5 sm:p-6">
        {isLoading ? (
          <div className="flex min-h-48 items-center justify-center rounded-xl border border-slate-100 bg-slate-50 text-sm text-slate-500">
            <LoaderCircle className="mr-2 h-4 w-4 animate-spin" /> Đang tải hợp đồng…
          </div>
        ) : loadError ? (
          <div className="rounded-xl border border-rose-100 bg-rose-50 p-4 text-sm text-rose-700">
            <p>{loadError}</p>
            <button type="button" onClick={loadContract} className="mt-2 font-semibold underline">Thử lại</button>
          </div>
        ) : !contract ? (
          <div className="flex min-h-48 flex-col items-center justify-center rounded-xl border border-dashed border-slate-200 bg-slate-50 px-4 text-center">
            <FileText className="mb-2 h-6 w-6 text-slate-300" />
            <p className="text-sm font-medium text-slate-600">Chưa có hợp đồng</p>
            <p className="mt-1 text-xs text-slate-400">Hợp đồng sẽ hiển thị tại đây sau khi bộ phận nhân sự phát hành.</p>
          </div>
        ) : (
          <div className="space-y-5">
            <div className="grid gap-3 sm:grid-cols-2">
              <Field label="Số hợp đồng" value={contract.contractNumber} />
              <Field label="Chương trình thực tập" value={contract.programName} />
              <Field label="Ngày bắt đầu" value={formatDate(contract.startDate)} />
              <Field label="Ngày kết thúc" value={formatDate(contract.endDate)} />
              <Field label="Ngày phát hành" value={formatDate(contract.issuedAt)} />
              <Field label="Mentor phụ trách" value={contract.mentorName} />
            </div>

            {contract.hasFile && (
              <div className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-slate-200 p-3.5">
                <div className="flex min-w-0 items-center gap-3">
                  <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-indigo-50 text-indigo-600">
                    <FileText className="h-4 w-4" />
                  </div>
                  <div className="min-w-0">
                    <p className="truncate text-xs font-semibold text-slate-800">
                      {contract.fileName || 'hop-dong-thuc-tap.pdf'}
                    </p>
                    <p className="mt-0.5 text-[11px] text-slate-500">Tệp hợp đồng đính kèm</p>
                  </div>
                </div>
                <button
                  type="button"
                  onClick={handleDownload}
                  className="inline-flex items-center gap-1 rounded-lg bg-indigo-50 px-3 py-2 text-xs font-semibold text-indigo-700 hover:bg-indigo-100"
                >
                  <Download className="h-3.5 w-3.5" /> Tải xuống để xem
                </button>
              </div>
            )}

            {contract.status === 'CONFIRMED' && (
              <div className="flex items-start gap-3 rounded-xl border border-emerald-100 bg-emerald-50 p-4">
                <BadgeCheck className="mt-0.5 h-5 w-5 shrink-0 text-emerald-600" />
                <div>
                  <p className="text-sm font-semibold text-emerald-800">Bạn đã xác nhận hợp đồng này</p>
                  <p className="mt-1 flex items-center gap-1.5 text-xs text-emerald-700">
                    <CalendarDays className="h-3.5 w-3.5" /> Thời điểm xác nhận: {formatDateTime(contract.confirmedAt)}
                  </p>
                </div>
              </div>
            )}

            {contract.status === 'CANCELLED' && (
              <div className="flex items-start gap-3 rounded-xl border border-rose-100 bg-rose-50 p-4 text-sm text-rose-700">
                <AlertTriangle className="mt-0.5 h-5 w-5 shrink-0" />
                <p>Hợp đồng này đã bị hủy nên không thể xác nhận. Vui lòng liên hệ bộ phận nhân sự.</p>
              </div>
            )}

            {canConfirm && (
              <form
                className="space-y-4 rounded-xl border border-indigo-100 bg-indigo-50/40 p-4"
                onSubmit={(event) => {
                  event.preventDefault();
                  if (agreed) setShowConfirm(true);
                }}
              >
                <label className="flex cursor-pointer items-start gap-3 text-sm text-slate-700">
                  <input
                    type="checkbox"
                    checked={agreed}
                    onChange={(event) => setAgreed(event.target.checked)}
                    className="mt-0.5 h-4 w-4 rounded border-slate-300 text-indigo-600 focus:ring-indigo-500"
                  />
                  <span>Tôi đã đọc, hiểu rõ và đồng ý với toàn bộ nội dung hợp đồng thực tập này.</span>
                </label>
                <button
                  type="submit"
                  disabled={!agreed}
                  className="inline-flex w-full items-center justify-center gap-2 rounded-xl bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-indigo-700 disabled:cursor-not-allowed disabled:opacity-50 sm:w-auto"
                >
                  <BadgeCheck className="h-4 w-4" /> Xác nhận hợp đồng
                </button>
              </form>
            )}
          </div>
        )}
      </div>

      <Modal
        isOpen={showConfirm}
        onClose={() => !isConfirming && setShowConfirm(false)}
        title="Xác nhận hợp đồng"
      >
        <p className="text-sm leading-6 text-slate-600">
          Sau khi xác nhận, bạn không thể hoàn tác hoặc xác nhận lại. Bạn chắc chắn muốn tiếp tục?
        </p>
        <div className="mt-5 flex justify-end gap-2">
          <button
            type="button"
            onClick={() => setShowConfirm(false)}
            disabled={isConfirming}
            className="rounded-xl bg-slate-100 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-200 disabled:opacity-50"
          >
            Hủy
          </button>
          <button
            type="button"
            onClick={handleConfirm}
            disabled={isConfirming}
            className="inline-flex items-center gap-2 rounded-xl bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
          >
            {isConfirming && <LoaderCircle className="h-4 w-4 animate-spin" />}
            {isConfirming ? 'Đang xác nhận…' : 'Đồng ý xác nhận'}
          </button>
        </div>
      </Modal>
    </section>
  );
};

export default StudentContractPanel;
