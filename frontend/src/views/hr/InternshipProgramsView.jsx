import React, { useCallback, useEffect, useState } from 'react';
import { Building2, Plus, RefreshCw } from 'lucide-react';
import toast from 'react-hot-toast';
import DashboardLayout from '../../components/layout/DashboardLayout';
import AssignStudentToProgramModal from '../../components/modals/AssignStudentToProgramModal';
import US11 from '../../services/sprint2/US11';
import { messageOf } from '../../services/sprint2/common';
import ProgramDatesEditor from './ProgramDatesEditor';

const emptyProgram = { name: '', description: '', departmentId: '' };
const fieldClass = 'mt-1.5 w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-sm text-slate-800 outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100';

export const InternshipProgramsView = () => {
  const [departments, setDepartments] = useState([]);
  const [programs, setPrograms] = useState([]);
  const [departmentName, setDepartmentName] = useState('');
  const [departmentDescription, setDepartmentDescription] = useState('');
  const [form, setForm] = useState(emptyProgram);
  const [loading, setLoading] = useState(true);
  const [savingDepartment, setSavingDepartment] = useState(false);
  const [savingProgram, setSavingProgram] = useState(false);
  const [programToAssign, setProgramToAssign] = useState(null);

  const load = useCallback(async () => {
    try {
      const [nextDepartments, nextPrograms] = await Promise.all([
        US11.getDepartments(),
        US11.getHrPrograms(),
      ]);
      setDepartments(nextDepartments || []);
      setPrograms(nextPrograms || []);
    } catch (error) {
      toast.error(messageOf(error, 'Không thể tải danh sách phòng ban và chương trình.'));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { void Promise.resolve().then(load); }, [load]);

  const addDepartment = async (event) => {
    event.preventDefault();
    const name = departmentName.trim();
    if (name.length < 2) return toast.error('Tên phòng ban cần có ít nhất 2 ký tự.');

    setSavingDepartment(true);
    try {
      const department = await US11.createDepartment({
        name,
        description: departmentDescription.trim() || null,
      });
      setDepartments((items) => [...items, department].sort((a, b) => a.name.localeCompare(b.name, 'vi')));
      setForm((current) => ({ ...current, departmentId: current.departmentId || String(department.id) }));
      setDepartmentName('');
      setDepartmentDescription('');
      toast.success('Đã tạo phòng ban.');
    } catch (error) {
      toast.error(messageOf(error, 'Không thể tạo phòng ban.'));
    } finally {
      setSavingDepartment(false);
    }
  };

  const createProgram = async (event) => {
    event.preventDefault();
    const name = form.name.trim();
    if (name.length < 2) return toast.error('Tên chương trình cần có ít nhất 2 ký tự.');
    if (!form.departmentId) return toast.error('Hãy chọn phòng ban cho chương trình.');

    setSavingProgram(true);
    try {
      await US11.createProgram({
        name,
        description: form.description.trim() || null,
        departmentId: Number(form.departmentId),
      });
      setForm(emptyProgram);
      toast.success('Đã tạo chương trình thực tập.');
      await load();
    } catch (error) {
      toast.error(messageOf(error, 'Không thể tạo chương trình.'));
    } finally {
      setSavingProgram(false);
    }
  };

  const updateProgramDates = (updatedProgram) => {
    setPrograms((items) => items.map((program) => (
      program.id === updatedProgram.id ? { ...program, ...updatedProgram } : program
    )));
  };

  return (
    <DashboardLayout title="Chương trình thực tập" subtitle="Tổ chức chương trình theo phòng ban">
      <div className="space-y-6">
        <div className="grid gap-6 xl:grid-cols-[0.8fr_1.2fr]">
          <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
            <div className="flex items-center gap-3">
              <div className="rounded-xl bg-indigo-50 p-2.5 text-indigo-600"><Plus className="h-5 w-5" /></div>
              <div><h2 className="font-bold text-slate-900">Tạo chương trình</h2><p className="mt-0.5 text-xs text-slate-500">Gắn chương trình với một phòng ban.</p></div>
            </div>
            <form onSubmit={createProgram} className="mt-5 space-y-3.5">
              <label className="block text-xs font-semibold text-slate-700">Tên chương trình
                <input className={fieldClass} value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} minLength={2} maxLength={150} required />
              </label>
              <label className="block text-xs font-semibold text-slate-700">Phòng ban
                <select className={fieldClass} value={form.departmentId} onChange={(event) => setForm({ ...form, departmentId: event.target.value })} required>
                  <option value="">Chọn phòng ban</option>
                  {departments.map((department) => <option key={department.id} value={department.id}>{department.name}</option>)}
                </select>
              </label>
              <label className="block text-xs font-semibold text-slate-700">Mô tả
                <textarea className={`${fieldClass} min-h-20 resize-y`} value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} maxLength={1000} />
              </label>
              <button disabled={savingProgram || departments.length === 0} className="inline-flex w-full items-center justify-center gap-2 rounded-xl bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-indigo-500 disabled:opacity-50">
                <Plus className="h-4 w-4" />{savingProgram ? 'Đang lưu...' : 'Tạo chương trình'}
              </button>
              {departments.length === 0 && <p className="text-xs text-amber-700">Tạo phòng ban trước khi tạo chương trình.</p>}
            </form>
          </section>

          <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
            <div className="flex items-center gap-3">
              <div className="rounded-xl bg-sky-50 p-2.5 text-sky-600"><Building2 className="h-5 w-5" /></div>
              <div><h2 className="font-bold text-slate-900">Phòng ban</h2><p className="mt-0.5 text-xs text-slate-500">Danh mục dùng để phân loại chương trình.</p></div>
            </div>
            <form onSubmit={addDepartment} className="mt-5 grid gap-3 sm:grid-cols-[1fr_1.2fr_auto] sm:items-end">
              <label className="block text-xs font-semibold text-slate-700">Tên phòng ban
                <input className={fieldClass} value={departmentName} onChange={(event) => setDepartmentName(event.target.value)} minLength={2} maxLength={100} required />
              </label>
              <label className="block text-xs font-semibold text-slate-700">Mô tả
                <input className={fieldClass} value={departmentDescription} onChange={(event) => setDepartmentDescription(event.target.value)} maxLength={500} />
              </label>
              <button disabled={savingDepartment} className="rounded-xl border border-slate-200 px-4 py-2.5 text-sm font-semibold text-slate-700 hover:bg-slate-50 disabled:opacity-50">
                {savingDepartment ? 'Đang lưu...' : 'Thêm phòng ban'}
              </button>
            </form>
            <div className="mt-4 flex flex-wrap gap-2">
              {departments.map((department) => <span key={department.id} className="rounded-full bg-slate-100 px-3 py-1.5 text-xs font-medium text-slate-700">{department.name}</span>)}
              {departments.length === 0 && <span className="text-xs text-slate-400">Chưa có phòng ban.</span>}
            </div>
          </section>
        </div>

        <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
          <div className="flex items-center justify-between border-b border-slate-100 px-5 py-4 sm:px-6">
            <div><h2 className="font-bold text-slate-900">Danh sách chương trình</h2><p className="mt-0.5 text-xs text-slate-500">{programs.length} chương trình</p></div>
            <button type="button" onClick={() => { setLoading(true); load(); }} className="rounded-lg p-2 text-slate-500 hover:bg-slate-100" aria-label="Tải lại">
              <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
            </button>
          </div>
          {loading ? <p className="px-6 py-10 text-center text-sm text-slate-500">Đang tải...</p> : programs.length === 0 ? <p className="px-6 py-10 text-center text-sm text-slate-500">Chưa có chương trình thực tập.</p> : (
            <div className="overflow-x-auto">
              <table className="w-full min-w-[900px] text-left text-sm">
                <thead className="bg-slate-50 text-xs uppercase text-slate-500"><tr><th className="px-6 py-3">Chương trình</th><th className="px-6 py-3">Phòng ban</th><th className="px-6 py-3">Mô tả</th><th className="px-6 py-3">Thời gian</th></tr></thead>
                <tbody className="divide-y divide-slate-100">{programs.map((program) => (
                  <tr key={program.id} className="hover:bg-slate-50/70">
                    <td className="px-6 py-4 font-semibold text-slate-900">{program.name}</td>
                    <td className="px-6 py-4 text-slate-700">{program.departmentName}</td>
                    <td className="px-6 py-4 text-slate-600">{program.description || '—'}</td>
                    <td className="px-6 py-4"><ProgramDatesEditor key={`${program.id}-${program.startDate || ''}-${program.endDate || ''}`} program={program} onSaved={updateProgramDates} onAssign={setProgramToAssign} /></td>
                  </tr>
                ))}</tbody>
              </table>
            </div>
          )}
        </section>
      </div>
      {programToAssign && (
        <AssignStudentToProgramModal
          key={programToAssign.id}
          program={programToAssign}
          isOpen
          onClose={() => setProgramToAssign(null)}
        />
      )}
    </DashboardLayout>
  );
};

export default InternshipProgramsView;
