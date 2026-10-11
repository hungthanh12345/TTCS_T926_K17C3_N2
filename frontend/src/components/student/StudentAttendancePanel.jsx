import React, { useCallback, useEffect, useState } from 'react';
import {
  Calendar,
  CheckCircle2,
  Clock,
  Filter,
  History,
  Hourglass,
  Info,
  Loader2,
  LogIn,
  LogOut,
  MessageSquare,
  RefreshCw,
  Sparkles,
} from 'lucide-react';
import toast from 'react-hot-toast';
import attendanceService from '../../services/attendanceService';

const STATUS_BADGES = {
  CHECKED_IN: {
    label: 'Đang làm việc',
    bg: 'bg-amber-50 text-amber-700 border-amber-200',
    dot: 'bg-amber-500 animate-pulse',
  },
  COMPLETED: {
    label: 'Hoàn thành',
    bg: 'bg-emerald-50 text-emerald-700 border-emerald-200',
    dot: 'bg-emerald-500',
  },
};

const formatTime = (isoString) => {
  if (!isoString) return '--:--';
  const d = new Date(isoString);
  return d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
};

const formatDate = (dateStr) => {
  if (!dateStr) return '';
  const [year, month, day] = dateStr.split('-');
  return `${day}/${month}/${year}`;
};

const getDayOfWeekName = (dateStr) => {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  const days = ['Chủ Nhật', 'Thứ Hai', 'Thứ Ba', 'Thứ Tư', 'Thứ Năm', 'Thứ Sáu', 'Thứ Bảy'];
  return days[d.getDay()];
};

export const StudentAttendancePanel = () => {
  const [todayStatus, setTodayStatus] = useState(null);
  const [historyData, setHistoryData] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [notes, setNotes] = useState('');

  // Live real-time clock
  const [currentTime, setCurrentTime] = useState(new Date());

  // Filter states
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [statusFilter, setStatusFilter] = useState('');

  useEffect(() => {
    const timer = setInterval(() => setCurrentTime(new Date()), 1000);
    return () => clearInterval(timer);
  }, []);

  const loadData = useCallback(async () => {
    try {
      const [today, history] = await Promise.all([
        attendanceService.getTodayStatus(),
        attendanceService.getHistory({
          startDate: startDate || undefined,
          endDate: endDate || undefined,
          status: statusFilter || undefined,
        }),
      ]);
      setTodayStatus(today);
      setHistoryData(history);
    } catch (error) {
      toast.error(error.message || 'Không thể tải dữ liệu chấm công.');
    } finally {
      setIsLoading(false);
    }
  }, [startDate, endDate, statusFilter]);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  const handleCheckIn = async () => {
    if (isSubmitting) return;
    setIsSubmitting(true);
    try {
      await attendanceService.checkIn({ notes: notes.trim() || undefined });
      toast.success('Check-in thành công! Bắt đầu ghi nhận giờ làm việc.');
      setNotes('');
      await loadData();
    } catch (error) {
      toast.error(error.message || 'Check-in thất bại.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCheckOut = async () => {
    if (isSubmitting) return;
    setIsSubmitting(true);
    try {
      await attendanceService.checkOut({ notes: notes.trim() || undefined });
      toast.success('Check-out thành công! Ca làm việc đã hoàn thành.');
      setNotes('');
      await loadData();
    } catch (error) {
      toast.error(error.message || 'Check-out thất bại.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const isCheckedIn = todayStatus?.isCheckedIn ?? todayStatus?.IsCheckedIn ?? false;
  const isCheckedOut = todayStatus?.isCheckedOut ?? todayStatus?.IsCheckedOut ?? false;
  const currentAttendance = todayStatus?.currentAttendance ?? todayStatus?.CurrentAttendance;

  // Real-time elapsed time calculation if checked in but not checked out yet
  const getElapsedWorkTime = () => {
    if (!isCheckedIn || isCheckedOut || !currentAttendance?.checkInTime) return null;
    const checkIn = new Date(currentAttendance.checkInTime).getTime();
    const now = currentTime.getTime();
    const diffMins = Math.max(0, Math.floor((now - checkIn) / 60000));
    const h = Math.floor(diffMins / 60);
    const m = diffMins % 60;
    return `${h} giờ ${m} phút`;
  };

  const elapsedTime = getElapsedWorkTime();

  return (
    <div className="space-y-6">
      {/* Top Banner & Live Action Card */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        {/* Live Clock & Date Card */}
        <div className="flex flex-col justify-between rounded-2xl bg-gradient-to-br from-indigo-900 via-indigo-950 to-slate-950 p-6 text-white shadow-xl">
          <div>
            <div className="flex items-center justify-between">
              <span className="flex items-center gap-1.5 rounded-full bg-indigo-500/20 px-3 py-1 text-xs font-semibold text-indigo-200">
                <Clock className="h-3.5 w-3.5 animate-pulse text-indigo-400" />
                Thời gian thực tế
              </span>
              <span className="text-xs text-indigo-300">Giờ Việt Nam (GMT+7)</span>
            </div>

            <div className="mt-4">
              <p className="font-mono text-4xl font-extrabold tracking-tight text-white sm:text-5xl">
                {currentTime.toLocaleTimeString('vi-VN', { hour12: false })}
              </p>
              <p className="mt-1 text-sm font-medium text-indigo-200">
                {currentTime.toLocaleDateString('vi-VN', {
                  weekday: 'long',
                  year: 'numeric',
                  month: 'long',
                  day: 'numeric',
                })}
              </p>
            </div>
          </div>

          <div className="mt-6 rounded-xl border border-indigo-800/40 bg-indigo-950/60 p-3.5 text-xs text-indigo-200">
            <div className="flex items-start gap-2">
              <Info className="mt-0.5 h-4 w-4 shrink-0 text-indigo-400" />
              <p>
                Thực hiện <strong>Check-in</strong> khi bắt đầu làm việc và <strong>Check-out</strong> khi kết thúc ca để hệ thống tự động ghi nhận thời gian làm việc chính xác.
              </p>
            </div>
          </div>
        </div>

        {/* Today Status & Check-in / Check-out Controller */}
        <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm lg:col-span-2">
          <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 pb-4">
            <div>
              <h3 className="text-lg font-bold text-slate-800">Trạng thái chấm công hôm nay</h3>
              <p className="text-xs text-slate-500">
                Ngày: {formatDate(todayStatus?.todayDate || todayStatus?.TodayDate)}
              </p>
            </div>

            {/* Dynamic Status Badge */}
            {isCheckedOut ? (
              <span className="inline-flex items-center gap-1.5 rounded-full border border-emerald-200 bg-emerald-50 px-3 py-1 text-xs font-semibold text-emerald-700">
                <CheckCircle2 className="h-3.5 w-3.5 text-emerald-600" />
                Đã hoàn thành ca làm việc
              </span>
            ) : isCheckedIn ? (
              <span className="inline-flex items-center gap-1.5 rounded-full border border-amber-200 bg-amber-50 px-3 py-1 text-xs font-semibold text-amber-700">
                <span className="h-2 w-2 rounded-full bg-amber-500 animate-pulse" />
                Đang trong ca làm việc
              </span>
            ) : (
              <span className="inline-flex items-center gap-1.5 rounded-full border border-slate-200 bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">
                <Hourglass className="h-3.5 w-3.5 text-slate-400" />
                Chưa check-in
              </span>
            )}
          </div>

          {/* Time Tracking Progress Details */}
          <div className="mt-5 grid grid-cols-1 gap-4 sm:grid-cols-3">
            <div className="rounded-xl border border-slate-100 bg-slate-50/70 p-3.5">
              <p className="text-[11px] font-bold uppercase tracking-wider text-slate-400">Giờ vào (Check-in)</p>
              <p className="mt-1 text-lg font-bold text-slate-800">
                {currentAttendance?.checkInTime ? formatTime(currentAttendance.checkInTime) : '--:--:--'}
              </p>
              {currentAttendance?.checkInTime && (
                <p className="mt-0.5 text-[11px] text-emerald-600 font-medium">✓ Đã ghi nhận</p>
              )}
            </div>

            <div className="rounded-xl border border-slate-100 bg-slate-50/70 p-3.5">
              <p className="text-[11px] font-bold uppercase tracking-wider text-slate-400">Giờ ra (Check-out)</p>
              <p className="mt-1 text-lg font-bold text-slate-800">
                {currentAttendance?.checkOutTime ? formatTime(currentAttendance.checkOutTime) : '--:--:--'}
              </p>
              {currentAttendance?.checkOutTime ? (
                <p className="mt-0.5 text-[11px] text-emerald-600 font-medium">✓ Đã ghi nhận</p>
              ) : isCheckedIn ? (
                <p className="mt-0.5 text-[11px] text-amber-600 font-medium">Đang chờ check-out</p>
              ) : (
                <p className="mt-0.5 text-[11px] text-slate-400">Chưa bắt đầu</p>
              )}
            </div>

            <div className="rounded-xl border border-slate-100 bg-indigo-50/50 p-3.5">
              <p className="text-[11px] font-bold uppercase tracking-wider text-indigo-400">Thời lượng làm việc</p>
              <p className="mt-1 text-lg font-bold text-indigo-700">
                {isCheckedOut
                  ? currentAttendance?.durationFormatted || `${currentAttendance?.durationMinutes || 0} phút`
                  : elapsedTime || '0 phút'}
              </p>
              <p className="mt-0.5 text-[11px] text-indigo-500 font-medium">
                {isCheckedOut ? 'Tổng thời gian ca' : isCheckedIn ? 'Đang tích lũy...' : 'Chưa có'}
              </p>
            </div>
          </div>

          {/* Optional Notes Input */}
          <div className="mt-4">
            <label htmlFor="attendance-notes" className="flex items-center gap-1.5 text-xs font-semibold text-slate-700 mb-1.5">
              <MessageSquare className="h-3.5 w-3.5 text-slate-400" />
              Ghi chú (Tùy chọn: nội dung công việc, địa điểm làm việc...)
            </label>
            <input
              id="attendance-notes"
              type="text"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              disabled={isCheckedOut || isSubmitting}
              placeholder={
                isCheckedOut
                  ? 'Đã hoàn tất chấm công hôm nay.'
                  : isCheckedIn
                  ? 'Nhập ghi chú khi Check-out (ví dụ: đã xong task sprint 2)'
                  : 'Nhập ghi chú khi Check-in (ví dụ: làm việc tại phòng lab)'
              }
              className="w-full rounded-xl border border-slate-200 bg-slate-50/50 px-3.5 py-2 text-sm text-slate-800 placeholder-slate-400 transition-colors focus:border-indigo-500 focus:bg-white focus:outline-none focus:ring-1 focus:ring-indigo-500 disabled:bg-slate-100 disabled:text-slate-400"
            />
          </div>

          {/* Action Buttons: Check-in / Check-out */}
          <div className="mt-5 flex flex-wrap items-center gap-3">
            {/* Check-in Button */}
            <button
              id="btn-checkin"
              type="button"
              onClick={handleCheckIn}
              disabled={isCheckedIn || isSubmitting}
              className={`flex flex-1 items-center justify-center gap-2 rounded-xl px-5 py-3 text-sm font-bold shadow-sm transition-all ${
                isCheckedIn
                  ? 'cursor-not-allowed bg-slate-100 text-slate-400 border border-slate-200'
                  : 'bg-emerald-600 text-white hover:bg-emerald-700 active:scale-[0.99] shadow-emerald-600/20'
              }`}
            >
              {isSubmitting ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <LogIn className="h-4 w-4" />
              )}
              {isCheckedIn ? '✓ Đã Check-in hôm nay' : 'Check-in (Bắt đầu)'}
            </button>

            {/* Check-out Button */}
            <button
              id="btn-checkout"
              type="button"
              onClick={handleCheckOut}
              disabled={!isCheckedIn || isCheckedOut || isSubmitting}
              className={`flex flex-1 items-center justify-center gap-2 rounded-xl px-5 py-3 text-sm font-bold shadow-sm transition-all ${
                !isCheckedIn
                  ? 'cursor-not-allowed bg-slate-100 text-slate-400 border border-slate-200'
                  : isCheckedOut
                  ? 'cursor-not-allowed bg-slate-100 text-slate-400 border border-slate-200'
                  : 'bg-indigo-600 text-white hover:bg-indigo-700 active:scale-[0.99] shadow-indigo-600/20'
              }`}
            >
              {isSubmitting ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <LogOut className="h-4 w-4" />
              )}
              {isCheckedOut
                ? '✓ Đã Check-out hoàn tất'
                : !isCheckedIn
                ? 'Check-out (Cần Check-in trước)'
                : 'Check-out (Kết thúc ca)'}
            </button>
          </div>
        </div>
      </div>

      {/* Summary KPI Cards */}
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
        <div className="rounded-2xl border border-slate-100 bg-white p-4 shadow-sm">
          <p className="text-xs font-semibold text-slate-400">Tổng ngày chấm công</p>
          <p className="mt-1 text-2xl font-black text-slate-800">{historyData?.totalDays ?? 0}</p>
          <p className="mt-0.5 text-[11px] text-slate-500">Toàn bộ thời gian</p>
        </div>

        <div className="rounded-2xl border border-slate-100 bg-white p-4 shadow-sm">
          <p className="text-xs font-semibold text-indigo-500">Tổng thời gian tích lũy</p>
          <p className="mt-1 text-2xl font-black text-indigo-600">
            {historyData?.totalHoursFormatted || '0 giờ 0 phút'}
          </p>
          <p className="mt-0.5 text-[11px] text-slate-500">{historyData?.totalMinutes ?? 0} phút làm việc</p>
        </div>

        <div className="rounded-2xl border border-slate-100 bg-white p-4 shadow-sm">
          <p className="text-xs font-semibold text-emerald-600">Ca hoàn thành đầy đủ</p>
          <p className="mt-1 text-2xl font-black text-emerald-600">{historyData?.completedDays ?? 0}</p>
          <p className="mt-0.5 text-[11px] text-slate-500">Đã check-out trọn vẹn</p>
        </div>

        <div className="rounded-2xl border border-slate-100 bg-white p-4 shadow-sm">
          <p className="text-xs font-semibold text-amber-600">Ca chưa check-out</p>
          <p className="mt-1 text-2xl font-black text-amber-600">{historyData?.pendingDays ?? 0}</p>
          <p className="mt-0.5 text-[11px] text-slate-500">Đang hoặc chưa kết thúc</p>
        </div>
      </div>

      {/* Attendance History Section */}
      <div className="rounded-2xl border border-slate-200 bg-white shadow-sm overflow-hidden">
        {/* Header & Filter Controls */}
        <div className="p-5 border-b border-slate-100 space-y-4">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div className="flex items-center gap-2">
              <History className="h-5 w-5 text-indigo-600" />
              <h3 className="text-base font-bold text-slate-800">Lịch sử chấm công thực tập</h3>
            </div>

            <button
              type="button"
              onClick={() => {
                setIsLoading(true);
                void loadData();
              }}
              disabled={isLoading}
              className="inline-flex items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-50 disabled:opacity-50"
            >
              <RefreshCw className={`h-3.5 w-3.5 ${isLoading ? 'animate-spin' : ''}`} />
              Làm mới
            </button>
          </div>

          {/* Filters Bar */}
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-4">
            <div>
              <label htmlFor="filter-start-date" className="block text-[11px] font-semibold text-slate-500 mb-1">
                Từ ngày
              </label>
              <input
                id="filter-start-date"
                type="date"
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
                className="w-full rounded-lg border border-slate-200 px-3 py-1.5 text-xs text-slate-700 focus:border-indigo-500 focus:outline-none"
              />
            </div>

            <div>
              <label htmlFor="filter-end-date" className="block text-[11px] font-semibold text-slate-500 mb-1">
                Đến ngày
              </label>
              <input
                id="filter-end-date"
                type="date"
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
                className="w-full rounded-lg border border-slate-200 px-3 py-1.5 text-xs text-slate-700 focus:border-indigo-500 focus:outline-none"
              />
            </div>

            <div>
              <label htmlFor="filter-status" className="block text-[11px] font-semibold text-slate-500 mb-1">
                Trạng thái
              </label>
              <select
                id="filter-status"
                value={statusFilter}
                onChange={(e) => setStatusFilter(e.target.value)}
                className="w-full rounded-lg border border-slate-200 px-3 py-1.5 text-xs text-slate-700 focus:border-indigo-500 focus:outline-none"
              >
                <option value="">Tất cả trạng thái</option>
                <option value="COMPLETED">Hoàn thành</option>
                <option value="CHECKED_IN">Đang làm việc</option>
              </select>
            </div>

            <div className="flex items-end">
              {(startDate || endDate || statusFilter) && (
                <button
                  type="button"
                  onClick={() => {
                    setStartDate('');
                    setEndDate('');
                    setStatusFilter('');
                  }}
                  className="w-full rounded-lg border border-slate-200 bg-slate-50 px-3 py-1.5 text-xs font-semibold text-slate-600 hover:bg-slate-100"
                >
                  Xóa bộ lọc
                </button>
              )}
            </div>
          </div>
        </div>

        {/* Records Table */}
        <div className="overflow-x-auto">
          {isLoading ? (
            <div className="flex items-center justify-center py-12 text-slate-400">
              <Loader2 className="h-6 w-6 animate-spin" />
              <span className="ml-2 text-sm font-medium">Đang tải lịch sử chấm công...</span>
            </div>
          ) : !historyData?.records || historyData.records.length === 0 ? (
            <div className="py-12 text-center">
              <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-slate-100 text-slate-400">
                <Calendar className="h-6 w-6" />
              </div>
              <p className="mt-3 text-sm font-semibold text-slate-700">Chưa có bản ghi chấm công nào</p>
              <p className="mt-1 text-xs text-slate-400">
                Hãy nhấn nút "Check-in" ở trên để bắt đầu chấm công cho ngày hôm nay.
              </p>
            </div>
          ) : (
            <table className="w-full text-left text-sm text-slate-700">
              <thead className="border-b border-slate-100 bg-slate-50/75 text-[11px] font-bold uppercase tracking-wider text-slate-400">
                <tr>
                  <th className="px-5 py-3.5">Ngày làm việc</th>
                  <th className="px-5 py-3.5">Giờ vào</th>
                  <th className="px-5 py-3.5">Giờ ra</th>
                  <th className="px-5 py-3.5">Thời lượng</th>
                  <th className="px-5 py-3.5">Trạng thái</th>
                  <th className="px-5 py-3.5">Ghi chú</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 font-medium">
                {historyData.records.map((item) => {
                  const badge = STATUS_BADGES[item.status] || {
                    label: item.status,
                    bg: 'bg-slate-50 text-slate-600 border-slate-200',
                    dot: 'bg-slate-400',
                  };
                  return (
                    <tr key={item.id} className="hover:bg-slate-50/60 transition-colors">
                      <td className="px-5 py-3.5">
                        <div className="font-bold text-slate-800">{formatDate(item.date)}</div>
                        <div className="text-xs text-slate-400 font-normal">{getDayOfWeekName(item.date)}</div>
                      </td>
                      <td className="px-5 py-3.5 font-mono text-xs text-slate-700">
                        {formatTime(item.checkInTime)}
                      </td>
                      <td className="px-5 py-3.5 font-mono text-xs text-slate-700">
                        {formatTime(item.checkOutTime)}
                      </td>
                      <td className="px-5 py-3.5">
                        <span className="font-semibold text-indigo-700">
                          {item.durationFormatted || (item.durationMinutes ? `${item.durationMinutes} phút` : '--')}
                        </span>
                      </td>
                      <td className="px-5 py-3.5">
                        <span
                          className={`inline-flex items-center gap-1.5 rounded-full border px-2.5 py-0.5 text-xs font-semibold ${badge.bg}`}
                        >
                          <span className={`h-1.5 w-1.5 rounded-full ${badge.dot}`} />
                          {badge.label}
                        </span>
                      </td>
                      <td className="px-5 py-3.5 text-xs text-slate-500 max-w-xs truncate" title={item.notes || ''}>
                        {item.notes || <span className="text-slate-300 italic">Không có</span>}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </div>
  );
};

export default StudentAttendancePanel;
