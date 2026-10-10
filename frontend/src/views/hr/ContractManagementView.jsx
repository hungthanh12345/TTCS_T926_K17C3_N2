import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import toast from 'react-hot-toast';
import {
  Download,
  FileSignature,
  LoaderCircle,
  RefreshCw,
  Search,
  UploadCloud,
} from 'lucide-react';
import DashboardLayout from '../../components/layout/DashboardLayout';
import Badge from '../../components/common/Badge';
import hrContractService from '../../services/hrContractService';

const MAX_FILE_SIZE = 10 * 1024 * 1024;
const ACCEPTED_EXTENSIONS = ['.pdf', '.doc', '.docx'];

const STATUS_BADGE = {
  PENDING_CONFIRMATION: { variant: 'amber', label: 'Chờ sinh viên xác nhận' },
  CONFIRMED: { variant: 'emerald', label: 'Đã xác nhận' },
  CANCELLED: { variant: 'rose', label: 'Đã hủy' },
};

const getErrorMessage = (error) =>
  error.response?.data?.message || error.message || 'Không thể xử lý hợp đồng.';

const validateFile = (file) => {
  if (!file) return 'Vui lòng chọn tệp.';
  const extension = `.${file.name.split('.').pop()?.toLowerCase()}`;
  if (!ACCEPTED_EXTENSIONS.includes(extension)) return 'Chỉ chấp nhận tệp PDF, DOC hoặc DOCX.';
  if (file.size === 0) return 'Tệp không được để trống.';
  if (file.size > MAX_FILE_SIZE) return 'Dung lượng tệp tối đa là 10 MB.';
  return null;
};

const formatSize = (size) => {
  if (!size) return '—';
  if (size < 1024) return `${size} B`;
  if (size < 1024 * 1024) return `${(size / 1024).toFixed(0)} KB`;
  return `${(size / (1024 * 1024)).toFixed(1)} MB`;
};

const formatDateTime = (value) => (value ? new Date(value).toLocaleString('vi-VN') : '—');

export const ContractManagementView = () => {
  const [rows, setRows] = useState([]);
  const [search, setSearch] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState('');
  const [uploadingId, setUploadingId] = useState(null);
  const fileInputRef = useRef(null);
  const targetStudentRef = useRef(null);

  const loadContracts = useCallback(async () => {
    setIsLoading(true);
    setLoadError('');
    try {
      setRows(await hrContractService.getContracts());
    } catch (error) {
      setLoadError(getErrorMessage(error));
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadContracts();
  }, [loadContracts]);

  const filteredRows = useMemo(() => {
    const query = search.trim().toLocaleLowerCase('vi');
    if (!query) return rows;
    return rows.filter((row) =>
      [row.studentName, row.studentCode, row.programName, row.contractNumber]
        .filter(Boolean)
        .some((value) => value.toLocaleLowerCase('vi').includes(query)));
  }, [rows, search]);

  const beginUpload = (studentId) => {
    targetStudentRef.current = studentId;
    fileInputRef.current?.click();
  };

  const handleFileChosen = async (event) => {
    const file = event.target.files?.[0];
    const studentId = targetStudentRef.current;
    event.target.value = '';
    targetStudentRef.current = null;
    if (!file || !studentId) return;

    const validationError = validateFile(file);
    if (validationError) {
      toast.error(validationError);
      return;
    }

    setUploadingId(studentId);
    try {
      const updated = await hrContractService.upload(studentId, file);
      setRows((current) => current.map((row) => (row.studentId === studentId ? { ...row, ...updated } : row)));
      toast.success('Đã lưu hợp đồng.');
    } catch (error) {
      toast.error(getErrorMessage(error));
      if (error.response?.status === 409) void loadContracts();
    } finally {
      setUploadingId(null);
    }
  };

  const handleDownload = async (row) => {
    try {
      const response = await hrContractService.download(row.studentId);
      const url = URL.createObjectURL(response.data);
      const anchor = window.document.createElement('a');
      anchor.href = url;
      anchor.download = row.fileName || 'hop-dong-thuc-tap.pdf';
      anchor.click();
      window.setTimeout(() => URL.revokeObjectURL(url), 1000);
    } catch (error) {
      toast.error(getErrorMessage(error));
    }
  };

  return (
    <DashboardLayout
      title="Hợp đồng thực tập"
      subtitle="Tải lên và quản lý bản số hóa hợp đồng của từng sinh viên"
    >
      <section className="overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-xs">
        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 px-5 py-4 sm:px-6">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl border border-indigo-100 bg-indigo-50 text-indigo-600">
              <FileSignature className="h-5 w-5" />
            </div>
            <div>
              <h3 className="text-sm font-bold text-slate-900">Quản lý hợp đồng</h3>
              <p className="mt-0.5 text-xs text-slate-500">
                PDF, DOC, DOCX · tối đa 10 MB. Hợp đồng đã được sinh viên xác nhận thì không thể thay thế.
              </p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <div className="relative">
              <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
              <input
                type="search"
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Tìm theo tên, mã sinh viên…"
                aria-label="Tìm kiếm sinh viên"
                className="w-64 rounded-xl border border-slate-200 bg-white py-2 pl-9 pr-3 text-sm text-slate-800 outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
              />
            </div>
            <button
              type="button"
              onClick={loadContracts}
              disabled={isLoading}
              className="inline-flex items-center gap-1.5 rounded-lg px-2 py-1.5 text-xs font-medium text-slate-500 hover:bg-slate-100 hover:text-slate-800 disabled:opacity-50"
            >
              <RefreshCw className={`h-3.5 w-3.5 ${isLoading ? 'animate-spin' : ''}`} />
              Làm mới
            </button>
          </div>
        </div>

        <input
          ref={fileInputRef}
          type="file"
          accept=".pdf,.doc,.docx,application/pdf,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
          onChange={handleFileChosen}
          className="sr-only"
          aria-label="Chọn tệp hợp đồng"
        />

        {isLoading ? (
          <div className="flex min-h-48 items-center justify-center text-sm text-slate-500">
            <LoaderCircle className="mr-2 h-4 w-4 animate-spin" /> Đang tải danh sách…
          </div>
        ) : loadError ? (
          <div className="m-5 rounded-xl border border-rose-100 bg-rose-50 p-4 text-sm text-rose-700">
            <p>{loadError}</p>
            <button type="button" onClick={loadContracts} className="mt-2 font-semibold underline">Thử lại</button>
          </div>
        ) : filteredRows.length === 0 ? (
          <div className="flex min-h-48 flex-col items-center justify-center px-4 text-center">
            <FileSignature className="mb-2 h-6 w-6 text-slate-300" />
            <p className="text-sm font-medium text-slate-600">Không có sinh viên phù hợp</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[860px] text-left text-sm">
              <thead className="border-b border-slate-100 bg-slate-50 text-xs font-semibold text-slate-600">
                <tr>
                  <th className="px-5 py-3">Sinh viên</th>
                  <th className="px-3 py-3">Chương trình</th>
                  <th className="px-3 py-3">Tệp hợp đồng</th>
                  <th className="px-3 py-3">Trạng thái</th>
                  <th className="px-5 py-3 text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {filteredRows.map((row) => {
                  const badge = STATUS_BADGE[row.status];
                  const isConfirmed = row.status === 'CONFIRMED';
                  const isUploading = uploadingId === row.studentId;
                  return (
                    <tr key={row.studentId} className="align-top hover:bg-slate-50/60">
                      <td className="px-5 py-3.5">
                        <p className="font-semibold text-slate-800">{row.studentName}</p>
                        <p className="mt-0.5 text-xs text-slate-500">{row.studentCode}</p>
                      </td>
                      <td className="px-3 py-3.5 text-slate-700">{row.programName || '—'}</td>
                      <td className="px-3 py-3.5">
                        {row.hasFile ? (
                          <>
                            <p className="max-w-56 truncate text-xs font-semibold text-slate-800" title={row.fileName}>
                              {row.fileName}
                            </p>
                            <p className="mt-0.5 text-[11px] text-slate-500">
                              {formatSize(row.sizeBytes)} · {formatDateTime(row.fileUploadedAt)}
                            </p>
                          </>
                        ) : (
                          <span className="text-xs text-slate-400">Chưa có tệp</span>
                        )}
                      </td>
                      <td className="px-3 py-3.5">
                        {badge ? <Badge variant={badge.variant}>{badge.label}</Badge> : <span className="text-xs text-slate-400">Chưa có hợp đồng</span>}
                        {isConfirmed && (
                          <p className="mt-1 text-[11px] text-slate-500">{formatDateTime(row.confirmedAt)}</p>
                        )}
                      </td>
                      <td className="px-5 py-3.5">
                        <div className="flex flex-wrap items-center justify-end gap-1.5">
                          {row.hasFile && (
                            <button
                              type="button"
                              onClick={() => handleDownload(row)}
                              className="inline-flex items-center gap-1 rounded-lg bg-indigo-50 px-2.5 py-1.5 text-[11px] font-semibold text-indigo-700 hover:bg-indigo-100"
                            >
                              <Download className="h-3.5 w-3.5" /> Tải xuống
                            </button>
                          )}
                          <button
                            type="button"
                            onClick={() => beginUpload(row.studentId)}
                            disabled={isUploading || isConfirmed}
                            title={isConfirmed ? 'Hợp đồng đã được sinh viên xác nhận' : undefined}
                            className="inline-flex items-center gap-1 rounded-lg bg-indigo-600 px-2.5 py-1.5 text-[11px] font-semibold text-white hover:bg-indigo-700 disabled:cursor-not-allowed disabled:opacity-50"
                          >
                            {isUploading ? <LoaderCircle className="h-3.5 w-3.5 animate-spin" /> : <UploadCloud className="h-3.5 w-3.5" />}
                            {row.hasFile ? 'Thay thế' : 'Tải lên'}
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </DashboardLayout>
  );
};

export default ContractManagementView;
