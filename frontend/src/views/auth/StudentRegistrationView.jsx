import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { ArrowLeft, CheckCircle2, ClipboardCheck, GraduationCap, Loader2, UserPlus } from 'lucide-react';
import studentRegistrationService from '../../services/studentRegistrationService';

const blankRegistration = {
  email: '',
  password: '',
  studentCode: '',
  fullName: '',
  phoneNumber: '',
  university: '',
  major: '',
  programId: '',
};

const statusStyles = {
  PENDING: 'border-amber-300 bg-amber-50 text-amber-800',
  PENDING_APPROVAL: 'border-amber-300 bg-amber-50 text-amber-800',
  APPROVED: 'border-emerald-300 bg-emerald-50 text-emerald-800',
  REJECTED: 'border-rose-300 bg-rose-50 text-rose-800',
};

const statusLabels = {
  PENDING: 'Đang chờ duyệt',
  PENDING_APPROVAL: 'Đang chờ duyệt',
  APPROVED: 'Đã được duyệt',
  ACTIVE: 'Đang hoạt động',
  REJECTED: 'Đã từ chối',
  INACTIVE: 'Không hoạt động',
  LOCKED: 'Đã khóa',
};

export const StudentRegistrationView = () => {
  const [mode, setMode] = useState('register');
  const [registration, setRegistration] = useState(blankRegistration);
  const [credentials, setCredentials] = useState({ email: '', password: '' });
  const [result, setResult] = useState(null);
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [programs, setPrograms] = useState([]);
  const [documents, setDocuments] = useState({ cv: null, internshipLetter: null });
  const [isLoadingPrograms, setIsLoadingPrograms] = useState(true);

  useEffect(() => {
    let isActive = true;
    studentRegistrationService.getPrograms()
      .then((items) => { if (isActive) setPrograms(Array.isArray(items) ? items : []); })
      .catch((requestError) => { if (isActive) setError(requestError.message); })
      .finally(() => { if (isActive) setIsLoadingPrograms(false); });
    return () => { isActive = false; };
  }, []);

  const switchMode = (nextMode) => {
    setMode(nextMode);
    setResult(null);
    setError('');
  };

  const handleRegister = async (event) => {
    event.preventDefault();
    setError('');
    setResult(null);
    setIsSubmitting(true);
    try {
      const created = await studentRegistrationService.register(registration, documents);
      setResult(created);
      setCredentials({ email: registration.email, password: registration.password });
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCheckStatus = async (event) => {
    event.preventDefault();
    setError('');
    setResult(null);
    setIsSubmitting(true);
    try {
      setResult(await studentRegistrationService.getOwnStatus(credentials));
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <main className="min-h-screen bg-slate-950 px-4 py-10 text-slate-900">
      <div className="mx-auto max-w-3xl overflow-hidden rounded-3xl border border-slate-800 bg-white shadow-2xl">
        <header className="bg-gradient-to-r from-slate-950 via-indigo-950 to-slate-900 px-6 py-7 text-white sm:px-9">
          <Link to="/login" className="mb-6 inline-flex items-center gap-2 text-sm text-indigo-200 hover:text-white">
            <ArrowLeft className="h-4 w-4" /> Quay lại đăng nhập
          </Link>
          <div className="flex items-center gap-4">
            <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-indigo-500/20 text-indigo-200 ring-1 ring-indigo-300/30">
              <GraduationCap className="h-6 w-6" />
            </div>
            <div>
              <h1 className="text-2xl font-bold">Đăng ký thực tập sinh</h1>
              <p className="mt-1 text-sm text-slate-300">Hồ sơ chỉ được kích hoạt sau khi HR xét duyệt.</p>
            </div>
          </div>
        </header>

        <div className="p-6 sm:p-9">
          <div className="mb-7 flex rounded-xl bg-slate-100 p-1">
            <button
              type="button"
              onClick={() => switchMode('register')}
              className={`flex flex-1 items-center justify-center gap-2 rounded-lg px-3 py-2.5 text-sm font-semibold transition ${mode === 'register' ? 'bg-white text-indigo-700 shadow-sm' : 'text-slate-500 hover:text-slate-800'}`}
            >
              <UserPlus className="h-4 w-4" /> Tạo hồ sơ
            </button>
            <button
              type="button"
              onClick={() => switchMode('status')}
              className={`flex flex-1 items-center justify-center gap-2 rounded-lg px-3 py-2.5 text-sm font-semibold transition ${mode === 'status' ? 'bg-white text-indigo-700 shadow-sm' : 'text-slate-500 hover:text-slate-800'}`}
            >
              <ClipboardCheck className="h-4 w-4" /> Kiểm tra trạng thái
            </button>
          </div>

          {mode === 'register' ? (
            <form onSubmit={handleRegister} className="grid gap-4 sm:grid-cols-2">
              <Field label="Họ và tên" required value={registration.fullName} onChange={(value) => setRegistration({ ...registration, fullName: value })} />
              <Field label="Mã sinh viên" required value={registration.studentCode} onChange={(value) => setRegistration({ ...registration, studentCode: value })} />
              <Field label="Email" type="email" required value={registration.email} onChange={(value) => setRegistration({ ...registration, email: value })} />
              <Field label="Mật khẩu" type="password" minLength={6} required value={registration.password} onChange={(value) => setRegistration({ ...registration, password: value })} />
              <Field label="Số điện thoại" value={registration.phoneNumber} onChange={(value) => setRegistration({ ...registration, phoneNumber: value })} />
              <Field label="Trường đại học" required value={registration.university} onChange={(value) => setRegistration({ ...registration, university: value })} />
              <Field label="Chuyên ngành" required value={registration.major} onChange={(value) => setRegistration({ ...registration, major: value })} />
              <label className="block space-y-1.5 sm:col-span-2">
                <span className="text-xs font-bold uppercase tracking-wide text-slate-600">Chương trình thực tập<span className="ml-1 text-rose-500">*</span></span>
                <select value={registration.programId} onChange={(event) => setRegistration({ ...registration, programId: event.target.value })} required disabled={isLoadingPrograms || programs.length === 0} className="w-full rounded-xl border border-slate-300 bg-white px-3.5 py-2.5 text-sm outline-none focus:border-indigo-500 focus:ring-4 focus:ring-indigo-100">
                  <option value="">{isLoadingPrograms ? 'Đang tải chương trình...' : 'Chọn chương trình thực tập'}</option>
                  {programs.map((program) => <option key={program.id} value={program.id}>{program.name} · {program.departmentName}</option>)}
                </select>
                {programs.length === 0 && !isLoadingPrograms && <span className="block text-xs text-amber-700">Hiện chưa có chương trình để đăng ký. Vui lòng liên hệ HR.</span>}
              </label>
              <p className="text-xs leading-5 text-slate-500 sm:col-span-2">
                CV và đơn xin thực tập sẽ được gửi cho HR xét duyệt, đồng thời lưu vào mục Hồ sơ &amp; tài liệu trong tài khoản sinh viên sau khi hồ sơ được duyệt. Bạn không cần tải lại các tệp này.
              </p>
              <FileField label="CV" required value={documents.cv} onChange={(file) => setDocuments((current) => ({ ...current, cv: file }))} />
              <FileField label="Đơn xin thực tập" required value={documents.internshipLetter} onChange={(file) => setDocuments((current) => ({ ...current, internshipLetter: file }))} />
              <button
                type="submit"
                disabled={isSubmitting || isLoadingPrograms || programs.length === 0}
                className="mt-2 inline-flex items-center justify-center gap-2 rounded-xl bg-indigo-600 px-4 py-3 text-sm font-semibold text-white shadow-lg shadow-indigo-600/20 transition hover:bg-indigo-500 disabled:cursor-wait disabled:opacity-60 sm:col-span-2"
              >
                {isSubmitting ? <Loader2 className="h-4 w-4 animate-spin" /> : <UserPlus className="h-4 w-4" />}
                Gửi hồ sơ xét duyệt
              </button>
            </form>
          ) : (
            <form onSubmit={handleCheckStatus} className="space-y-4">
              <Field label="Email đăng ký" type="email" required value={credentials.email} onChange={(value) => setCredentials({ ...credentials, email: value })} />
              <Field label="Mật khẩu" type="password" required value={credentials.password} onChange={(value) => setCredentials({ ...credentials, password: value })} />
              <p className="text-xs leading-5 text-slate-500">Cần nhập đúng email và mật khẩu của hồ sơ để chỉ xem được trạng thái của chính bạn.</p>
              <button
                type="submit"
                disabled={isSubmitting}
                className="inline-flex w-full items-center justify-center gap-2 rounded-xl bg-indigo-600 px-4 py-3 text-sm font-semibold text-white transition hover:bg-indigo-500 disabled:cursor-wait disabled:opacity-60"
              >
                {isSubmitting ? <Loader2 className="h-4 w-4 animate-spin" /> : <ClipboardCheck className="h-4 w-4" />}
                Tra cứu trạng thái
              </button>
            </form>
          )}

          {error && <p role="alert" className="mt-5 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{error}</p>}

          {result && (
            <section aria-live="polite" className="mt-6 rounded-2xl border border-slate-200 bg-slate-50 p-5">
              <div className="flex items-start gap-3">
                <CheckCircle2 className="mt-0.5 h-5 w-5 shrink-0 text-indigo-600" />
                <div className="min-w-0 flex-1">
                  <div className="flex flex-wrap items-center justify-between gap-3">
                    <h2 className="font-bold text-slate-900">{result.fullName || 'Hồ sơ sinh viên'}</h2>
                    <span className={`rounded-full border px-3 py-1 text-xs font-bold ${statusStyles[result.status] || 'border-slate-300 bg-white text-slate-700'}`}>
                      {statusLabels[result.status] || 'Không xác định'}
                    </span>
                  </div>
                  <p className="mt-2 text-sm text-slate-600">{result.message}</p>
                  {result.status === 'APPROVED' && (
                    <Link to="/login" className="mt-4 inline-flex text-sm font-semibold text-indigo-700 hover:text-indigo-900">Đăng nhập hệ thống</Link>
                  )}
                </div>
              </div>
            </section>
          )}
        </div>
      </div>
    </main>
  );
};

const Field = ({ label, value, onChange, type = 'text', required = false, minLength }) => (
  <label className="block space-y-1.5">
    <span className="text-xs font-bold uppercase tracking-wide text-slate-600">{label}{required && <span className="ml-1 text-rose-500">*</span>}</span>
    <input
      type={type}
      value={value}
      onChange={(event) => onChange(event.target.value)}
      required={required}
      minLength={minLength}
      className="w-full rounded-xl border border-slate-300 bg-white px-3.5 py-2.5 text-sm outline-none transition placeholder:text-slate-400 focus:border-indigo-500 focus:ring-4 focus:ring-indigo-100"
    />
  </label>
);

const FileField = ({ label, value, onChange, required = false }) => (
  <label className="block space-y-1.5">
    <span className="text-xs font-bold uppercase tracking-wide text-slate-600">{label}{required && <span className="ml-1 text-rose-500">*</span>}</span>
    <input type="file" accept=".pdf,.doc,.docx" required={required} onChange={(event) => onChange(event.target.files?.[0] || null)} className="w-full rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm text-slate-600 file:mr-3 file:rounded-lg file:border-0 file:bg-indigo-50 file:px-3 file:py-1.5 file:text-xs file:font-semibold file:text-indigo-700" />
    <span className="block truncate text-xs text-slate-500">{value?.name || 'PDF, DOC hoặc DOCX · tối đa 10 MB'}</span>
  </label>
);

export default StudentRegistrationView;
