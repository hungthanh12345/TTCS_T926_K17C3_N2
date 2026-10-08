import React, { useEffect, useState } from 'react';
import { ClipboardCheck, Loader2, RefreshCw, Save, Send } from 'lucide-react';
import toast from 'react-hot-toast';
import internshipEvaluationService from '../../services/internshipEvaluationService';

const emptyForm = { skillsScore: '', attitudeScore: '', comments: '' };
const toForm = (evaluation) => evaluation ? ({
  skillsScore: String(evaluation.skillsScore),
  attitudeScore: String(evaluation.attitudeScore),
  comments: evaluation.comments || '',
}) : emptyForm;

const formatDate = (value) => value ? new Date(value).toLocaleDateString('vi-VN') : '—';

export const MentorEvaluationPanel = () => {
  const [students, setStudents] = useState([]);
  const [selectedStudentId, setSelectedStudentId] = useState(null);
  const [form, setForm] = useState(emptyForm);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [loadError, setLoadError] = useState('');
  const [formError, setFormError] = useState('');

  useEffect(() => {
    let isActive = true;
    internshipEvaluationService.getAssignedStudents()
      .then((items) => {
        if (!isActive) return;
        setStudents(items);
        setSelectedStudentId(items[0]?.studentId ?? null);
        setForm(toForm(items[0]?.evaluation));
      })
      .catch((error) => { if (isActive) setLoadError(error.message || 'Không thể tải danh sách sinh viên.'); })
      .finally(() => { if (isActive) setIsLoading(false); });
    return () => { isActive = false; };
  }, []);

  const refreshStudents = async (preferredStudentId = selectedStudentId) => {
    setIsLoading(true);
    setLoadError('');
    try {
      const items = await internshipEvaluationService.getAssignedStudents();
      setStudents(items);
      const selected = items.find((item) => item.studentId === preferredStudentId) || items[0] || null;
      setSelectedStudentId(selected?.studentId ?? null);
      setForm(toForm(selected?.evaluation));
    } catch (error) {
      setLoadError(error.message || 'Không thể tải danh sách sinh viên.');
    } finally {
      setIsLoading(false);
    }
  };

  const selectedStudent = students.find((student) => student.studentId === selectedStudentId) || null;
  const evaluation = selectedStudent?.evaluation || null;
  const canEdit = !evaluation || evaluation.canEdit;

  const selectStudent = (student) => {
    setSelectedStudentId(student.studentId);
    setForm(toForm(student.evaluation));
    setFormError('');
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    setFormError('');
    if (!selectedStudent || !canEdit) return;
    const skillsScore = Number(form.skillsScore);
    const attitudeScore = Number(form.attitudeScore);
    if (!Number.isInteger(skillsScore) || skillsScore < 1 || skillsScore > 10 ||
        !Number.isInteger(attitudeScore) || attitudeScore < 1 || attitudeScore > 10) {
      setFormError('Điểm kỹ năng và thái độ phải là số nguyên từ 1 đến 10.');
      return;
    }
    if (form.comments.trim().length < 2) {
      setFormError('Nhận xét cần có ít nhất 2 ký tự.');
      return;
    }

    setIsSaving(true);
    try {
      const payload = { skillsScore, attitudeScore, comments: form.comments.trim() };
      if (evaluation) {
        await internshipEvaluationService.update(evaluation.id, payload);
        toast.success('Đã cập nhật đánh giá thực tập.');
      } else {
        await internshipEvaluationService.create(selectedStudent.studentId, payload);
        toast.success('Đã gửi đánh giá thực tập.');
      }
      await refreshStudents(selectedStudent.studentId);
    } catch (error) {
      setFormError(error.message || 'Không thể lưu đánh giá.');
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <section className="space-y-5 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h3 className="flex items-center gap-2 text-base font-bold text-slate-900"><ClipboardCheck className="h-5 w-5 text-indigo-600" /> Đánh giá thực tập sinh</h3>
          <p className="mt-1 text-xs text-slate-500">Chấm kỹ năng và thái độ cho sinh viên được phân công.</p>
        </div>
        <button type="button" onClick={() => void refreshStudents()} disabled={isLoading} className="inline-flex items-center gap-2 rounded-lg border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-600 hover:bg-slate-50 disabled:opacity-50">
          <RefreshCw className={`h-3.5 w-3.5 ${isLoading ? 'animate-spin' : ''}`} /> Làm mới
        </button>
      </div>

      {isLoading ? <div role="status" className="flex items-center gap-2 py-6 text-sm text-slate-500"><Loader2 className="h-4 w-4 animate-spin" /> Đang tải sinh viên...</div> : loadError ? (
        <div role="alert" className="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800">{loadError}</div>
      ) : students.length === 0 ? (
        <p role="status" className="rounded-xl border border-dashed border-slate-300 px-4 py-8 text-center text-sm text-slate-500">Bạn chưa có sinh viên đang hoạt động được phân công.</p>
      ) : (
        <div className="grid gap-4 lg:grid-cols-[minmax(0,0.8fr)_minmax(0,1.2fr)]">
          <div className="space-y-2">
            {students.map((student) => (
              <button key={student.studentId} type="button" onClick={() => selectStudent(student)} className={`w-full rounded-xl border p-4 text-left ${selectedStudentId === student.studentId ? 'border-indigo-300 bg-indigo-50' : 'border-slate-200 hover:bg-slate-50'}`}>
                <div className="flex items-start justify-between gap-2">
                  <div><p className="text-sm font-bold text-slate-900">{student.fullName}</p><p className="mt-0.5 text-xs text-slate-500">{student.studentCode} · {student.major}</p></div>
                  <span className={`rounded-full px-2 py-1 text-[10px] font-bold ${student.evaluation ? 'bg-emerald-50 text-emerald-700' : 'bg-amber-50 text-amber-700'}`}>{student.evaluation ? 'Đã đánh giá' : 'Chờ đánh giá'}</span>
                </div>
              </button>
            ))}
          </div>

          {selectedStudent && (
            <div className="space-y-4 rounded-xl border border-slate-200 p-4 sm:p-5">
              <div>
                <p className="text-xs font-semibold text-indigo-700">{selectedStudent.studentCode} · {selectedStudent.university}</p>
                <h4 className="mt-1 text-sm font-bold text-slate-900">{selectedStudent.fullName}</h4>
              </div>
              {evaluation && !evaluation.canEdit && (
                <div role="status" className="rounded-lg border border-amber-200 bg-amber-50 p-3 text-xs text-amber-800">
                  Đánh giá hiện tại do {evaluation.mentorName} tạo. Bạn có thể xem nhưng chỉ người đánh giá ban đầu mới được sửa.
                </div>
              )}
              <form onSubmit={handleSubmit} className="space-y-4">
                <div className="grid gap-3 sm:grid-cols-2">
                  <label className="space-y-1 text-xs font-semibold text-slate-600">
                    <span>Điểm kỹ năng (1–10)</span>
                    <input type="number" min="1" max="10" step="1" required disabled={!canEdit} value={form.skillsScore} onChange={(event) => setForm({ ...form, skillsScore: event.target.value })} className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm outline-none focus:border-indigo-500 focus:ring-4 focus:ring-indigo-100 disabled:bg-slate-50 disabled:text-slate-500" />
                  </label>
                  <label className="space-y-1 text-xs font-semibold text-slate-600">
                    <span>Điểm thái độ (1–10)</span>
                    <input type="number" min="1" max="10" step="1" required disabled={!canEdit} value={form.attitudeScore} onChange={(event) => setForm({ ...form, attitudeScore: event.target.value })} className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm outline-none focus:border-indigo-500 focus:ring-4 focus:ring-indigo-100 disabled:bg-slate-50 disabled:text-slate-500" />
                  </label>
                </div>
                <label className="block space-y-1 text-xs font-semibold text-slate-600">
                  <span>Nhận xét</span>
                  <textarea required minLength={2} maxLength={4000} rows={5} disabled={!canEdit} value={form.comments} onChange={(event) => setForm({ ...form, comments: event.target.value })} className="w-full resize-y rounded-lg border border-slate-300 px-3 py-2.5 text-sm font-normal outline-none focus:border-indigo-500 focus:ring-4 focus:ring-indigo-100 disabled:bg-slate-50 disabled:text-slate-500" />
                </label>
                {evaluation && <p className="text-xs text-slate-500">Đánh giá gần nhất: {formatDate(evaluation.evaluatedAt)} · Điểm tổng hợp {evaluation.overallScore}/10</p>}
                {formError && <p role="alert" className="text-sm text-rose-700">{formError}</p>}
                {canEdit && <div className="flex justify-end">
                  <button type="submit" disabled={isSaving} className="inline-flex items-center gap-2 rounded-lg bg-indigo-600 px-4 py-2.5 text-xs font-bold text-white hover:bg-indigo-500 disabled:opacity-50">
                    {isSaving ? <Loader2 className="h-4 w-4 animate-spin" /> : evaluation ? <Save className="h-4 w-4" /> : <Send className="h-4 w-4" />}
                    {evaluation ? 'Lưu đánh giá' : 'Gửi đánh giá'}
                  </button>
                </div>}
              </form>
            </div>
          )}
        </div>
      )}
    </section>
  );
};

export default MentorEvaluationPanel;
