import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { FileDown, Loader2, RefreshCw } from 'lucide-react';
import { Link } from 'react-router-dom';
import DashboardLayout from '../../components/layout/DashboardLayout';
import studentDemographicsService from '../../services/studentDemographicsService';

const groupCounts = (items, field) => {
  const counts = new Map();
  items.forEach((item) => {
    counts.set(item[field], (counts.get(item[field]) || 0) + item.studentCount);
  });
  return Array.from(counts, ([name, studentCount]) => ({ name, studentCount }))
    .sort((left, right) => right.studentCount - left.studentCount || left.name.localeCompare(right.name, 'vi'));
};

const DemographicTable = ({ title, items }) => {
  const maximum = Math.max(...items.map((item) => item.studentCount), 1);

  return (
    <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
      <div className="border-b border-slate-100 p-5">
        <h2 className="text-sm font-bold text-slate-900">{title}</h2>
      </div>
      {items.length === 0 ? (
        <p role="status" className="p-6 text-center text-sm text-slate-500">Không có dữ liệu trong bộ lọc này.</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full min-w-[420px] text-left text-sm">
            <thead className="bg-slate-50 text-xs text-slate-500">
              <tr><th className="px-5 py-3 font-semibold">Nhóm</th><th className="w-1/2 px-5 py-3 font-semibold">Tỷ lệ</th><th className="px-5 py-3 text-right font-semibold">Sinh viên</th></tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {items.map((item) => (
                <tr key={item.name}>
                  <td className="px-5 py-3 font-medium text-slate-800">{item.name}</td>
                  <td className="px-5 py-3">
                    <div className="h-2 overflow-hidden rounded-full bg-slate-100" aria-label="Số lượng sinh viên">
                      <div className="h-full rounded-full bg-indigo-500" style={{ width: ((item.studentCount / maximum) * 100) + '%' }} />
                    </div>
                  </td>
                  <td className="px-5 py-3 text-right font-semibold text-slate-700">{item.studentCount}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
};

export const StudentDemographicsView = () => {
  const [report, setReport] = useState(null);
  const [selectedUniversity, setSelectedUniversity] = useState('');
  const [selectedMajor, setSelectedMajor] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    setReport(null);
    try {
      setReport(await studentDemographicsService.get());
    } catch (loadError) {
      setError(loadError.message || 'Không thể tải thống kê trường và ngành.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { void load(); }, [load]);

  const filteredItems = useMemo(() => (report?.items || []).filter((item) =>
    (!selectedUniversity || item.university === selectedUniversity) &&
    (!selectedMajor || item.major === selectedMajor)), [report, selectedUniversity, selectedMajor]);
  const universityStats = useMemo(() => groupCounts(filteredItems, 'university'), [filteredItems]);
  const majorStats = useMemo(() => groupCounts(filteredItems, 'major'), [filteredItems]);
  const filteredTotal = filteredItems.reduce((sum, item) => sum + item.studentCount, 0);
  const exportHref = useMemo(() => {
    const params = new URLSearchParams({ reportType: 'student-demographics' });
    if (selectedUniversity) params.set('university', selectedUniversity);
    if (selectedMajor) params.set('major', selectedMajor);
    return `/hr/report-export?${params.toString()}`;
  }, [selectedUniversity, selectedMajor]);

  return (
    <DashboardLayout title="Thống kê thực tập sinh theo trường/ngành" subtitle="Chỉ tính hồ sơ đã được HR duyệt; thông tin còn trống được gom vào nhóm Chưa cập nhật.">
      <div className="space-y-5">
        {error && (
          <div role="alert" className="flex items-center justify-between gap-3 rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800">
            <span>{error}</span>
            <button type="button" onClick={() => void load()} disabled={loading} className="inline-flex items-center gap-1 font-semibold disabled:opacity-50">
              <RefreshCw className="h-4 w-4" /> Thử lại
            </button>
          </div>
        )}
        {loading ? (
          <div role="status" className="flex items-center gap-2 rounded-2xl border border-slate-200 bg-white p-6 text-sm text-slate-500">
            <Loader2 className="h-5 w-5 animate-spin" /> Đang tải thống kê...
          </div>
        ) : error ? null : (
          <>
            <section className="flex flex-wrap items-end gap-3 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
              <label className="min-w-56 flex-1 text-xs font-semibold text-slate-600">
                Lọc theo trường
                <select value={selectedUniversity} onChange={(event) => setSelectedUniversity(event.target.value)} className="mt-1 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 font-normal">
                  <option value="">Tất cả trường</option>
                  {(report?.universities || []).map((university) => <option key={university} value={university}>{university}</option>)}
                </select>
              </label>
              <label className="min-w-56 flex-1 text-xs font-semibold text-slate-600">
                Lọc theo ngành
                <select value={selectedMajor} onChange={(event) => setSelectedMajor(event.target.value)} className="mt-1 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 font-normal">
                  <option value="">Tất cả ngành</option>
                  {(report?.majors || []).map((major) => <option key={major} value={major}>{major}</option>)}
                </select>
              </label>
              <p className="rounded-lg bg-indigo-50 px-4 py-2.5 text-sm font-semibold text-indigo-800">Tổng hồ sơ trong bộ lọc: {filteredTotal}</p>
              <Link to={exportHref} className="ml-auto inline-flex items-center gap-2 rounded-lg bg-slate-800 px-4 py-2.5 text-sm font-semibold text-white">
                <FileDown className="h-4 w-4" /> Tùy chọn xuất báo cáo
              </Link>
            </section>
            {report?.items?.length === 0 ? (
              <p role="status" className="rounded-2xl border border-slate-200 bg-white p-8 text-center text-sm text-slate-500">Chưa có hồ sơ thực tập sinh đã được duyệt để thống kê.</p>
            ) : filteredItems.length === 0 ? (
              <p role="status" className="rounded-2xl border border-slate-200 bg-white p-8 text-center text-sm text-slate-500">Không có hồ sơ phù hợp với bộ lọc.</p>
            ) : (
              <div className="grid gap-5 xl:grid-cols-2">
                <DemographicTable title="Số lượng theo trường" items={universityStats} />
                <DemographicTable title="Số lượng theo ngành" items={majorStats} />
              </div>
            )}
          </>
        )}
      </div>
    </DashboardLayout>
  );
};

export default StudentDemographicsView;
