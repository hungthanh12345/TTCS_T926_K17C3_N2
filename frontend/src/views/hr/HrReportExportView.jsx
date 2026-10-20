import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { toast } from 'react-hot-toast';
import { Download, FileSpreadsheet, FileText, Loader2, RefreshCw } from 'lucide-react';
import { useSearchParams } from 'react-router-dom';
import DashboardLayout from '../../components/layout/DashboardLayout';
import programCompletionService from '../../services/programCompletionService';
import reportExportService from '../../services/reportExportService';
import studentDemographicsService from '../../services/studentDemographicsService';

const reportTypes = [
  { value: 'aggregate', label: 'Báo cáo tổng hợp' },
  { value: 'program-completion', label: 'Tỷ lệ hoàn thành chương trình' },
  { value: 'student-demographics', label: 'Thống kê theo trường/ngành' },
  { value: 'mentor-workload', label: 'Khối lượng hướng dẫn của Mentor' },
];

const normalizeReportType = (value) => reportTypes.some((item) => item.value === value)
  ? value
  : 'aggregate';

const HrReportExportView = () => {
  const [searchParams] = useSearchParams();
  const [reportType, setReportType] = useState(() => normalizeReportType(searchParams.get('reportType')));
  const [programId, setProgramId] = useState(() => searchParams.get('programId') || '');
  const [university, setUniversity] = useState(() => searchParams.get('university') || '');
  const [major, setMajor] = useState(() => searchParams.get('major') || '');
  const [programs, setPrograms] = useState([]);
  const [demographics, setDemographics] = useState(null);
  const [programError, setProgramError] = useState('');
  const [demographicsError, setDemographicsError] = useState('');
  const [loadingFilters, setLoadingFilters] = useState(true);
  const [exporting, setExporting] = useState('');
  const [exportError, setExportError] = useState('');

  const fetchFilterOptions = useCallback(() => Promise.allSettled([
      programCompletionService.getDashboard(),
      studentDemographicsService.get(),
  ]), []);

  const applyFilterOptions = useCallback(([programResult, demographicsResult]) => {
    if (programResult.status === 'fulfilled') {
      setPrograms(programResult.value.programs || []);
    } else {
      setPrograms([]);
      setProgramError(programResult.reason?.message || 'Không thể tải danh sách chương trình.');
    }

    if (demographicsResult.status === 'fulfilled') {
      setDemographics(demographicsResult.value);
    } else {
      setDemographics(null);
      setDemographicsError(demographicsResult.reason?.message || 'Không thể tải bộ lọc trường/ngành.');
    }

    setLoadingFilters(false);
  }, []);

  useEffect(() => {
    let active = true;
    void fetchFilterOptions().then((results) => {
      if (active) applyFilterOptions(results);
    });
    return () => { active = false; };
  }, [applyFilterOptions, fetchFilterOptions]);

  const reloadFilters = () => {
    setLoadingFilters(true);
    setProgramError('');
    setDemographicsError('');
    void fetchFilterOptions().then(applyFilterOptions);
  };

  const includesCompletion = reportType === 'aggregate' || reportType === 'program-completion';
  const includesDemographics = reportType === 'aggregate' || reportType === 'student-demographics';
  const filterError = (includesCompletion && programError) || (includesDemographics && demographicsError);
  const filteredDemographicCount = useMemo(() => (demographics?.items || [])
    .filter((item) => (!university || item.university === university) && (!major || item.major === major))
    .reduce((total, item) => total + item.studentCount, 0), [demographics, university, major]);

  const handleExport = async (format) => {
    if (loadingFilters || exporting || filterError) return;

    setExporting(format);
    setExportError('');
    try {
      const filters = { reportType };
      if (includesCompletion && programId) filters.programId = Number(programId);
      if (includesDemographics && university) filters.university = university;
      if (includesDemographics && major) filters.major = major;

      const blob = await reportExportService.download(format, filters);
      const objectUrl = URL.createObjectURL(blob);
      const link = document.createElement('a');
      const extension = format === 'excel' ? 'xlsx' : 'pdf';
      link.href = objectUrl;
      link.download = `bao-cao-thuc-tap-${new Date().toISOString().replaceAll(':', '-')}.${extension}`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.setTimeout(() => URL.revokeObjectURL(objectUrl), 1000);
      toast.success(`Đã tạo và tải báo cáo ${extension.toUpperCase()}.`);
    } catch (failure) {
      const message = failure.message || 'Không thể xuất báo cáo. Vui lòng thử lại sau.';
      setExportError(message);
      toast.error(message);
    } finally {
      setExporting('');
    }
  };

  return (
    <DashboardLayout
      title="Xuất báo cáo thực tập"
      subtitle="Chọn nội dung và bộ lọc phù hợp, sau đó tải báo cáo Excel hoặc PDF."
    >
      <div className="space-y-5">
        {filterError && (
          <div role="alert" className="flex items-center justify-between gap-3 rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800">
            <span>{filterError}</span>
            <button type="button" onClick={reloadFilters} disabled={loadingFilters} className="inline-flex items-center gap-1 font-semibold disabled:opacity-50">
              <RefreshCw className="h-4 w-4" /> Thử lại
            </button>
          </div>
        )}
        {exportError && <p role="alert" className="rounded-xl border border-rose-200 bg-rose-50 p-3 text-sm text-rose-800">{exportError}</p>}

        {loadingFilters ? (
          <div role="status" className="flex items-center gap-2 rounded-2xl border border-slate-200 bg-white p-6 text-sm text-slate-500">
            <Loader2 className="h-5 w-5 animate-spin" /> Đang tải danh sách bộ lọc...
          </div>
        ) : (
          <>
            <section className="grid gap-4 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm md:grid-cols-2">
              <label className="text-xs font-semibold text-slate-600">
                Loại báo cáo
                <select value={reportType} onChange={(event) => { setReportType(event.target.value); setExportError(''); }} className="mt-1 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 font-normal">
                  {reportTypes.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}
                </select>
              </label>

              {includesCompletion && (
                <label className="text-xs font-semibold text-slate-600">
                  Chương trình thực tập
                  <select value={programId} onChange={(event) => { setProgramId(event.target.value); setExportError(''); }} className="mt-1 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 font-normal">
                    <option value="">Tất cả chương trình</option>
                    {programs.map((program) => <option key={program.programId} value={program.programId}>{program.programName}</option>)}
                  </select>
                </label>
              )}

              {includesDemographics && (
                <>
                  <label className="text-xs font-semibold text-slate-600">
                    Trường
                    <select value={university} onChange={(event) => { setUniversity(event.target.value); setExportError(''); }} className="mt-1 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 font-normal">
                      <option value="">Tất cả trường</option>
                      {(demographics?.universities || []).map((value) => <option key={value} value={value}>{value}</option>)}
                    </select>
                  </label>
                  <label className="text-xs font-semibold text-slate-600">
                    Ngành
                    <select value={major} onChange={(event) => { setMajor(event.target.value); setExportError(''); }} className="mt-1 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 font-normal">
                      <option value="">Tất cả ngành</option>
                      {(demographics?.majors || []).map((value) => <option key={value} value={value}>{value}</option>)}
                    </select>
                  </label>
                </>
              )}

              {reportType === 'student-demographics' && (
                <p className="self-end rounded-lg bg-indigo-50 px-4 py-3 text-sm font-semibold text-indigo-800 md:col-span-2">
                  Số hồ sơ phù hợp: {filteredDemographicCount}
                </p>
              )}
            </section>

            {includesCompletion && programs.length === 0 && !programError && (
              <p role="status" className="rounded-xl border border-slate-200 bg-white p-4 text-sm text-slate-500">Chưa có chương trình thực tập để lọc tỷ lệ hoàn thành.</p>
            )}
            {reportType === 'student-demographics' && filteredDemographicCount === 0 && !demographicsError && (
              <p role="status" className="rounded-xl border border-slate-200 bg-white p-4 text-sm text-slate-500">Không có hồ sơ phù hợp với bộ lọc này. Hệ thống sẽ thông báo và không tạo file trống.</p>
            )}

            <section className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
              <div className="text-sm text-slate-600">
                Báo cáo được tạo theo dữ liệu hiện có và ghi rõ các mục không có dữ liệu.
              </div>
              <div className="flex flex-wrap gap-2">
                <button type="button" onClick={() => void handleExport('excel')} disabled={Boolean(exporting) || Boolean(filterError)} className="inline-flex items-center gap-2 rounded-lg bg-emerald-700 px-4 py-2.5 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:opacity-50">
                  <FileSpreadsheet className="h-4 w-4" /> {exporting === 'excel' ? 'Đang xuất...' : 'Xuất Excel (.xlsx)'}
                </button>
                <button type="button" onClick={() => void handleExport('pdf')} disabled={Boolean(exporting) || Boolean(filterError)} className="inline-flex items-center gap-2 rounded-lg bg-indigo-700 px-4 py-2.5 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:opacity-50">
                  <FileText className="h-4 w-4" /> {exporting === 'pdf' ? 'Đang xuất...' : 'Xuất PDF'}
                </button>
              </div>
            </section>
            <p className="flex items-center gap-2 text-xs text-slate-500"><Download className="h-4 w-4" /> Nếu bộ lọc không có dữ liệu, hệ thống sẽ thông báo thay vì tải file trống.</p>
          </>
        )}
      </div>
    </DashboardLayout>
  );
};

export default HrReportExportView;
