import React, { useState } from 'react';
import toast from 'react-hot-toast';
import { messageOf } from '../../services/sprint2/common';
import US13 from '../../services/sprint2/US13';

const dateFieldClass = 'w-full rounded-lg border border-slate-200 bg-white px-2.5 py-2 text-sm text-slate-800 outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100';

const nextDate = (value) => {
  if (!value) return undefined;
  const [year, month, day] = value.split('-').map(Number);
  return new Date(Date.UTC(year, month - 1, day + 1)).toISOString().slice(0, 10);
};

const ProgramDatesEditor = ({ program, onSaved }) => {
  const [startDate, setStartDate] = useState(program.startDate || '');
  const [endDate, setEndDate] = useState(program.endDate || '');
  const [saving, setSaving] = useState(false);

  const datesValid = Boolean(startDate && endDate && endDate > startDate);
  const dateWarning = !startDate || !endDate
    ? (startDate || endDate ? 'Cần chọn cả ngày bắt đầu và ngày kết thúc.' : '')
    : endDate <= startDate ? 'Ngày kết thúc phải sau ngày bắt đầu.' : '';

  const saveDates = async (event) => {
    event.preventDefault();
    if (!datesValid) return;

    setSaving(true);
    try {
      const updatedProgram = await US13.updateProgramDates(program.id, { startDate, endDate });
      onSaved(updatedProgram);
      toast.success('Đã cập nhật thời gian chương trình.');
    } catch (error) {
      toast.error(messageOf(error, 'Không thể cập nhật thời gian chương trình.'));
    } finally {
      setSaving(false);
    }
  };

  return (
    <form onSubmit={saveDates} className="min-w-[360px] space-y-2">
      <div className="grid grid-cols-2 gap-2">
        <label className="block text-xs font-medium text-slate-600">
          Bắt đầu
          <input
            type="date"
            className={dateFieldClass}
            value={startDate}
            onChange={(event) => setStartDate(event.target.value)}
            aria-label={`Ngày bắt đầu ${program.name}`}
            required
          />
        </label>
        <label className="block text-xs font-medium text-slate-600">
          Kết thúc
          <input
            type="date"
            className={dateFieldClass}
            value={endDate}
            min={nextDate(startDate)}
            onChange={(event) => setEndDate(event.target.value)}
            aria-label={`Ngày kết thúc ${program.name}`}
            required
          />
        </label>
      </div>
      <div className="flex items-center justify-between gap-3">
        <p className={`text-xs ${dateWarning ? 'text-rose-600' : 'text-slate-500'}`} aria-live="polite">
          {dateWarning || 'Ngày kết thúc phải sau ngày bắt đầu.'}
        </p>
        <button
          type="submit"
          disabled={!datesValid || saving}
          className="shrink-0 rounded-lg bg-indigo-600 px-3 py-2 text-xs font-semibold text-white hover:bg-indigo-500 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {saving ? 'Đang lưu...' : 'Lưu ngày'}
        </button>
      </div>
    </form>
  );
};

export default ProgramDatesEditor;
