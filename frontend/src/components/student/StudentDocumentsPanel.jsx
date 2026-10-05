import React, { useEffect, useRef, useState } from 'react';
import toast from 'react-hot-toast';
import {
  Download,
  FileText,
  LoaderCircle,
  RefreshCw,
  Trash2,
  UploadCloud,
  X,
} from 'lucide-react';
import studentDocumentService from '../../services/studentDocumentService';

const MAX_FILE_SIZE = 10 * 1024 * 1024;
const ACCEPTED_EXTENSIONS = ['.pdf', '.doc', '.docx'];

const getErrorMessage = (error) =>
  error.response?.data?.message || error.message || 'Không thể hoàn thành thao tác tài liệu.';

const validateFile = (file) => {
  if (!file) return 'Vui lòng chọn tệp.';
  const extension = `.${file.name.split('.').pop()?.toLowerCase()}`;
  if (!ACCEPTED_EXTENSIONS.includes(extension)) return 'Chỉ chấp nhận tệp PDF, DOC hoặc DOCX.';
  if (file.size === 0) return 'Tệp không được để trống.';
  if (file.size > MAX_FILE_SIZE) return 'Dung lượng tệp tối đa là 10 MB.';
  return null;
};

const formatSize = (size) => {
  if (size < 1024) return `${size} B`;
  if (size < 1024 * 1024) return `${(size / 1024).toFixed(0)} KB`;
  return `${(size / (1024 * 1024)).toFixed(1)} MB`;
};

const documentTypeLabel = (type) =>
  type === 'CV' ? 'CV' : 'Đơn xin thực tập';

export const StudentDocumentsPanel = () => {
  const [documents, setDocuments] = useState([]);
  const [documentType, setDocumentType] = useState('CV');
  const [selectedFile, setSelectedFile] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isUploading, setIsUploading] = useState(false);
  const [replacingId, setReplacingId] = useState(null);
  const [deletingId, setDeletingId] = useState(null);
  const [loadError, setLoadError] = useState('');
  const fileInputRef = useRef(null);
  const replacementInputRef = useRef(null);
  const replacementTargetRef = useRef(null);
  const existingTypeDocument = documents.find((document) => document.documentType === documentType);

  const loadDocuments = async () => {
    setIsLoading(true);
    setLoadError('');
    try {
      setDocuments(await studentDocumentService.getMyDocuments());
    } catch (error) {
      setLoadError(getErrorMessage(error));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    let isActive = true;
    studentDocumentService.getMyDocuments()
      .then((result) => {
        if (isActive) setDocuments(result);
      })
      .catch((error) => {
        if (isActive) setLoadError(getErrorMessage(error));
      })
      .finally(() => {
        if (isActive) setIsLoading(false);
      });

    return () => {
      isActive = false;
    };
  }, []);

  const chooseFile = (file) => {
    const validationError = validateFile(file);
    if (validationError) {
      setSelectedFile(null);
      toast.error(validationError);
      return;
    }
    setSelectedFile(file);
  };

  const handleFileInput = (event) => {
    chooseFile(event.target.files?.[0] || null);
    event.target.value = '';
  };

  const handleUpload = async (event) => {
    event.preventDefault();
    const validationError = validateFile(selectedFile);
    if (validationError) {
      toast.error(validationError);
      return;
    }

    setIsUploading(true);
    try {
      if (existingTypeDocument) {
        await studentDocumentService.replace(existingTypeDocument.id, selectedFile);
      } else {
        await studentDocumentService.upload(documentType, selectedFile);
      }
      setSelectedFile(null);
      toast.success(existingTypeDocument ? 'Đã cập nhật tài liệu hiện có.' : 'Tải tài liệu lên thành công.');
      await loadDocuments();
    } catch (error) {
      toast.error(getErrorMessage(error));
    } finally {
      setIsUploading(false);
    }
  };

  const beginReplace = (documentId) => {
    replacementTargetRef.current = documentId;
    replacementInputRef.current?.click();
  };

  const handleReplacementInput = async (event) => {
    const file = event.target.files?.[0];
    const documentId = replacementTargetRef.current;
    event.target.value = '';
    replacementTargetRef.current = null;
    if (!file || !documentId) return;

    const validationError = validateFile(file);
    if (validationError) {
      toast.error(validationError);
      return;
    }

    setReplacingId(documentId);
    try {
      await studentDocumentService.replace(documentId, file);
      toast.success('Đã thay thế tài liệu.');
      await loadDocuments();
    } catch (error) {
      toast.error(getErrorMessage(error));
    } finally {
      setReplacingId(null);
    }
  };

  const handleDownload = async (document) => {
    try {
      const response = await studentDocumentService.download(document.id);
      const url = URL.createObjectURL(response.data);
      const anchor = window.document.createElement('a');
      anchor.href = url;
      anchor.download = document.originalFileName;
      anchor.click();
      window.setTimeout(() => URL.revokeObjectURL(url), 1000);
    } catch (error) {
      toast.error(getErrorMessage(error));
    }
  };

  const handleDelete = async (document) => {
    if (!window.confirm(`Xóa tài liệu “${document.originalFileName}”?`)) return;
    setDeletingId(document.id);
    try {
      await studentDocumentService.remove(document.id);
      setDocuments((current) => current.filter((item) => item.id !== document.id));
      toast.success('Đã xóa tài liệu.');
    } catch (error) {
      toast.error(getErrorMessage(error));
    } finally {
      setDeletingId(null);
    }
  };

  return (
    <section className="overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-xs">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 px-5 py-4 sm:px-6">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-xl border border-indigo-100 bg-indigo-50 text-indigo-600">
            <FileText className="h-5 w-5" />
          </div>
          <div>
            <h3 className="text-sm font-bold text-slate-900">Tài liệu thực tập</h3>
            <p className="mt-0.5 text-xs text-slate-500">CV và đơn đã nộp khi đăng ký sẽ hiển thị tại đây; bạn chỉ cần thay thế nếu muốn cập nhật.</p>
          </div>
        </div>
        <span className="rounded-full border border-slate-200 bg-slate-50 px-2.5 py-1 text-[11px] font-semibold text-slate-600">
          PDF, DOC, DOCX · tối đa 10 MB
        </span>
      </div>

      <div className="grid gap-6 p-5 sm:p-6 lg:grid-cols-[minmax(0,0.9fr)_minmax(0,1.1fr)]">
        <form
          className="space-y-4"
          onSubmit={handleUpload}
          onDragOver={(event) => event.preventDefault()}
          onDrop={(event) => {
            event.preventDefault();
            chooseFile(event.dataTransfer.files?.[0] || null);
          }}
        >
          <div>
            <label htmlFor="student-document-type" className="mb-1.5 block text-xs font-semibold text-slate-700">
              Loại tài liệu
            </label>
            <select
              id="student-document-type"
              value={documentType}
              onChange={(event) => setDocumentType(event.target.value)}
              className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-sm text-slate-800 outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
            >
              <option value="CV">CV</option>
              <option value="INTERNSHIP_LETTER">Đơn xin thực tập</option>
            </select>
          </div>

          <input
            ref={fileInputRef}
            type="file"
            accept=".pdf,.doc,.docx,application/pdf,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            onChange={handleFileInput}
            className="sr-only"
            aria-label="Chọn tài liệu để tải lên"
          />
          <button
            type="button"
            onClick={() => fileInputRef.current?.click()}
            className="flex min-h-36 w-full flex-col items-center justify-center rounded-2xl border-2 border-dashed border-indigo-200 bg-indigo-50/40 px-4 py-5 text-center transition hover:border-indigo-400 hover:bg-indigo-50"
          >
            <UploadCloud className="mb-2 h-7 w-7 text-indigo-500" />
            <span className="text-sm font-semibold text-slate-800">Kéo thả tệp vào đây</span>
            <span className="mt-1 text-xs text-slate-500">hoặc bấm để chọn tệp từ thiết bị</span>
          </button>

          {selectedFile && (
            <div className="flex items-center justify-between gap-3 rounded-xl border border-slate-200 bg-slate-50 px-3 py-2.5">
              <div className="flex min-w-0 items-center gap-2">
                <FileText className="h-4 w-4 shrink-0 text-indigo-500" />
                <span className="truncate text-xs font-medium text-slate-700">{selectedFile.name}</span>
                <span className="shrink-0 text-[11px] text-slate-400">{formatSize(selectedFile.size)}</span>
              </div>
              <button
                type="button"
                onClick={() => setSelectedFile(null)}
                className="rounded-lg p-1 text-slate-400 hover:bg-white hover:text-slate-700"
                aria-label="Bỏ chọn tệp"
              >
                <X className="h-4 w-4" />
              </button>
            </div>
          )}

          <button
            type="submit"
            disabled={isUploading || !selectedFile}
            className="inline-flex w-full items-center justify-center gap-2 rounded-xl bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-indigo-700 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {isUploading ? <LoaderCircle className="h-4 w-4 animate-spin" /> : <UploadCloud className="h-4 w-4" />}
            {isUploading ? 'Đang lưu…' : existingTypeDocument ? 'Cập nhật tài liệu hiện có' : 'Tải tài liệu lên'}
          </button>
        </form>

        <div>
          <div className="mb-3 flex items-center justify-between">
            <h4 className="text-xs font-bold uppercase tracking-wide text-slate-700">Tài liệu đã tải</h4>
            <button
              type="button"
              onClick={loadDocuments}
              disabled={isLoading}
              className="inline-flex items-center gap-1.5 rounded-lg px-2 py-1 text-xs font-medium text-slate-500 hover:bg-slate-100 hover:text-slate-800 disabled:opacity-50"
            >
              <RefreshCw className={`h-3.5 w-3.5 ${isLoading ? 'animate-spin' : ''}`} />
              Làm mới
            </button>
          </div>

          <input
            ref={replacementInputRef}
            type="file"
            accept=".pdf,.doc,.docx"
            onChange={handleReplacementInput}
            className="sr-only"
            aria-label="Chọn tệp thay thế"
          />

          {isLoading ? (
            <div className="flex min-h-36 items-center justify-center rounded-xl border border-slate-100 bg-slate-50 text-sm text-slate-500">
              <LoaderCircle className="mr-2 h-4 w-4 animate-spin" /> Đang tải danh sách…
            </div>
          ) : loadError ? (
            <div className="rounded-xl border border-rose-100 bg-rose-50 p-4 text-sm text-rose-700">
              <p>{loadError}</p>
              <button type="button" onClick={loadDocuments} className="mt-2 font-semibold underline">Thử lại</button>
            </div>
          ) : documents.length === 0 ? (
            <div className="flex min-h-36 flex-col items-center justify-center rounded-xl border border-dashed border-slate-200 bg-slate-50 px-4 text-center">
              <FileText className="mb-2 h-6 w-6 text-slate-300" />
              <p className="text-sm font-medium text-slate-600">Chưa có tài liệu</p>
              <p className="mt-1 text-xs text-slate-400">Tải CV hoặc đơn xin thực tập để hoàn thiện hồ sơ.</p>
            </div>
          ) : (
            <ul className="space-y-2.5">
              {documents.map((document) => (
                <li key={document.id} className="rounded-xl border border-slate-200 p-3 transition hover:border-indigo-200">
                  <div className="flex items-start gap-3">
                    <div className="mt-0.5 flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-indigo-50 text-indigo-600">
                      <FileText className="h-4 w-4" />
                    </div>
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-xs font-semibold text-slate-800" title={document.originalFileName}>
                        {document.originalFileName}
                      </p>
                      <p className="mt-1 text-[11px] text-slate-500">
                        {documentTypeLabel(document.documentType)} · {formatSize(document.sizeBytes)} ·{' '}
                        {new Date(document.uploadedAt).toLocaleDateString('vi-VN')}
                      </p>
                      <div className="mt-2.5 flex flex-wrap items-center gap-1.5">
                        <button type="button" onClick={() => handleDownload(document)} className="inline-flex items-center gap-1 rounded-lg bg-indigo-50 px-2 py-1.5 text-[11px] font-semibold text-indigo-700 hover:bg-indigo-100">
                          <Download className="h-3.5 w-3.5" /> Tải xuống
                        </button>
                        <button type="button" onClick={() => beginReplace(document.id)} disabled={replacingId === document.id} className="inline-flex items-center gap-1 rounded-lg bg-slate-100 px-2 py-1.5 text-[11px] font-semibold text-slate-700 hover:bg-slate-200 disabled:opacity-50">
                          {replacingId === document.id ? <LoaderCircle className="h-3.5 w-3.5 animate-spin" /> : <RefreshCw className="h-3.5 w-3.5" />}
                          Thay thế
                        </button>
                        <button type="button" onClick={() => handleDelete(document)} disabled={deletingId === document.id} className="inline-flex items-center gap-1 rounded-lg bg-rose-50 px-2 py-1.5 text-[11px] font-semibold text-rose-700 hover:bg-rose-100 disabled:opacity-50">
                          {deletingId === document.id ? <LoaderCircle className="h-3.5 w-3.5 animate-spin" /> : <Trash2 className="h-3.5 w-3.5" />}
                          Xóa
                        </button>
                      </div>
                    </div>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </section>
  );
};

export default StudentDocumentsPanel;
