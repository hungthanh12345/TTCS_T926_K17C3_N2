import React, { useEffect, useState } from 'react';
import { ClipboardList, Loader2 } from 'lucide-react';
import toast from 'react-hot-toast';
import mentorTaskService from '../../services/mentorTaskService';

export const StudentTasksPanel = () => {
  const [tasks, setTasks] = useState([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let mounted = true;
    mentorTaskService.getMyTasks()
      .then((result) => { if (mounted) setTasks(result); })
      .catch((error) => { if (mounted) toast.error(error.message || 'Không thể tải task được giao.'); })
      .finally(() => { if (mounted) setIsLoading(false); });
    return () => { mounted = false; };
  }, []);

  return (
    <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
      <div className="mb-4">
        <h3 className="flex items-center gap-2 text-base font-bold text-slate-900"><ClipboardList className="h-5 w-5 text-indigo-600" /> Task được giao</h3>
        <p className="mt-1 text-xs text-slate-500">Danh sách task gắn với tài khoản sinh viên của bạn.</p>
      </div>
      {isLoading ? <div className="flex items-center gap-2 py-6 text-sm text-slate-500"><Loader2 className="h-4 w-4 animate-spin" /> Đang tải task...</div> : tasks.length === 0 ? (
        <p className="rounded-xl border border-dashed border-slate-300 px-4 py-7 text-center text-sm text-slate-500">Bạn chưa được giao task nào.</p>
      ) : (
        <div className="grid gap-3 md:grid-cols-2">
          {tasks.map((task) => (
            <article key={task.id} className="rounded-xl border border-slate-200 p-4">
              <div className="flex items-start justify-between gap-3">
                <h4 className="text-sm font-bold text-slate-900">{task.title}</h4>
                <span className="shrink-0 rounded-full bg-indigo-50 px-2.5 py-1 text-[10px] font-bold text-indigo-700">{task.status}</span>
              </div>
              <p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-slate-600">{task.description || 'Không có mô tả.'}</p>
              <p className="mt-3 border-t border-slate-100 pt-3 text-xs text-slate-500">Deadline: {task.dueDate || 'Chưa đặt'}</p>
            </article>
          ))}
        </div>
      )}
    </section>
  );
};

export default StudentTasksPanel;
