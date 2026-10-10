import React, { useEffect, useMemo, useState } from 'react';
import { AlertCircle, CheckCircle2, Loader2, Search, UserPlus } from 'lucide-react';
import Modal from '../common/Modal';
import studentService from '../../services/studentService';
import US11 from '../../services/sprint2/US11';
import { messageOf } from '../../services/sprint2/common';

const statusLabels = {
  ACTIVE: 'Đã duyệt',
  INACTIVE: 'Không hoạt động',
  LOCKED: 'Đã khóa',
  PENDING_APPROVAL: 'Chờ duyệt',
  REJECTED: 'Đã từ chối',
};

const formatDate = (value) => {
  if (!value) return 'Chưa thiết lập';
  const date = new Date(`${value}T00:00:00`);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleDateString('vi-VN');
};

const AssignStudentToProgramModal = ({ program, isOpen, onClose }) => {
  const [students, setStudents] = useState([]);
  const [search, setSearch] = useState('');
  const [selectedStudent, setSelectedStudent] = useState(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [confirmTransfer, setConfirmTransfer] = useState(false);
  const [retryKey, setRetryKey] = useState(0);

  useEffect(() => {
    let active = true;

    studentService.getStudents()
      .then((items) => {
        if (active) setStudents(Array.isArray(items) ? items : []);
      })
      .catch((requestError) => {
        if (active) setError(messageOf(requestError, 'Không thể tải danh sách sinh viên.'));
      })
      .finally(() => {
        if (active) setLoading(false);
      });

    return () => { active = false; };
  }, [retryKey]);

  const eligibleStudents = useMemo(
    () => students.filter((student) => !['PENDING_APPROVAL', 'REJECTED'].includes(student.accountStatus)),
    [students],
  );

  const filteredStudents = useMemo(() => {
    const query = search.trim().toLocaleLowerCase('vi');
    if (!query) return eligibleStudents;
    return eligibleStudents.filter((student) => [student.fullName, student.studentCode, student.email]
      .some((value) => value?.toLocaleLowerCase('vi').includes(query)));
  }, [eligibleStudents, search]);

  const transferRequired = Boolean(
    selectedStudent?.programId && selectedStudent.programId !== program?.id,
  );
  const alreadyAssigned = selectedStudent?.programId === program?.id;

  const closeModal = () => {
    if (!submitting) onClose();
  };

  const retryLoadingStudents = () => {
    setError('');
    setLoading(true);
    setRetryKey((current) => current + 1);
  };

  const assignStudent = async () => {
    if (!program || !selectedStudent || submitting || alreadyAssigned) return;
    if (transferRequired && !confirmTransfer) return;

    setSubmitting(true);
    setError('');
    setSuccess('');
    try {
      const result = await US11.assignStudentProgram(selectedStudent.id, program.id);
      const updatedStudent = {
        ...selectedStudent,
        programId: result.programId,
        programName: result.programName,
      };
      setStudents((items) => items.map((item) => (
        item.id === updatedStudent.id ? updatedStudent : item
      )));
      setSelectedStudent(updatedStudent);
      setConfirmTransfer(false);
      setSuccess(`Đã gán ${updatedStudent.fullName} vào chương trình ${program.name}.`);
    } catch (requestError) {
      setError(messageOf(requestError, 'Không thể gán sinh viên vào chương trình.'));
    } finally {
      setSubmitting(false);
    }
  };

  if (!program) return null;

  return (
    <Modal
      isOpen={isOpen}
      onClose={closeModal}
      title="Gán sinh viên vào chương trình thực tập"
      subtitle="Tìm sinh viên trong danh sách hồ sơ HR được phép quản lý."
      icon={UserPlus}
      maxWidth="max-w-3xl"
    >
      <div className="space-y-5">
        <section className="rounded-xl border border-slate-200 bg-slate-50 p-4" aria-label="Thông tin chương trình">
          <h4 className="text-xs font-bold uppercase tracking-wide text-slate-500">Thông tin chương trình</h4>
          <dl className="mt-3 grid gap-3 text-sm sm:grid-cols-2">
            <div><dt className="text-xs text-slate-500">Tên chương trình</dt><dd className="mt-1 font-semibold text-slate-900">{program.name}</dd></div>
            <div><dt className="text-xs text-slate-500">Phòng ban</dt><dd className="mt-1 font-semibold text-slate-900">{program.departmentName}</dd></div>
            <div><dt className="text-xs text-slate-500">Ngày bắt đầu</dt><dd className="mt-1 text-slate-700">{formatDate(program.startDate)}</dd></div>
            <div><dt className="text-xs text-slate-500">Ngày kết thúc</dt><dd className="mt-1 text-slate-700">{formatDate(program.endDate)}</dd></div>
          </dl>
        </section>

        <section className="space-y-3">
          <label className="block text-sm font-semibold text-slate-800" htmlFor="program-student-search">Tìm kiếm sinh viên</label>
          <div className="relative">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
            <input
              id="program-student-search"
              type="search"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Họ tên, mã sinh viên hoặc email"
              className="w-full rounded-xl border border-slate-200 bg-white py-2.5 pl-10 pr-3 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
              disabled={loading || submitting}
            />
          </div>

          {loading && (
            <div className="flex items-center justify-center gap-2 rounded-xl border border-slate-100 py-8 text-sm text-slate-500" role="status">
              <Loader2 className="h-4 w-4 animate-spin" /> Đang tải sinh viên...
            </div>
          )}

          {!loading && error && (
            <div className="rounded-xl border border-rose-200 bg-rose-50 p-3 text-sm text-rose-800" role="alert">
              <div className="flex items-start gap-2"><AlertCircle className="mt-0.5 h-4 w-4 shrink-0" /><p>{error}</p></div>
              <button
                type="button"
                onClick={retryLoadingStudents}
                className="mt-2 font-semibold underline underline-offset-2"
              >
                Tải lại danh sách
              </button>
            </div>
          )}

          {!loading && !error && eligibleStudents.length === 0 && (
            <p className="rounded-xl border border-slate-100 py-8 text-center text-sm text-slate-500">Không có sinh viên đủ điều kiện trong danh sách HR hiện tại.</p>
          )}

          {!loading && !error && eligibleStudents.length > 0 && filteredStudents.length === 0 && (
            <p className="rounded-xl border border-slate-100 py-8 text-center text-sm text-slate-500">Không tìm thấy sinh viên phù hợp.</p>
          )}

          {!loading && !error && filteredStudents.length > 0 && (
            <div className="max-h-64 space-y-2 overflow-y-auto rounded-xl" role="listbox" aria-label="Danh sách sinh viên">
              {filteredStudents.map((student) => (
                <button
                  key={student.id}
                  type="button"
                  role="option"
                  aria-selected={selectedStudent?.id === student.id}
                  onClick={() => { setSelectedStudent(student); setConfirmTransfer(false); setSuccess(''); }}
                  disabled={submitting}
                  className={`w-full rounded-xl border p-3 text-left transition-colors disabled:opacity-60 ${selectedStudent?.id === student.id ? 'border-indigo-400 bg-indigo-50' : 'border-slate-200 bg-white hover:border-indigo-200 hover:bg-slate-50'}`}
                >
                  <span className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
                    <span className="min-w-0">
                      <span className="block truncate text-sm font-semibold text-slate-900">{student.fullName}</span>
                      <span className="mt-1 block text-xs text-slate-500">{student.studentCode} · {student.email || 'Chưa liên kết email'}</span>
                    </span>
                    <span className="shrink-0 text-xs text-slate-600">{statusLabels[student.accountStatus] || 'Chưa liên kết tài khoản'}</span>
                  </span>
                  <span className="mt-2 block text-xs text-slate-600">
                    Chương trình hiện tại: <strong>{student.programName || 'Chưa được gán'}</strong>
                  </span>
                </button>
              ))}
            </div>
          )}
        </section>

        {selectedStudent && alreadyAssigned && (
          <p className="rounded-xl border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800" role="status">
            Sinh viên này đã thuộc chương trình đang chọn.
          </p>
        )}

        {selectedStudent && transferRequired && !alreadyAssigned && (
          <label className="flex items-start gap-2 rounded-xl border border-amber-200 bg-amber-50 p-3 text-sm text-amber-900">
            <input
              type="checkbox"
              checked={confirmTransfer}
              onChange={(event) => setConfirmTransfer(event.target.checked)}
              disabled={submitting}
              className="mt-0.5 rounded border-amber-300 text-indigo-600 focus:ring-indigo-500"
            />
            <span>Chương trình hiện tại là <strong>{selectedStudent.programName}</strong>. Tôi xác nhận chuyển sinh viên sang <strong>{program.name}</strong>.</span>
          </label>
        )}

        {success && (
          <p className="flex items-start gap-2 rounded-xl border border-emerald-200 bg-emerald-50 p-3 text-sm text-emerald-800" role="status">
            <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0" /> {success}
          </p>
        )}

        <div className="flex flex-col-reverse gap-2 border-t border-slate-100 pt-4 sm:flex-row sm:justify-end">
          <button type="button" onClick={closeModal} disabled={submitting} className="rounded-xl border border-slate-200 px-4 py-2.5 text-sm font-semibold text-slate-700 hover:bg-slate-50 disabled:opacity-50">Đóng</button>
          <button
            type="button"
            onClick={() => void assignStudent()}
            disabled={!selectedStudent || alreadyAssigned || submitting || (transferRequired && !confirmTransfer)}
            className="inline-flex items-center justify-center gap-2 rounded-xl bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-indigo-500 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {submitting ? <Loader2 className="h-4 w-4 animate-spin" /> : <UserPlus className="h-4 w-4" />}
            {submitting ? 'Đang gán...' : transferRequired ? 'Xác nhận chuyển chương trình' : 'Xác nhận gán'}
          </button>
        </div>
      </div>
    </Modal>
  );
};

export default AssignStudentToProgramModal;
