import React, { useCallback, useEffect, useState } from 'react';
import { CalendarDays, CheckCircle2, Clock3, LoaderCircle, RefreshCw } from 'lucide-react';
import DashboardLayout from '../../components/layout/DashboardLayout';
import studentScheduleService from '../../services/studentScheduleService';

const STATUS_LABELS = {
  TO_DO: 'Cần thực hiện',
  IN_PROGRESS: 'Đang thực hiện',
  DONE: 'Hoàn thành',
};

const formatDate = (value) => {
  if (!value) return 'Chưa có ngày';
  const [year, month, day] = value.split('-').map(Number);
  const date = new Date(year, month - 1, day);
  return Number.isNaN(date.getTime())
    ? value
    : new Intl.DateTimeFormat('vi-VN', {
      weekday: 'long',
      day: '2-digit',
      month: 'long',
      year: 'numeric',
    }).format(date);
};

export const StudentScheduleView = () => {
  const [events, setEvents] = useState([]);
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(true);

  const loadSchedule = useCallback(async () => {
    setIsLoading(true);
    setError('');
    try {
      setEvents(await studentScheduleService.getMySchedule());
    } catch (loadError) {
      setError(loadError.message || 'Không thể tải lịch thực tập của bạn.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    let isActive = true;
    studentScheduleService.getMySchedule()
      .then((schedule) => {
        if (isActive) setEvents(schedule);
      })
      .catch((loadError) => {
        if (isActive) setError(loadError.message || 'Không thể tải lịch thực tập của bạn.');
      })
      .finally(() => {
        if (isActive) setIsLoading(false);
      });

    return () => {
      isActive = false;
    };
  }, []);

  return (
    <DashboardLayout
      title="Lịch Thực Tập Cá Nhân"
      subtitle="Theo dõi các hạn hoàn thành công việc được Mentor giao cho bạn"
    >
      <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
        <div className="mb-5 flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 pb-4">
          <div>
            <h2 className="flex items-center gap-2 text-base font-bold text-slate-900">
              <CalendarDays className="h-5 w-5 text-indigo-600" />
              Lịch công việc
            </h2>
            <p className="mt-1 text-xs text-slate-500">Các task có hạn hoàn thành, sắp xếp theo ngày gần nhất.</p>
          </div>
          <button
            type="button"
            onClick={() => void loadSchedule()}
            disabled={isLoading}
            className="inline-flex items-center gap-2 rounded-lg border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-600 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
          >
            <RefreshCw className={`h-3.5 w-3.5 ${isLoading ? 'animate-spin' : ''}`} />
            Làm mới
          </button>
        </div>

        {isLoading ? (
          <div role="status" className="flex items-center gap-2 py-8 text-sm text-slate-500">
            <LoaderCircle className="h-4 w-4 animate-spin" /> Đang tải lịch thực tập...
          </div>
        ) : error ? (
          <div role="alert" className="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800">
            <p>Không thể tải lịch: {error}</p>
            <button type="button" onClick={() => void loadSchedule()} className="mt-3 font-semibold underline underline-offset-2">
              Thử lại
            </button>
          </div>
        ) : events.length === 0 ? (
          <div role="status" className="rounded-xl border border-dashed border-slate-300 px-4 py-10 text-center">
            <CalendarDays className="mx-auto h-8 w-8 text-slate-300" />
            <h3 className="mt-3 text-sm font-bold text-slate-800">Chưa có lịch công việc</h3>
            <p className="mt-1 text-sm text-slate-500">Các task được giao có hạn hoàn thành sẽ xuất hiện tại đây.</p>
          </div>
        ) : (
          <ol className="space-y-3">
            {events.map((event) => (
              <li key={event.taskId} className="relative rounded-xl border border-slate-200 p-4 sm:p-5">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div className="min-w-0">
                    <p className="flex items-center gap-2 text-xs font-semibold capitalize text-indigo-700">
                      <Clock3 className="h-4 w-4 shrink-0" />
                      {formatDate(event.dueDate)}
                    </p>
                    <h3 className="mt-2 text-sm font-bold text-slate-900">{event.title}</h3>
                    <p className="mt-1 whitespace-pre-wrap text-sm leading-6 text-slate-600">
                      {event.description || 'Không có mô tả.'}
                    </p>
                  </div>
                  <span className={`inline-flex shrink-0 items-center gap-1.5 rounded-full px-2.5 py-1 text-[11px] font-bold ${event.status === 'DONE' ? 'bg-emerald-50 text-emerald-700' : 'bg-indigo-50 text-indigo-700'}`}>
                    {event.status === 'DONE' && <CheckCircle2 className="h-3.5 w-3.5" />}
                    {STATUS_LABELS[event.status] || event.status}
                  </span>
                </div>
              </li>
            ))}
          </ol>
        )}
      </section>
    </DashboardLayout>
  );
};

export default StudentScheduleView;
