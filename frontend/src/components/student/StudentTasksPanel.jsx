import React, { useCallback, useEffect, useState } from 'react';
import { ClipboardList, Loader2, RefreshCw, Save } from 'lucide-react';
import toast from 'react-hot-toast';
import mentorTaskService from '../../services/mentorTaskService';

const STATUS_OPTIONS = [
  { value: 'TO_DO', label: 'Cần thực hiện' },
  { value: 'IN_PROGRESS', label: 'Đang thực hiện' },
  { value: 'DONE', label: 'Hoàn thành' },
];

const statusLabel = (status) => STATUS_OPTIONS.find((option) => option.value === status)?.label || status;

export const StudentTasksPanel = () => {
  const [tasks, setTasks] = useState([]);
  const [statusDrafts, setStatusDrafts] = useState({});
  const [details, setDetails] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [savingTaskId, setSavingTaskId] = useState(null);
  const [loadingDetailsId, setLoadingDetailsId] = useState(null);

  const loadTasks = useCallback(async () => {
    try {
      setTasks(await mentorTaskService.getMyTasks());
      setStatusDrafts({});
      setDetails(null);
    } catch (error) {
      toast.error(error.message || 'Không thể tải task được giao.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadTasks();
  }, [loadTasks]);

  const refreshTasks = () => {
    setIsLoading(true);
    void loadTasks();
  };

  const showDetails = async (taskId) => {
    if (details?.id === taskId) {
      setDetails(null);
      return;
    }
    setLoadingDetailsId(taskId);
    try {
      setDetails(await mentorTaskService.getMyTask(taskId));
    } catch (error) {
      toast.error(error.message || 'Không thể tải chi tiết task.');
    } finally {
      setLoadingDetailsId(null);
    }
  };

  const saveProgress = async (task) => {
    const status = statusDrafts[task.id] ?? task.status;
    if (status === task.status) return;

    setSavingTaskId(task.id);
    try {
      const updated = await mentorTaskService.updateMyTaskProgress(task.id, { status });
      setTasks((current) => current.map((item) => item.id === task.id ? updated : item));
      setStatusDrafts((current) => ({ ...current, [task.id]: updated.status }));
      setDetails((current) => current?.id === task.id ? updated : current);
      toast.success('Đã cập nhật tiến độ task.');
    } catch (error) {
      toast.error(error.message || 'Không thể cập nhật tiến độ task.');
    } finally {
      setSavingTaskId(null);
    }
  };

  return (
    <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <div>
          <h3 className="flex items-center gap-2 text-base font-bold text-slate-900"><ClipboardList className="h-5 w-5 text-indigo-600" /> Task được giao</h3>
          <p className="mt-1 text-xs text-slate-500">Cập nhật trạng thái cho task gắn với tài khoản của bạn.</p>
        </div>
        <button type="button" onClick={refreshTasks} disabled={isLoading} className="inline-flex items-center gap-2 rounded-lg border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-600 hover:bg-slate-50 disabled:opacity-50">
          <RefreshCw className={`h-3.5 w-3.5 ${isLoading ? 'animate-spin' : ''}`} /> Làm mới
        </button>
      </div>
      {isLoading ? <div className="flex items-center gap-2 py-6 text-sm text-slate-500"><Loader2 className="h-4 w-4 animate-spin" /> Đang tải task...</div> : tasks.length === 0 ? (
        <p className="rounded-xl border border-dashed border-slate-300 px-4 py-7 text-center text-sm text-slate-500">Bạn chưa được giao task nào.</p>
      ) : (
        <div className="grid gap-3 md:grid-cols-2">
          {tasks.map((task) => (
            <article key={task.id} className="rounded-xl border border-slate-200 p-4">
              <div className="flex items-start justify-between gap-3">
                <h4 className="text-sm font-bold text-slate-900">{task.title}</h4>
                <span className="shrink-0 rounded-full bg-indigo-50 px-2.5 py-1 text-[10px] font-bold text-indigo-700">{statusLabel(task.status)}</span>
              </div>
              <p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-slate-600">{task.description || 'Không có mô tả.'}</p>
              <p className="mt-3 border-t border-slate-100 pt-3 text-xs text-slate-500">Deadline: {task.dueDate || 'Chưa đặt'}</p>
              <label className="mt-4 block space-y-1 text-xs font-semibold text-slate-600">
                <span>Trạng thái tiến độ</span>
                <select
                  value={statusDrafts[task.id] ?? task.status}
                  onChange={(event) => setStatusDrafts((current) => ({ ...current, [task.id]: event.target.value }))}
                  className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-indigo-500 focus:ring-4 focus:ring-indigo-100"
                >
                  {STATUS_OPTIONS.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
                </select>
              </label>
              <div className="mt-3 flex flex-wrap items-center justify-between gap-2">
                <button type="button" onClick={() => showDetails(task.id)} disabled={loadingDetailsId === task.id} className="rounded-lg px-2 py-1.5 text-xs font-semibold text-indigo-700 hover:bg-indigo-50 disabled:opacity-50">
                  {loadingDetailsId === task.id ? 'Đang tải...' : details?.id === task.id ? 'Ẩn chi tiết' : 'Xem chi tiết'}
                </button>
                <button type="button" onClick={() => saveProgress(task)} disabled={savingTaskId === task.id || (statusDrafts[task.id] ?? task.status) === task.status} className="inline-flex items-center gap-1.5 rounded-lg bg-indigo-600 px-3 py-2 text-xs font-bold text-white hover:bg-indigo-500 disabled:cursor-not-allowed disabled:opacity-50">
                  {savingTaskId === task.id ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Save className="h-3.5 w-3.5" />}
                  Lưu tiến độ
                </button>
              </div>
              {details?.id === task.id && (
                <div className="mt-3 rounded-lg bg-slate-50 p-3 text-xs text-slate-600">
                  <p><span className="font-semibold">Trạng thái đã lưu:</span> {statusLabel(details.status)}</p>
                  <p className="mt-1"><span className="font-semibold">Cập nhật lần cuối:</span> {new Date(details.updatedAt).toLocaleString()}</p>
                  {details.description && <p className="mt-2 whitespace-pre-wrap">{details.description}</p>}
                </div>
              )}
            </article>
          ))}
        </div>
      )}
    </section>
  );
};

export default StudentTasksPanel;
