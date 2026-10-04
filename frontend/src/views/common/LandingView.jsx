import React, { useEffect, useState } from 'react';
import { ArrowRight, BriefcaseBusiness, CalendarDays, GraduationCap, Layers3 } from 'lucide-react';
import { Link } from 'react-router-dom';
import studentRegistrationService from '../../services/studentRegistrationService';

const LandingView = () => {
  const [programs, setPrograms] = useState([]);

  useEffect(() => {
    let active = true;
    studentRegistrationService.getPrograms()
      .then((items) => { if (active) setPrograms(Array.isArray(items) ? items : []); })
      .catch(() => { if (active) setPrograms([]); });
    return () => { active = false; };
  }, []);

  return (
    <main className="min-h-screen bg-slate-950 text-white">
      <header className="mx-auto flex max-w-6xl items-center justify-between px-5 py-6 sm:px-8">
        <Link to="/" className="flex items-center gap-3 font-bold"><span className="rounded-xl bg-gradient-to-br from-indigo-500 to-sky-400 p-2"><Layers3 className="h-5 w-5" /></span> Cổng thông tin thực tập</Link>
        <Link to="/login" className="rounded-lg border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-200 hover:border-indigo-400 hover:text-white">Đăng nhập</Link>
      </header>
      <section className="mx-auto grid max-w-6xl gap-10 px-5 pb-14 pt-8 sm:px-8 lg:grid-cols-[1.1fr_0.9fr] lg:items-center lg:py-20">
        <div>
          <span className="inline-flex items-center gap-2 rounded-full border border-indigo-400/30 bg-indigo-400/10 px-3 py-1.5 text-xs font-semibold text-indigo-200"><BriefcaseBusiness className="h-3.5 w-3.5" /> Chương trình thực tập</span>
          <h1 className="mt-6 max-w-2xl text-4xl font-extrabold leading-tight tracking-tight sm:text-5xl">Bắt đầu hành trình nghề nghiệp cùng chúng tôi</h1>
          <p className="mt-5 max-w-xl text-base leading-7 text-slate-300">Tạo hồ sơ, chọn chương trình phù hợp và gửi CV cùng đơn xin thực tập. HR sẽ tiếp nhận hồ sơ trực tuyến.</p>
          <div className="mt-8 flex flex-wrap gap-3">
            <Link to="/register" className="inline-flex items-center gap-2 rounded-xl bg-indigo-600 px-5 py-3 text-sm font-bold shadow-lg shadow-indigo-950/40 hover:bg-indigo-500">Đăng ký thực tập <ArrowRight className="h-4 w-4" /></Link>
            <Link to="/login" className="rounded-xl border border-slate-700 px-5 py-3 text-sm font-semibold text-slate-200 hover:bg-slate-900">Đăng nhập hệ thống</Link>
          </div>
        </div>
        <section className="rounded-3xl border border-slate-800 bg-slate-900/70 p-5 shadow-2xl sm:p-7">
          <div className="flex items-center gap-3"><span className="rounded-xl bg-sky-400/10 p-2.5 text-sky-300"><GraduationCap className="h-5 w-5" /></span><div><h2 className="font-bold">Chương trình đang mở</h2><p className="mt-1 text-xs text-slate-400">Danh sách lấy trực tiếp từ hệ thống.</p></div></div>
          <div className="mt-5 space-y-3">
            {programs.length ? programs.slice(0, 5).map((program) => (
              <article key={program.id} className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4">
                <h3 className="text-sm font-semibold">{program.name}</h3><p className="mt-1 text-xs text-slate-400">{program.departmentName}</p>
                {(program.startDate || program.endDate) && <p className="mt-2 flex items-center gap-1.5 text-xs text-slate-400"><CalendarDays className="h-3.5 w-3.5" />{program.startDate || 'Chưa đặt'} – {program.endDate || 'Chưa đặt'}</p>}
              </article>
            )) : <p className="rounded-2xl border border-dashed border-slate-700 px-4 py-8 text-center text-sm text-slate-400">Hiện chưa có chương trình được công bố.</p>}
          </div>
        </section>
      </section>
    </main>
  );
};

export default LandingView;
