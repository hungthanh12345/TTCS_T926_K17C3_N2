import React, { useCallback, useEffect, useState } from 'react';
import { ClipboardList, Loader2, Pencil, Plus, RefreshCw, Trash2, X } from 'lucide-react';
import toast from 'react-hot-toast';
import mentorTaskService from '../../services/mentorTaskService';

const emptyForm = { studentId: '', title: '', description: '', dueDate: '' };
const statusLabel = (status) => ({
  TO_DO: 'To Do',
  IN_PROGRESS: 'In Progress',
  DONE: 'Done',
}[status] || status);

export const MentorTaskManagement = () => {
  const [students, setStudents] = useState([]);
  const [tasks, setTasks] = useState([]);
  const [form, setForm] = useState(emptyForm);
  const [editingTaskId, setEditingTaskId] = useState(null);
  const [details, setDetails] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [busyTaskId, setBusyTaskId] = useState(null);

  const loadData = useCallback(async () => {
    try {
      const [assignedStudents, assignedTasks] = await Promise.all([
        mentorTaskService.getAssignedStudents(),
        mentorTaskService.getTasks(),
      ]);
      setStudents(assignedStudents);
      setTasks(assignedTasks);
    } catch (error) {
      toast.error(error.message || 'Không thể tải danh sách sinh viên và task.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => { void loadData(); }, [loadData]);

  const refreshData = async () => {
    setIsLoading(true);
    await loadData();
  };

  const startEdit = (task) => {
    setEditingTaskId(task.id);
    setForm({
      studentId: String(task.studentId),
      title: task.title,
      description: task.description || '',
      dueDate: task.dueDate || '',
    });
    setDetails(null);
  };

  const cancelEdit = () => {
    setEditingTaskId(null);
    setForm(emptyForm);
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (!form.title.trim() || (!editingTaskId && !form.studentId)) return;
    setIsSaving(true);
    try {
      const payload = {
        title: form.title.trim(),
        description: form.description.trim() || null,
        dueDate: form.dueDate || null,
      };
      if (editingTaskId) {
        await mentorTaskService.updateTask(editingTaskId, payload);
        toast.success('Đã cập nhật task.');
      } else {
        await mentorTaskService.createTask({ ...payload, studentId: Number(form.studentId) });
        toast.success('Đã giao task cho sinh viên.');
      }
      cancelEdit();
      await refreshData();
    } catch (error) {
      toast.error(error.message || 'Không thể lưu task.');
    } finally {
      setIsSaving(false);
    }
  };

  const showDetails = async (taskId) => {
    setBusyTaskId(taskId);
    try {
      setDetails(await mentorTaskService.getTask(taskId));
    } catch (error) {
      toast.error(error.message || 'Không thể tải chi tiết task.');
    } finally {
      setBusyTaskId(null);
    }
  };

  const removeTask = async (task) => {
    if (!window.confirm(`Xóa task “${task.title}”?`)) return;
    setBusyTaskId(task.id);
    try {
      await mentorTaskService.deleteTask(task.id);
      if (details?.id === task.id) setDetails(null);
      toast.success('Đã xóa task.');
      await refreshData();
    } catch (error) {
      toast.error(error.message || 'Không thể xóa task.');
    } finally {
      setBusyTaskId(null);
    }
  };

  return (
    <section className="space-y-5 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h3 className="flex items-center gap-2 text-base font-bold text-slate-900"><ClipboardList className="h-5 w-5 text-indigo-600" /> Giao và quản lý task</h3>
          <p className="mt-1 text-xs text-slate-500">Chỉ sinh viên được HR ghép với bạn mới xuất hiện ở đây. Trạng thái hiển thị tiến độ do sinh viên cập nhật.</p>
        </div>
        <button type="button" onClick={refreshData} disabled={isLoading} className="inline-flex items-center gap-2 rounded-lg border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-600 hover:bg-slate-50 disabled:opacity-50">
          <RefreshCw className={`h-3.5 w-3.5 ${isLoading ? 'animate-spin' : ''}`} /> Làm mới
        </button>
      </div>

      <form onSubmit={handleSubmit} className="grid gap-3 rounded-xl border border-indigo-100 bg-indigo-50/50 p-4 md:grid-cols-2">
        <div className="md:col-span-2 flex items-center justify-between">
          <h4 className="text-sm font-bold text-slate-800">{editingTaskId ? 'Chỉnh sửa task' : 'Tạo task mới'}</h4>
          {editingTaskId && <button type="button" onClick={cancelEdit} className="rounded-md p-1 text-slate-500 hover:bg-white" aria-label="Hủy chỉnh sửa"><X className="h-4 w-4" /></button>}
        </div>
        {!editingTaskId && (
          <label className="space-y-1 text-xs font-semibold text-slate-600">
            <span>Sinh viên nhận task</span>
            <select required value={form.studentId} onChange={(event) => setForm({ ...form, studentId: event.target.value })} className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-indigo-500 focus:ring-4 focus:ring-indigo-100">
              <option value="">Chọn sinh viên được phân công</option>
              {students.map((student) => <option key={student.studentId} value={student.studentId}>{student.fullName} · {student.studentCode}</option>)}
            </select>
          </label>
        )}
        {editingTaskId && <div className="rounded-lg bg-white px-3 py-2.5 text-xs text-slate-600">Người nhận: <strong>{tasks.find((task) => task.id === editingTaskId)?.studentName}</strong> (không thể chuyển task sang sinh viên khác)</div>}
        <label className="space-y-1 text-xs font-semibold text-slate-600">
          <span>Tên task</span>
          <input required minLength={2} maxLength={200} value={form.title} onChange={(event) => setForm({ ...form, title: event.target.value })} className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-indigo-500 focus:ring-4 focus:ring-indigo-100" />
        </label>
        <label className="space-y-1 text-xs font-semibold text-slate-600">
          <span>Deadline (không bắt buộc)</span>
          <input type="date" min={new Date().toISOString().slice(0, 10)} value={form.dueDate} onChange={(event) => setForm({ ...form, dueDate: event.target.value })} className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-indigo-500 focus:ring-4 focus:ring-indigo-100" />
        </label>
        <label className="space-y-1 text-xs font-semibold text-slate-600 md:col-span-2">
          <span>Mô tả</span>
          <textarea maxLength={2000} rows={3} value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} className="w-full resize-y rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-indigo-500 focus:ring-4 focus:ring-indigo-100" />
        </label>
        <div className="flex items-center justify-between gap-3 md:col-span-2">
          <span className="text-xs text-slate-500">Task được tạo với trạng thái ban đầu TO_DO.</span>
          <button type="submit" disabled={isSaving || (!editingTaskId && students.length === 0)} className="inline-flex items-center gap-2 rounded-lg bg-indigo-600 px-4 py-2.5 text-xs font-bold text-white hover:bg-indigo-500 disabled:cursor-not-allowed disabled:opacity-50">
            {isSaving ? <Loader2 className="h-4 w-4 animate-spin" /> : editingTaskId ? <Pencil className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
            {editingTaskId ? 'Lưu thay đổi' : 'Giao task'}
          </button>
        </div>
      </form>

      {details && (
        <article className="rounded-xl border border-indigo-200 bg-indigo-50/60 p-4">
          <div className="flex items-start justify-between gap-3">
            <div><p className="text-[11px] font-bold uppercase tracking-wide text-indigo-600">Chi tiết task · {statusLabel(details.status)}</p><h4 className="mt-1 font-bold text-slate-900">{details.title}</h4></div>
            <button type="button" onClick={() => setDetails(null)} className="rounded-md p-1 text-slate-500 hover:bg-white" aria-label="Đóng chi tiết"><X className="h-4 w-4" /></button>
          </div>
          <p className="mt-2 text-sm text-slate-700">{details.description || 'Không có mô tả.'}</p>
          <p className="mt-2 text-xs text-slate-500">{details.studentName} · Deadline: {details.dueDate || 'Chưa đặt'}</p>
        </article>
      )}

      <div>
        <h4 className="mb-3 text-sm font-bold text-slate-800">Task đã giao <span className="ml-1 rounded-full bg-slate-100 px-2 py-0.5 text-xs text-slate-600">{tasks.length}</span></h4>
        {isLoading ? <div className="flex items-center gap-2 py-6 text-sm text-slate-500"><Loader2 className="h-4 w-4 animate-spin" /> Đang tải...</div> : tasks.length === 0 ? (
          <p className="rounded-xl border border-dashed border-slate-300 px-4 py-8 text-center text-sm text-slate-500">Chưa có task nào được giao.</p>
        ) : (
          <div className="divide-y divide-slate-100 rounded-xl border border-slate-200">
            {tasks.map((task) => (
              <article key={task.id} className="flex flex-col gap-3 p-4 sm:flex-row sm:items-center sm:justify-between">
                <div className="min-w-0">
                  <div className="flex flex-wrap items-center gap-2"><h5 className="truncate text-sm font-bold text-slate-900">{task.title}</h5><span className="rounded-full bg-slate-100 px-2 py-0.5 text-[10px] font-bold text-slate-600">{statusLabel(task.status)}</span></div>
                  <p className="mt-1 truncate text-xs text-slate-500">{task.studentName} · {task.studentCode} · Deadline: {task.dueDate || 'Chưa đặt'}</p>
                  {task.description && <p className="mt-1 line-clamp-2 text-xs text-slate-600">{task.description}</p>}
                </div>
                <div className="flex shrink-0 items-center gap-1">
                  <button type="button" disabled={busyTaskId === task.id} onClick={() => showDetails(task.id)} className="rounded-lg px-2.5 py-2 text-xs font-semibold text-indigo-700 hover:bg-indigo-50 disabled:opacity-50">Chi tiết</button>
                  <button type="button" onClick={() => startEdit(task)} className="rounded-lg p-2 text-slate-500 hover:bg-slate-100 hover:text-indigo-700" title="Sửa task"><Pencil className="h-4 w-4" /></button>
                  <button type="button" disabled={busyTaskId === task.id} onClick={() => removeTask(task)} className="rounded-lg p-2 text-slate-500 hover:bg-rose-50 hover:text-rose-700 disabled:opacity-50" title="Xóa task"><Trash2 className="h-4 w-4" /></button>
                </div>
              </article>
            ))}
          </div>
        )}
      </div>
    </section>
  );
};

export default MentorTaskManagement;
