import React, { useEffect, useState } from 'react';
import { Activity, CalendarDays, Database, GraduationCap, Mail, RefreshCw, Users } from 'lucide-react';
import DashboardLayout from '../../components/layout/DashboardLayout';
import systemSettingsService from '../../services/systemSettingsService';

const Metric = ({ icon: Icon, label, value, detail }) => (
  <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
    <div className="flex items-start justify-between gap-3">
      <div>
        <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">{label}</p>
        <p className="mt-2 text-3xl font-extrabold text-slate-900">{value}</p>
        <p className="mt-1 text-xs text-slate-500">{detail}</p>
      </div>
      <span className="rounded-xl bg-indigo-50 p-2.5 text-indigo-700"><Icon className="h-5 w-5" /></span>
    </div>
  </section>
);

const formatDate = (value) => value ? new Date(`${value}T00:00:00`).toLocaleDateString('vi-VN') : 'Chưa thiết lập';

const getProgramStatus = (program) => {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const start = program.startDate ? new Date(`${program.startDate}T00:00:00`) : null;
  const end = program.endDate ? new Date(`${program.endDate}T00:00:00`) : null;
  if (start && today < start) return 'Sắp diễn ra';
  if (end && today > end) return 'Đã kết thúc';
  if (start || end) return 'Đang diễn ra';
  return 'Chưa thiết lập thời gian';
};

export const SystemSettingsView = () => {
  const [settings, setSettings] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  const load = async () => {
    setIsLoading(true);
    try {
      const result = await systemSettingsService.get();
      setError('');
      setSettings(result);
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải thông tin hệ thống.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    let active = true;
    systemSettingsService.get()
      .then((result) => { if (active) setSettings(result); })
      .catch((requestError) => { if (active) setError(requestError.message || 'Không thể tải thông tin hệ thống.'); })
      .finally(() => { if (active) setIsLoading(false); });
    return () => { active = false; };
  }, []);

  return (
    <DashboardLayout title="Cài Đặt Hệ Thống" subtitle="Thông tin vận hành và chương trình thực tập đang được lưu trên hệ thống">
      {isLoading ? (
        <div role="status" className="rounded-2xl border border-slate-200 bg-white p-10 text-center text-sm text-slate-500">Đang tải dữ liệu hệ thống...</div>
      ) : error ? (
        <div role="alert" className="rounded-2xl border border-rose-200 bg-rose-50 p-5 text-sm text-rose-700">
          <p>{error}</p>
          <button type="button" onClick={() => void load()} className="mt-3 inline-flex items-center gap-2 font-semibold"><RefreshCw className="h-4 w-4" /> Thử lại</button>
        </div>
      ) : (
        <div className="space-y-5">
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <Metric icon={Users} label="Tài khoản" value={settings.accountCount} detail="Tổng số tài khoản trong cơ sở dữ liệu" />
            <Metric icon={GraduationCap} label="Hồ sơ sinh viên" value={settings.studentProfileCount} detail="Hồ sơ sinh viên đang được lưu" />
            <Metric icon={Activity} label="Hồ sơ Mentor" value={settings.mentorProfileCount} detail="Hồ sơ Mentor đang được lưu" />
            <Metric icon={CalendarDays} label="Đăng ký chờ duyệt" value={settings.pendingRegistrationCount} detail="Tài khoản Student đang chờ HR xét duyệt" />
          </div>

          <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
            <div className="flex items-center gap-3">
              <span className="rounded-xl bg-emerald-50 p-2.5 text-emerald-700"><Database className="h-5 w-5" /></span>
              <div>
                <h2 className="font-bold text-slate-900">Kết nối cơ sở dữ liệu</h2>
                <p className="mt-1 text-sm text-emerald-700">{settings.databaseStatus === 'Available' ? 'Đang hoạt động · MySQL phản hồi thành công' : 'Không khả dụng'}</p>
              </div>
            </div>
          </section>

          <div className="grid gap-5 xl:grid-cols-[minmax(0,1.4fr)_minmax(260px,0.6fr)]">
            <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
              <div className="border-b border-slate-100 px-5 py-4">
                <h2 className="font-bold text-slate-900">Chương trình thực tập</h2>
                <p className="mt-1 text-xs text-slate-500">Tên chương trình, thời gian và số sinh viên lấy từ dữ liệu nghiệp vụ.</p>
              </div>
              {settings.internshipPrograms.length === 0 ? (
                <p className="p-8 text-center text-sm text-slate-500">Chưa có chương trình thực tập được lưu.</p>
              ) : (
                <div className="divide-y divide-slate-100">
                  {settings.internshipPrograms.map((program) => (
                    <article key={program.id} className="flex flex-col gap-2 px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
                      <div className="min-w-0">
                        <h3 className="truncate text-sm font-semibold text-slate-900">{program.name}</h3>
                        <p className="mt-1 text-xs text-slate-500">{program.departmentName} · {formatDate(program.startDate)} – {formatDate(program.endDate)}</p>
                      </div>
                      <div className="flex shrink-0 items-center gap-2 text-xs text-slate-600">
                        <span className="rounded-full bg-indigo-50 px-2.5 py-1 font-semibold text-indigo-700">{getProgramStatus(program)}</span>
                        <span>{program.studentCount} sinh viên</span>
                      </div>
                    </article>
                  ))}
                </div>
              )}
            </section>

            <div className="space-y-5">
              <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
                <div className="flex items-center gap-2 text-slate-800"><Mail className="h-4 w-4 text-indigo-600" /><h2 className="text-sm font-bold">Liên hệ quản trị</h2></div>
                {settings.adminContacts.length ? (
                  <ul className="mt-3 space-y-2">{settings.adminContacts.map((email) => <li key={email} className="break-all text-sm text-slate-600">{email}</li>)}</ul>
                ) : <p className="mt-3 text-sm text-slate-500">Chưa có tài khoản Admin trong dữ liệu.</p>}
              </section>
              <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
                <h2 className="text-sm font-bold text-slate-800">Trường trong hồ sơ sinh viên</h2>
                {settings.universities.length ? (
                  <ul className="mt-3 space-y-2">{settings.universities.map((university) => <li key={university} className="text-sm text-slate-600">{university}</li>)}</ul>
                ) : <p className="mt-3 text-sm text-slate-500">Chưa có trường đại học được ghi nhận.</p>}
              </section>
            </div>
          </div>
          <p className="text-xs leading-5 text-slate-500">Trang này chỉ hiển thị cấu hình và số liệu đã có trong cơ sở dữ liệu. Năm học chung và tùy chọn thông báo chưa được lưu trong nghiệp vụ hiện tại nên không tự tạo giá trị mặc định.</p>
        </div>
      )}
    </DashboardLayout>
  );
};

export default SystemSettingsView;
