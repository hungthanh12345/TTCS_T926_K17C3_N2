import React, { useCallback, useEffect, useState } from 'react';
import { CalendarClock, Loader2, Plus, RefreshCw, Save, Trash2, Users, X } from 'lucide-react';
import toast from 'react-hot-toast';
import DashboardLayout from '../../components/layout/DashboardLayout';
import hrWorkforceService from '../../services/hrWorkforceService';

const WEEKDAYS = ['Chủ nhật', 'Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7'];
const newDay = (dayOfWeek = 1) => ({ dayOfWeek, startTime: '08:30', endTime: '17:30', breakStart: '12:00', breakEnd: '13:00' });
const emptySchedule = () => ({ name: '', targetType: 'STUDENT', studentId: '', studentGroupId: '', isFlexible: false, status: 'ACTIVE', days: [newDay()] });
const timeString = (value) => value ? (value.length === 5 ? `${value}:00` : value) : null;
const statusLabel = (value) => value === 'ACTIVE' ? 'Đang áp dụng' : value === 'INACTIVE' ? 'Tạm dừng' : 'Chưa xác định';

export const WorkSchedulesView = () => {
  const [schedules, setSchedules] = useState(null);
  const [groups, setGroups] = useState(null);
  const [students, setStudents] = useState(null);
  const [schedule, setSchedule] = useState(emptySchedule());
  const [editingId, setEditingId] = useState(null);
  const [groupForm, setGroupForm] = useState({ name: '', description: '' });
  const [memberStudentId, setMemberStudentId] = useState('');
  const [loading, setLoading] = useState(true);
  const [sending, setSending] = useState(false);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    setSchedules(null);
    setGroups(null);
    setStudents(null);
    setError('');
    try {
      const [loadedSchedules, loadedGroups, loadedStudents] = await Promise.all([
        hrWorkforceService.getSchedules(),
        hrWorkforceService.getGroups(),
        hrWorkforceService.getStudents(),
      ]);
      const scheduleRows = Array.isArray(loadedSchedules) ? loadedSchedules : loadedSchedules?.items;
      const groupRows = Array.isArray(loadedGroups) ? loadedGroups : loadedGroups?.items;
      const studentRows = Array.isArray(loadedStudents) ? loadedStudents : loadedStudents?.items;
      if (!Array.isArray(scheduleRows) || !Array.isArray(groupRows) || !Array.isArray(studentRows)) throw new Error('Dữ liệu lịch, nhóm hoặc sinh viên không đúng định dạng.');
      setSchedules(scheduleRows);
      setGroups(groupRows);
      setStudents(studentRows);
      setError('');
    } catch (loadError) {
      setError(loadError.message || 'Không thể tải lịch làm việc.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { void load(); }, [load]);

  const saveGroup = async (event) => {
    event.preventDefault();
    setSending(true);
    try {
      await hrWorkforceService.createGroup(groupForm);
      setGroupForm({ name: '', description: '' });
      toast.success('Đã tạo nhóm thực tập.');
      await load();
    } catch (requestError) {
      setSchedules(null);
      setGroups(null);
      setStudents(null);
      setError(requestError.message || 'Không thể tạo nhóm.');
    } finally {
      setSending(false);
    }
  };

  const addMember = async (groupId) => {
    if (!memberStudentId) return;
    setSending(true);
    try {
      await hrWorkforceService.addGroupMember(groupId, Number(memberStudentId));
      setMemberStudentId('');
      toast.success('Đã thêm sinh viên vào nhóm. Các nhóm khác của sinh viên được giữ nguyên.');
      await load();
    } catch (requestError) {
      setSchedules(null);
      setGroups(null);
      setStudents(null);
      setError(requestError.message || 'Không thể thêm sinh viên vào nhóm.');
    } finally {
      setSending(false);
    }
  };

  const removeMember = async (groupId, studentId, studentName) => {
    if (!window.confirm(`Gỡ ${studentName} khỏi nhóm này?`)) return;
    setSending(true);
    try {
      await hrWorkforceService.removeGroupMember(groupId, studentId);
      toast.success('Đã gỡ thành viên khỏi nhóm.');
      await load();
    } catch (requestError) {
      setSchedules(null);
      setGroups(null);
      setStudents(null);
      setError(requestError.message || 'Không thể gỡ thành viên.');
    } finally {
      setSending(false);
    }
  };

  const saveSchedule = async (event) => {
    event.preventDefault();
    setSending(true);
    const payload = {
      ...schedule,
      studentId: schedule.targetType === 'STUDENT' ? Number(schedule.studentId) : null,
      studentGroupId: schedule.targetType === 'GROUP' ? Number(schedule.studentGroupId) : null,
      days: schedule.days.map((day) => ({
        dayOfWeek: Number(day.dayOfWeek),
        startTime: timeString(day.startTime),
        endTime: timeString(day.endTime),
        breakStart: timeString(day.breakStart),
        breakEnd: timeString(day.breakEnd),
      })),
    };
    try {
      if (editingId) await hrWorkforceService.updateSchedule(editingId, payload);
      else await hrWorkforceService.createSchedule(payload);
      toast.success(editingId ? 'Đã cập nhật lịch.' : 'Đã tạo lịch.');
      setSchedule(emptySchedule());
      setEditingId(null);
      setError('');
      await load();
    } catch (requestError) {
      setSchedules(null);
      setGroups(null);
      setStudents(null);
      setError(requestError.message || 'Không thể lưu lịch làm việc.');
    } finally {
      setSending(false);
    }
  };

  const beginEdit = (item) => {
    setEditingId(item.id);
    setSchedule({
      name: item.name,
      targetType: item.targetType,
      studentId: item.studentId || '',
      studentGroupId: item.studentGroupId || '',
      isFlexible: item.isFlexible,
      status: item.status,
      days: (item.days || []).map((day) => ({
        dayOfWeek: day.dayOfWeek,
        startTime: String(day.startTime || '').slice(0, 5),
        endTime: String(day.endTime || '').slice(0, 5),
        breakStart: day.breakStart ? String(day.breakStart).slice(0, 5) : '',
        breakEnd: day.breakEnd ? String(day.breakEnd).slice(0, 5) : '',
      })),
    });
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const cancelEdit = () => {
    setSchedule(emptySchedule());
    setEditingId(null);
  };

  const removeSchedule = async (item) => {
    if (!window.confirm(`Xóa lịch “${item.name}”?`)) return;
    setSending(true);
    try {
      await hrWorkforceService.deleteSchedule(item.id);
      toast.success('Đã xóa lịch làm việc.');
      await load();
    } catch (requestError) {
      setSchedules(null);
      setGroups(null);
      setStudents(null);
      setError(requestError.message || 'Không thể xóa lịch.');
    } finally {
      setSending(false);
    }
  };

  const changeDay = (index, name, value) => setSchedule((current) => ({
    ...current,
    days: current.days.map((day, dayIndex) => dayIndex === index ? { ...day, [name]: value } : day),
  }));

  const addDay = () => {
    const usedDays = new Set(schedule.days.map((day) => Number(day.dayOfWeek)));
    const nextDay = WEEKDAYS.findIndex((_, dayOfWeek) => !usedDays.has(dayOfWeek));
    if (nextDay < 0) return;
    setSchedule((current) => ({ ...current, days: [...current.days, newDay(nextDay)] }));
  };

  return (
    <DashboardLayout title="Quản lý lịch làm việc" subtitle="Thiết lập lịch lặp theo nhóm hoặc từng thực tập sinh.">
      <div className="space-y-5">
        {error && (
          <div role="alert" className="flex justify-between gap-3 rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800">
            <span>{error}</span>
            <button type="button" onClick={() => void load()} className="inline-flex shrink-0 items-center gap-1 font-semibold">
              <RefreshCw className="h-4 w-4" /> Tải lại
            </button>
          </div>
        )}

        <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
          <div className="mb-4 flex items-center gap-3">
            <span className="rounded-xl bg-indigo-50 p-2.5 text-indigo-700"><CalendarClock className="h-5 w-5" /></span>
            <div>
              <h2 className="text-sm font-bold text-slate-900">{editingId ? 'Cập nhật lịch' : 'Tạo lịch mới'}</h2>
              <p className="mt-1 text-xs text-slate-500">Sinh viên có thể thuộc nhiều nhóm; việc thêm vào nhóm không thay đổi các nhóm khác.</p>
            </div>
          </div>

          <form onSubmit={saveSchedule} className="space-y-4">
            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
              <label className="text-xs font-semibold text-slate-600">Tên lịch
                <input required minLength={2} maxLength={120} value={schedule.name} onChange={(event) => setSchedule({ ...schedule, name: event.target.value })} className="mt-1 block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm" />
              </label>
              <label className="text-xs font-semibold text-slate-600">Áp dụng cho
                <select value={schedule.targetType} onChange={(event) => setSchedule({ ...schedule, targetType: event.target.value, studentId: '', studentGroupId: '' })} className="mt-1 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm">
                  <option value="STUDENT">Một sinh viên</option><option value="GROUP">Một nhóm</option>
                </select>
              </label>
              {schedule.targetType === 'STUDENT' ? (
                <label className="text-xs font-semibold text-slate-600">Sinh viên
                  <select required value={schedule.studentId} onChange={(event) => setSchedule({ ...schedule, studentId: event.target.value })} className="mt-1 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm">
                    <option value="">Chọn sinh viên</option>{(students || []).map((student) => <option key={student.id} value={student.id}>{student.fullName} · {student.studentCode}</option>)}
                  </select>
                </label>
              ) : (
                <label className="text-xs font-semibold text-slate-600">Nhóm
                  <select required value={schedule.studentGroupId} onChange={(event) => setSchedule({ ...schedule, studentGroupId: event.target.value })} className="mt-1 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm">
                    <option value="">Chọn nhóm</option>{(groups || []).filter((group) => group.status === 'ACTIVE').map((group) => <option key={group.id} value={group.id}>{group.name}</option>)}
                  </select>
                </label>
              )}
              <label className="text-xs font-semibold text-slate-600">Trạng thái
                <select value={schedule.status} onChange={(event) => setSchedule({ ...schedule, status: event.target.value })} className="mt-1 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm"><option value="ACTIVE">Đang áp dụng</option><option value="INACTIVE">Tạm dừng</option></select>
              </label>
            </div>

            <label className="inline-flex items-center gap-2 text-sm text-slate-700">
              <input type="checkbox" checked={schedule.isFlexible} onChange={(event) => setSchedule({ ...schedule, isFlexible: event.target.checked })} /> Lịch linh hoạt
            </label>

            <div className="space-y-3">
              {schedule.days.map((day, index) => (
                <div key={`${index}-${day.dayOfWeek}`} className="grid gap-3 rounded-xl border border-slate-200 p-3 sm:grid-cols-2 lg:grid-cols-6">
                  <label className="text-[11px] font-semibold text-slate-600">Thứ
                    <select value={day.dayOfWeek} onChange={(event) => changeDay(index, 'dayOfWeek', Number(event.target.value))} className="mt-1 block w-full rounded border border-slate-300 bg-white px-2 py-2 text-sm">
                      {WEEKDAYS.map((weekday, dayOfWeek) => <option key={dayOfWeek} value={dayOfWeek} disabled={schedule.days.some((other, otherIndex) => otherIndex !== index && Number(other.dayOfWeek) === dayOfWeek)}>{weekday}</option>)}
                    </select>
                  </label>
                  <TimeField label="Bắt đầu" value={day.startTime} onChange={(value) => changeDay(index, 'startTime', value)} />
                  <TimeField label="Kết thúc" value={day.endTime} onChange={(value) => changeDay(index, 'endTime', value)} />
                  <TimeField label="Bắt đầu nghỉ (không bắt buộc)" value={day.breakStart} onChange={(value) => changeDay(index, 'breakStart', value)} />
                  <TimeField label="Kết thúc nghỉ (không bắt buộc)" value={day.breakEnd} onChange={(value) => changeDay(index, 'breakEnd', value)} />
                  <div className="flex items-end"><button type="button" disabled={schedule.days.length <= 1} onClick={() => setSchedule((current) => ({ ...current, days: current.days.filter((_, dayIndex) => dayIndex !== index) }))} className="inline-flex w-full items-center justify-center gap-1 rounded border border-rose-200 px-2 py-2 text-xs font-semibold text-rose-700 disabled:opacity-40"><X className="h-4 w-4" /> Xóa ngày</button></div>
                </div>
              ))}
              <button type="button" disabled={schedule.days.length >= 7} onClick={addDay} className="inline-flex items-center gap-1 rounded-lg border px-3 py-2 text-xs font-semibold disabled:opacity-40"><Plus className="h-4 w-4" /> Thêm ngày</button>
            </div>

            <div className="flex flex-wrap gap-2">
              <button disabled={sending || loading || Boolean(error) || !students || !groups} className="inline-flex items-center gap-2 rounded-lg bg-indigo-700 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"><Save className="h-4 w-4" />{sending ? 'Đang lưu...' : editingId ? 'Cập nhật lịch' : 'Lưu lịch'}</button>
              {editingId && <button type="button" onClick={cancelEdit} className="rounded-lg border px-4 py-2 text-sm font-semibold">Hủy sửa</button>}
            </div>
          </form>
        </section>

        <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <div className="mb-4 flex items-center gap-3"><Users className="h-5 w-5 text-indigo-700" /><div><h2 className="text-sm font-bold text-slate-900">Nhóm thực tập</h2><p className="mt-1 text-xs text-slate-500">Quản lý thành viên nhóm; một sinh viên có thể thuộc nhiều nhóm.</p></div></div>
          <form onSubmit={saveGroup} className="grid gap-2 sm:grid-cols-[1fr_1.5fr_auto]">
            <input required minLength={2} maxLength={100} value={groupForm.name} onChange={(event) => setGroupForm({ ...groupForm, name: event.target.value })} placeholder="Tên nhóm" className="rounded-lg border border-slate-300 px-3 py-2 text-sm" />
            <input maxLength={500} value={groupForm.description} onChange={(event) => setGroupForm({ ...groupForm, description: event.target.value })} placeholder="Mô tả (không bắt buộc)" className="rounded-lg border border-slate-300 px-3 py-2 text-sm" />
            <button disabled={sending} className="rounded-lg bg-slate-900 px-4 py-2 text-xs font-semibold text-white disabled:opacity-50">Tạo nhóm</button>
          </form>
          {loading ? <p role="status" className="mt-4 text-sm text-slate-500">Đang tải nhóm...</p> : error ? null : groups?.length === 0 ? <p role="status" className="mt-4 text-sm text-slate-500">Chưa có nhóm.</p> : groups && (
            <div className="mt-4 grid gap-3 lg:grid-cols-2">
              {groups.map((group) => (
                <article key={group.id} className="rounded-xl border border-slate-200 p-4">
                  <div className="flex items-start justify-between gap-2"><div><h3 className="text-sm font-bold text-slate-900">{group.name}</h3><p className="mt-1 text-xs text-slate-500">{group.description || 'Không có mô tả'} · {group.members?.length || 0} thành viên</p></div><span className="rounded-full bg-emerald-50 px-2 py-1 text-[10px] font-bold text-emerald-700">{statusLabel(group.status)}</span></div>
                  <div className="mt-3 flex gap-2"><select aria-label={`Chọn sinh viên cho ${group.name}`} value={memberStudentId} onChange={(event) => setMemberStudentId(event.target.value)} className="min-w-0 flex-1 rounded-lg border border-slate-300 bg-white px-2 py-2 text-xs"><option value="">Chọn sinh viên</option>{(students || []).map((student) => <option key={student.id} value={student.id}>{student.fullName} · {student.studentCode}</option>)}</select><button type="button" disabled={sending || !memberStudentId} onClick={() => void addMember(group.id)} className="rounded-lg border px-3 py-2 text-xs font-semibold disabled:opacity-50">Gán</button></div>
                  <div className="mt-3 flex flex-wrap gap-2">{group.members?.map((member) => <span key={member.studentId} className="inline-flex items-center gap-2 rounded-full bg-slate-100 px-3 py-1.5 text-[11px] text-slate-700">{member.fullName} · {member.studentCode}<button type="button" disabled={sending} onClick={() => void removeMember(group.id, member.studentId, member.fullName)} aria-label={`Gỡ ${member.fullName}`} className="font-bold text-rose-700 disabled:opacity-50">×</button></span>)}</div>
                </article>
              ))}
            </div>
          )}
        </section>

        <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
          <div className="flex items-center justify-between border-b border-slate-100 p-5"><div><h2 className="text-sm font-bold text-slate-900">Lịch đã tạo</h2><p className="mt-1 text-xs text-slate-500">Giờ nghỉ nằm trong khung giờ làm việc; lịch qua nửa đêm chưa được hỗ trợ.</p></div><button type="button" disabled={loading} onClick={() => void load()} className="rounded-lg border p-2 disabled:opacity-50" aria-label="Tải lại lịch"><RefreshCw className="h-4 w-4" /></button></div>
          {loading ? <div role="status" className="flex items-center justify-center gap-2 p-8 text-sm text-slate-500"><Loader2 className="h-4 w-4 animate-spin" /> Đang tải...</div> : error ? null : schedules?.length === 0 ? <p role="status" className="p-8 text-center text-sm text-slate-500">Chưa có lịch làm việc.</p> : schedules && <div className="divide-y divide-slate-100">{schedules.map((item) => <article key={item.id} className="flex flex-col justify-between gap-3 p-4 md:flex-row md:items-center"><div><div className="flex flex-wrap items-center gap-2"><h3 className="text-sm font-bold text-slate-900">{item.name}</h3><span className="rounded-full bg-slate-100 px-2 py-1 text-[10px] font-bold">{statusLabel(item.status)}</span></div><p className="mt-1 text-xs text-slate-500">{item.targetType === 'GROUP' ? `Nhóm: ${item.studentGroupName || item.studentGroupId}` : `Sinh viên: ${item.studentName || item.studentId}`}{item.isFlexible ? ' · Linh hoạt' : ''}</p><p className="mt-2 text-xs text-slate-700">{(item.days || []).map((day) => `${WEEKDAYS[day.dayOfWeek]} ${String(day.startTime).slice(0, 5)}–${String(day.endTime).slice(0, 5)}`).join(' · ')}</p></div><div className="flex gap-2"><button type="button" onClick={() => beginEdit(item)} className="rounded-lg border px-3 py-2 text-xs font-semibold">Sửa</button><button type="button" disabled={sending} onClick={() => void removeSchedule(item)} className="inline-flex items-center gap-1 rounded-lg border border-rose-200 px-3 py-2 text-xs font-semibold text-rose-700 disabled:opacity-50"><Trash2 className="h-4 w-4" /> Xóa</button></div></article>)}</div>}
        </section>
      </div>
    </DashboardLayout>
  );
};

const TimeField = ({ label, value, onChange }) => <label className="text-[11px] font-semibold text-slate-600">{label}<input type="time" value={value || ''} onChange={(event) => onChange(event.target.value)} className="mt-1 block w-full rounded border border-slate-300 bg-white px-2 py-2" /></label>;
export default WorkSchedulesView;
