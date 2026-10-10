import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  CalendarDays, Plus, Search, RefreshCw, Pencil, Trash2, Clock3,
  Users, UserRound, X, CheckCircle2, AlertCircle,
} from 'lucide-react';
import toast from 'react-hot-toast';
import DashboardLayout from '../../components/layout/DashboardLayout';
import studentService from '../../services/studentService';
import workScheduleService from '../../services/workScheduleService';

const DAYS = [
  { value: 1, label: 'Thứ 2', short: 'T2' },
  { value: 2, label: 'Thứ 3', short: 'T3' },
  { value: 3, label: 'Thứ 4', short: 'T4' },
  { value: 4, label: 'Thứ 5', short: 'T5' },
  { value: 5, label: 'Thứ 6', short: 'T6' },
  { value: 6, label: 'Thứ 7', short: 'T7' },
  { value: 7, label: 'Chủ nhật', short: 'CN' },
];

const fieldClass = 'mt-1.5 w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-sm text-slate-800 outline-none transition focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100';
const emptyDay = (dayOfWeek) => ({
  dayOfWeek, startTime: '09:00', endTime: '18:00', breakStart: '12:00', breakEnd: '13:00',
});
const emptyForm = () => ({
  name: '', targetType: 'GROUP', groupName: '', studentId: '',
  isFlexible: true, days: [1, 2, 3, 4, 5].map(emptyDay),
});

const timeValue = (value) => typeof value === 'string' ? value.slice(0, 5) : '09:00';
const normalizeSchedule = (item) => ({
  ...item,
  days: (item.days || []).map((day) => ({
    ...day,
    startTime: timeValue(day.startTime),
    endTime: timeValue(day.endTime),
    breakStart: day.breakStart ? timeValue(day.breakStart) : '',
    breakEnd: day.breakEnd ? timeValue(day.breakEnd) : '',
  })),
});

export default function WorkSchedulesView() {
  const [schedules, setSchedules] = useState([]);
  const [students, setStudents] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [search, setSearch] = useState('');
  const [targetFilter, setTargetFilter] = useState('ALL');
  const [statusFilter, setStatusFilter] = useState('ALL');
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState(null);
  const [form, setForm] = useState(emptyForm());
  const [saving, setSaving] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState(null);

  const loadData = useCallback(async () => {
    setRefreshing(true);
    try {
      const [scheduleData, studentData] = await Promise.all([
        workScheduleService.getAll(),
        studentService.getStudents().catch(() => []),
      ]);
      setSchedules(Array.isArray(scheduleData) ? scheduleData : scheduleData?.items || []);
      setStudents(Array.isArray(studentData) ? studentData : []);
    } catch (error) {
      toast.error(error.message || 'Không thể tải danh sách lịch làm việc.');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useEffect(() => { void loadData(); }, [loadData]);

  const filteredSchedules = useMemo(() => schedules.filter((item) => {
    const targetName = item.targetType === 'STUDENT'
      ? (students.find((student) => Number(student.id) === Number(item.studentId))?.fullName || `Thực tập sinh #${item.studentId}`)
      : (item.groupName || 'Chưa đặt tên nhóm');
    const matchesSearch = `${item.name} ${targetName}`.toLocaleLowerCase('vi').includes(search.toLocaleLowerCase('vi'));
    const matchesTarget = targetFilter === 'ALL' || item.targetType === targetFilter;
    const matchesStatus = statusFilter === 'ALL' || (item.status || 'ACTIVE') === statusFilter;
    return matchesSearch && matchesTarget && matchesStatus;
  }), [schedules, students, search, targetFilter, statusFilter]);

  const activeCount = schedules.filter((item) => (item.status || 'ACTIVE') === 'ACTIVE').length;
  const flexibleCount = schedules.filter((item) => item.isFlexible && (item.status || 'ACTIVE') === 'ACTIVE').length;

  const openCreate = () => {
    setEditing(null);
    setForm(emptyForm());
    setModalOpen(true);
  };

  const openEdit = (item) => {
    setEditing(item);
    setForm({
      name: item.name || '',
      targetType: item.targetType || 'GROUP',
      groupName: item.groupName || '',
      studentId: item.studentId ? String(item.studentId) : '',
      isFlexible: Boolean(item.isFlexible),
      days: (normalizeSchedule(item).days || []).map((day) => ({
        ...day,
        breakStart: day.breakStart || '',
        breakEnd: day.breakEnd || '',
      })),
    });
    setModalOpen(true);
  };

  const toggleDay = (dayNumber) => {
    setForm((current) => {
      const exists = current.days.some((day) => Number(day.dayOfWeek) === dayNumber);
      const days = exists
        ? current.days.filter((day) => Number(day.dayOfWeek) !== dayNumber)
        : [...current.days, emptyDay(dayNumber)];
      return { ...current, days: days.sort((a, b) => a.dayOfWeek - b.dayOfWeek) };
    });
  };

  const updateDay = (dayNumber, field, value) => {
    setForm((current) => ({
      ...current,
      days: current.days.map((day) => Number(day.dayOfWeek) === dayNumber
        ? { ...day, [field]: value }
        : day),
    }));
  };

  const saveSchedule = async (event) => {
    event.preventDefault();
    if (form.name.trim().length < 2) return toast.error('Tên lịch cần có ít nhất 2 ký tự.');
    if (form.days.length === 0) return toast.error('Hãy chọn ít nhất một ngày làm việc.');
    if (form.targetType === 'GROUP' && !form.groupName.trim()) return toast.error('Vui lòng nhập tên nhóm.');
    if (form.targetType === 'STUDENT' && !form.studentId) return toast.error('Vui lòng chọn thực tập sinh.');

    for (const day of form.days) {
      if (!day.startTime || !day.endTime || day.startTime >= day.endTime) {
        return toast.error(`Khung giờ làm việc của ${DAYS.find((d) => d.value === Number(day.dayOfWeek))?.label || 'ngày đã chọn'} chưa hợp lệ.`);
      }
      if (Boolean(day.breakStart) !== Boolean(day.breakEnd)) return toast.error('Vui lòng nhập đủ giờ bắt đầu và kết thúc nghỉ trưa.');
      if (day.breakStart && (day.breakStart < day.startTime || day.breakEnd > day.endTime || day.breakStart >= day.breakEnd)) {
        return toast.error('Giờ nghỉ phải nằm trong khung giờ làm việc.');
      }
    }

    const payload = {
      name: form.name.trim(),
      targetType: form.targetType,
      groupName: form.targetType === 'GROUP' ? form.groupName.trim() : null,
      studentId: form.targetType === 'STUDENT' ? Number(form.studentId) : null,
      isFlexible: Boolean(form.isFlexible),
      days: form.days.map((day) => ({
        dayOfWeek: Number(day.dayOfWeek),
        startTime: `${day.startTime}:00`,
        endTime: `${day.endTime}:00`,
        breakStart: day.breakStart ? `${day.breakStart}:00` : null,
        breakEnd: day.breakEnd ? `${day.breakEnd}:00` : null,
      })),
    };

    setSaving(true);
    try {
      if (editing) {
        await workScheduleService.update(editing.id, payload);
        toast.success('Đã cập nhật lịch làm việc.');
      } else {
        await workScheduleService.create(payload);
        toast.success('Đã tạo lịch làm việc.');
      }
      setModalOpen(false);
      await loadData();
    } catch (error) {
      toast.error(error.message || 'Không thể lưu lịch làm việc.');
    } finally {
      setSaving(false);
    }
  };

  const confirmDelete = async () => {
    if (!deleteTarget) return;
    try {
      await workScheduleService.remove(deleteTarget.id);
      toast.success('Đã ngừng kích hoạt lịch làm việc.');
      setDeleteTarget(null);
      await loadData();
    } catch (error) {
      toast.error(error.message || 'Không thể ngừng kích hoạt lịch.');
    }
  };

  const getTargetLabel = (item) => {
    if (item.targetType === 'STUDENT') {
      const student = students.find((s) => Number(s.id) === Number(item.studentId));
      return student ? `${student.fullName} (${student.studentCode || `#${student.id}`})` : `Thực tập sinh #${item.studentId}`;
    }
    return item.groupName || 'Chưa đặt tên nhóm';
  };

  return (
    <DashboardLayout title="Thiết lập lịch làm việc" subtitle="Quản lý thời gian làm việc linh hoạt theo nhóm hoặc từng thực tập sinh">
      <div className="space-y-6">
        <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
            <div className="flex items-center justify-between">
              <p className="text-sm font-medium text-slate-500">Tổng lịch làm việc</p>
              <span className="rounded-xl bg-indigo-50 p-2.5 text-indigo-600"><CalendarDays className="h-5 w-5" /></span>
            </div>
            <p className="mt-3 text-3xl font-bold text-slate-900">{schedules.length}</p>
            <p className="mt-1 text-xs text-slate-400">Bao gồm lịch đang hoạt động và đã ngừng</p>
          </div>
          <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
            <div className="flex items-center justify-between">
              <p className="text-sm font-medium text-slate-500">Lịch đang hoạt động</p>
              <span className="rounded-xl bg-emerald-50 p-2.5 text-emerald-600"><CheckCircle2 className="h-5 w-5" /></span>
            </div>
            <p className="mt-3 text-3xl font-bold text-emerald-600">{activeCount}</p>
            <p className="mt-1 text-xs text-slate-400">Có thể áp dụng cho thực tập sinh</p>
          </div>
          <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:col-span-2 xl:col-span-1">
            <div className="flex items-center justify-between">
              <p className="text-sm font-medium text-slate-500">Lịch linh hoạt</p>
              <span className="rounded-xl bg-sky-50 p-2.5 text-sky-600"><Clock3 className="h-5 w-5" /></span>
            </div>
            <p className="mt-3 text-3xl font-bold text-sky-600">{flexibleCount}</p>
            <p className="mt-1 text-xs text-slate-400">Cho phép tùy chỉnh theo nhu cầu nhóm</p>
          </div>
        </section>

        <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
          <div className="flex flex-col gap-4 border-b border-slate-100 p-5 lg:flex-row lg:items-center lg:justify-between">
            <div>
              <h2 className="font-bold text-slate-900">Danh sách lịch làm việc</h2>
              <p className="mt-1 text-xs text-slate-500">Tạo lịch theo nhóm hoặc áp dụng riêng cho một thực tập sinh.</p>
            </div>
            <div className="flex flex-wrap gap-2">
              <button onClick={loadData} disabled={refreshing} className="inline-flex items-center gap-2 rounded-xl border border-slate-200 px-3 py-2.5 text-sm font-semibold text-slate-600 hover:bg-slate-50 disabled:opacity-50">
                <RefreshCw className={`h-4 w-4 ${refreshing ? 'animate-spin' : ''}`} /> Làm mới
              </button>
              <button onClick={openCreate} className="inline-flex items-center gap-2 rounded-xl bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-indigo-500">
                <Plus className="h-4 w-4" /> Tạo lịch mới
              </button>
            </div>
          </div>

          <div className="grid gap-3 border-b border-slate-100 bg-slate-50/70 p-4 md:grid-cols-[minmax(220px,1fr)_180px_180px]">
            <label className="relative block">
              <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
              <input className={`${fieldClass} mt-0 pl-9`} value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Tìm theo tên lịch, nhóm, thực tập sinh..." />
            </label>
            <select className={`${fieldClass} mt-0`} value={targetFilter} onChange={(e) => setTargetFilter(e.target.value)}>
              <option value="ALL">Tất cả đối tượng</option>
              <option value="GROUP">Theo nhóm</option>
              <option value="STUDENT">Theo thực tập sinh</option>
            </select>
            <select className={`${fieldClass} mt-0`} value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
              <option value="ALL">Tất cả trạng thái</option>
              <option value="ACTIVE">Đang hoạt động</option>
              <option value="INACTIVE">Đã ngừng</option>
            </select>
          </div>

          {loading ? (
            <div className="space-y-3 p-5">{[1, 2, 3].map((n) => <div key={n} className="h-16 animate-pulse rounded-xl bg-slate-100" />)}</div>
          ) : filteredSchedules.length === 0 ? (
            <div className="px-5 py-16 text-center">
              <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-indigo-50 text-indigo-500"><CalendarDays className="h-7 w-7" /></div>
              <h3 className="mt-4 font-semibold text-slate-900">Chưa có lịch phù hợp</h3>
              <p className="mt-1 text-sm text-slate-500">Tạo lịch làm việc đầu tiên hoặc thay đổi bộ lọc tìm kiếm.</p>
              <button onClick={openCreate} className="mt-4 rounded-xl bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-indigo-500"><Plus className="mr-1 inline h-4 w-4" /> Tạo lịch làm việc</button>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full min-w-[760px] text-left">
                <thead className="bg-white text-[11px] uppercase tracking-wider text-slate-400">
                  <tr>
                    <th className="px-5 py-3 font-semibold">Tên lịch</th>
                    <th className="px-5 py-3 font-semibold">Áp dụng cho</th>
                    <th className="px-5 py-3 font-semibold">Ngày làm việc</th>
                    <th className="px-5 py-3 font-semibold">Khung giờ</th>
                    <th className="px-5 py-3 font-semibold">Trạng thái</th>
                    <th className="px-5 py-3 text-right font-semibold">Thao tác</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {filteredSchedules.map((item) => (
                    <tr key={item.id} className="transition hover:bg-slate-50/70">
                      <td className="px-5 py-4">
                        <div className="font-semibold text-slate-800">{item.name}</div>
                        <div className="mt-1 text-xs text-slate-400">{item.isFlexible ? 'Lịch linh hoạt' : 'Lịch cố định'}</div>
                      </td>
                      <td className="px-5 py-4">
                        <div className="flex items-center gap-2 text-sm text-slate-700">
                          <span className="rounded-lg bg-slate-100 p-2 text-slate-500">{item.targetType === 'STUDENT' ? <UserRound className="h-4 w-4" /> : <Users className="h-4 w-4" />}</span>
                          <span>{getTargetLabel(item)}</span>
                        </div>
                        <span className="ml-10 text-xs text-slate-400">{item.targetType === 'STUDENT' ? 'Cá nhân' : 'Theo nhóm'}</span>
                      </td>
                      <td className="px-5 py-4">
                        <div className="flex max-w-56 flex-wrap gap-1">
                          {(item.days || []).sort((a, b) => a.dayOfWeek - b.dayOfWeek).map((day) => (
                            <span key={day.dayOfWeek} className="rounded-md bg-indigo-50 px-2 py-1 text-[11px] font-semibold text-indigo-700">{DAYS.find((d) => d.value === Number(day.dayOfWeek))?.short || day.dayOfWeek}</span>
                          ))}
                        </div>
                      </td>
                      <td className="px-5 py-4 text-sm text-slate-600">
                        {item.days?.length ? `${timeValue(item.days[0].startTime)}–${timeValue(item.days[0].endTime)}` : '—'}
                        {item.days?.length > 1 && <div className="mt-1 text-xs text-slate-400">Áp dụng theo từng ngày</div>}
                      </td>
                      <td className="px-5 py-4">
                        <span className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-semibold ${(item.status || 'ACTIVE') === 'ACTIVE' ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'}`}>
                          <span className={`h-1.5 w-1.5 rounded-full ${(item.status || 'ACTIVE') === 'ACTIVE' ? 'bg-emerald-500' : 'bg-slate-400'}`} />
                          {(item.status || 'ACTIVE') === 'ACTIVE' ? 'Đang hoạt động' : 'Đã ngừng'}
                        </span>
                      </td>
                      <td className="px-5 py-4">
                        <div className="flex justify-end gap-1">
                          <button onClick={() => openEdit(item)} title="Chỉnh sửa" className="rounded-lg p-2 text-slate-500 hover:bg-indigo-50 hover:text-indigo-600"><Pencil className="h-4 w-4" /></button>
                          {(item.status || 'ACTIVE') === 'ACTIVE' && <button onClick={() => setDeleteTarget(item)} title="Ngừng kích hoạt" className="rounded-lg p-2 text-slate-500 hover:bg-rose-50 hover:text-rose-600"><Trash2 className="h-4 w-4" /></button>}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          <div className="border-t border-slate-100 px-5 py-3 text-xs text-slate-400">Hiển thị {filteredSchedules.length} / {schedules.length} lịch làm việc</div>
        </section>
      </div>

      {modalOpen && (
        <div className="fixed inset-0 z-[100] flex items-center justify-center overflow-y-auto bg-slate-950/50 p-3 backdrop-blur-sm sm:p-6">
          <div className="my-auto max-h-[94vh] w-full max-w-3xl overflow-y-auto rounded-2xl bg-white shadow-2xl">
            <div className="sticky top-0 z-10 flex items-center justify-between border-b border-slate-100 bg-white px-5 py-4 sm:px-6">
              <div>
                <h2 className="font-bold text-slate-900">{editing ? 'Chỉnh sửa lịch làm việc' : 'Tạo lịch làm việc mới'}</h2>
                <p className="mt-1 text-xs text-slate-500">Thiết lập đối tượng áp dụng và thời gian làm việc.</p>
              </div>
              <button onClick={() => setModalOpen(false)} className="rounded-lg p-2 text-slate-400 hover:bg-slate-100 hover:text-slate-700"><X className="h-5 w-5" /></button>
            </div>
            <form onSubmit={saveSchedule} className="space-y-6 p-5 sm:p-6">
              <div className="grid gap-4 sm:grid-cols-2">
                <label className="block text-xs font-semibold text-slate-700 sm:col-span-2">Tên lịch làm việc *
                  <input className={fieldClass} value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} placeholder="Ví dụ: Lịch nhóm Backend" minLength={2} maxLength={100} required />
                </label>
                <label className="block text-xs font-semibold text-slate-700">Đối tượng áp dụng *
                  <select className={fieldClass} value={form.targetType} onChange={(e) => setForm({ ...form, targetType: e.target.value })}>
                    <option value="GROUP">Theo nhóm</option>
                    <option value="STUDENT">Theo thực tập sinh</option>
                  </select>
                </label>
                <label className="flex items-center gap-3 rounded-xl border border-indigo-100 bg-indigo-50/60 px-4 py-3">
                  <input type="checkbox" checked={form.isFlexible} onChange={(e) => setForm({ ...form, isFlexible: e.target.checked })} className="h-4 w-4 rounded border-slate-300 text-indigo-600 focus:ring-indigo-500" />
                  <span><span className="block text-sm font-semibold text-slate-800">Lịch linh hoạt</span><span className="mt-0.5 block text-xs text-slate-500">Cho phép điều chỉnh theo nhu cầu</span></span>
                </label>
                {form.targetType === 'GROUP' ? (
                  <label className="block text-xs font-semibold text-slate-700 sm:col-span-2">Tên nhóm *
                    <input className={fieldClass} value={form.groupName} onChange={(e) => setForm({ ...form, groupName: e.target.value })} placeholder="Ví dụ: Backend, Frontend, QA" maxLength={100} required />
                  </label>
                ) : (
                  <label className="block text-xs font-semibold text-slate-700 sm:col-span-2">Thực tập sinh *
                    <select className={fieldClass} value={form.studentId} onChange={(e) => setForm({ ...form, studentId: e.target.value })} required>
                      <option value="">Chọn thực tập sinh</option>
                      {students.map((student) => <option key={student.id} value={student.id}>{student.fullName} — {student.studentCode || `#${student.id}`}</option>)}
                    </select>
                    {students.length === 0 && <span className="mt-1 block font-normal text-amber-700">Chưa tải được danh sách thực tập sinh. Bạn vẫn có thể tạo lịch theo nhóm.</span>}
                  </label>
                )}
              </div>

              <div>
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <div><h3 className="font-bold text-slate-900">Ngày và giờ làm việc</h3><p className="mt-1 text-xs text-slate-500">Chọn ngày làm, sau đó thiết lập giờ bắt đầu, kết thúc và nghỉ giữa ca.</p></div>
                  <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">{form.days.length}/7 ngày</span>
                </div>
                <div className="mt-4 grid grid-cols-4 gap-2 sm:grid-cols-7">
                  {DAYS.map((day) => {
                    const selected = form.days.some((item) => Number(item.dayOfWeek) === day.value);
                    return <button type="button" key={day.value} onClick={() => toggleDay(day.value)} className={`rounded-xl border px-2 py-3 text-center text-xs font-semibold transition ${selected ? 'border-indigo-500 bg-indigo-50 text-indigo-700 ring-1 ring-indigo-100' : 'border-slate-200 bg-white text-slate-500 hover:border-slate-300'}`}>
                      <span className="block">{day.short}</span><span className={`mx-auto mt-2 block h-1.5 w-1.5 rounded-full ${selected ? 'bg-indigo-500' : 'bg-slate-200'}`} />
                    </button>;
                  })}
                </div>
                <div className="mt-4 space-y-3">
                  {form.days.map((day) => (
                    <div key={day.dayOfWeek} className="rounded-xl border border-slate-200 p-4">
                      <div className="mb-3 flex items-center justify-between">
                        <span className="text-sm font-semibold text-slate-800">{DAYS.find((item) => item.value === Number(day.dayOfWeek))?.label}</span>
                        <button type="button" onClick={() => toggleDay(Number(day.dayOfWeek))} className="text-xs font-medium text-slate-400 hover:text-rose-600">Bỏ ngày</button>
                      </div>
                      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
                        <label className="block text-xs font-medium text-slate-600">Bắt đầu
                          <input type="time" className={fieldClass} value={day.startTime} onChange={(e) => updateDay(Number(day.dayOfWeek), 'startTime', e.target.value)} required />
                        </label>
                        <label className="block text-xs font-medium text-slate-600">Kết thúc
                          <input type="time" className={fieldClass} value={day.endTime} onChange={(e) => updateDay(Number(day.dayOfWeek), 'endTime', e.target.value)} required />
                        </label>
                        <label className="block text-xs font-medium text-slate-600">Nghỉ từ
                          <input type="time" className={fieldClass} value={day.breakStart || ''} onChange={(e) => updateDay(Number(day.dayOfWeek), 'breakStart', e.target.value)} />
                        </label>
                        <label className="block text-xs font-medium text-slate-600">Nghỉ đến
                          <input type="time" className={fieldClass} value={day.breakEnd || ''} onChange={(e) => updateDay(Number(day.dayOfWeek), 'breakEnd', e.target.value)} />
                        </label>
                      </div>
                    </div>
                  ))}
                  {form.days.length === 0 && <div className="rounded-xl border border-dashed border-slate-300 p-6 text-center text-sm text-slate-500"><AlertCircle className="mx-auto mb-2 h-5 w-5" />Chọn ít nhất một ngày làm việc.</div>}
                </div>
              </div>

              <div className="flex flex-col-reverse gap-2 border-t border-slate-100 pt-4 sm:flex-row sm:justify-end">
                <button type="button" onClick={() => setModalOpen(false)} className="rounded-xl border border-slate-200 px-4 py-2.5 text-sm font-semibold text-slate-600 hover:bg-slate-50">Hủy</button>
                <button type="submit" disabled={saving} className="rounded-xl bg-indigo-600 px-5 py-2.5 text-sm font-semibold text-white hover:bg-indigo-500 disabled:cursor-not-allowed disabled:opacity-60">{saving ? 'Đang lưu...' : editing ? 'Lưu thay đổi' : 'Tạo lịch làm việc'}</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {deleteTarget && (
        <div className="fixed inset-0 z-[110] flex items-center justify-center bg-slate-950/50 p-4 backdrop-blur-sm">
          <div className="w-full max-w-md rounded-2xl bg-white p-6 shadow-2xl">
            <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-rose-50 text-rose-600"><AlertCircle className="h-6 w-6" /></div>
            <h2 className="mt-4 text-lg font-bold text-slate-900">Ngừng lịch làm việc?</h2>
            <p className="mt-2 text-sm leading-6 text-slate-500">Lịch “{deleteTarget.name}” sẽ được ngừng kích hoạt. Dữ liệu lịch không bị xóa vĩnh viễn.</p>
            <div className="mt-6 flex justify-end gap-2">
              <button onClick={() => setDeleteTarget(null)} className="rounded-xl border border-slate-200 px-4 py-2.5 text-sm font-semibold text-slate-600 hover:bg-slate-50">Hủy</button>
              <button onClick={confirmDelete} className="rounded-xl bg-rose-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-rose-500">Ngừng kích hoạt</button>
            </div>
          </div>
        </div>
      )}
    </DashboardLayout>
  );
}
