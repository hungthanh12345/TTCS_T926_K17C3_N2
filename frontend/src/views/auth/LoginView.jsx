import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import {
  Mail,
  Lock,
  Eye,
  EyeOff,
  Layers,
  ArrowRight,
  ShieldCheck,
  CheckCircle2,
  Loader2,
  GraduationCap,
  Briefcase,
  Users,
  Shield,
} from 'lucide-react';

export const LoginView = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { login, resetSession } = useAuth();

  // Reset any cached session on login screen mount for clean state
  useEffect(() => {
    resetSession?.();
  }, []);

  const [formData, setFormData] = useState({
    email: '',
    password: '',
  });
  const [showPassword, setShowPassword] = useState(false);
  const [errors, setErrors] = useState({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Danh sách tài khoản trải nghiệm nhanh theo cơ sở dữ liệu thực tế
  const demoAccounts = [
    {
      role: 'ROLE_ADMIN',
      title: 'Quản trị viên',
      desc: 'Quản lý tài khoản & phân quyền',
      email: 'hung.nt.admin@gmail.com',
      password: 'Admin@123',
      icon: Shield,
      accent: 'border-rose-200 hover:border-rose-400 bg-rose-50/40 text-rose-900',
    },
    {
      role: 'ROLE_HR',
      title: 'Quản lý Nhân sự (HR)',
      desc: 'Hồ sơ thực tập sinh & phân công mentor',
      email: 'customer.hr@company.com',
      password: 'Admin@123',
      icon: Briefcase,
      accent: 'border-indigo-200 hover:border-indigo-400 bg-indigo-50/40 text-indigo-900',
    },
    {
      role: 'ROLE_MENTOR',
      title: 'Mentor Hướng dẫn',
      desc: 'Hướng dẫn & đánh giá thực tập sinh',
      email: 'tung.nk@gmail.com',
      password: 'Admin@123',
      icon: Users,
      accent: 'border-purple-200 hover:border-purple-400 bg-purple-50/40 text-purple-900',
    },
    {
      role: 'ROLE_STUDENT',
      title: 'Thực tập sinh',
      desc: 'Hồ sơ cá nhân & thông tin mentor',
      email: 'hung.dm@gmail.com',
      password: 'Admin@123',
      icon: GraduationCap,
      accent: 'border-emerald-200 hover:border-emerald-400 bg-emerald-50/40 text-emerald-900',
    },
  ];

  const executeLogin = async (credentials) => {
    setIsSubmitting(true);
    try {
      const loggedUser = await login(credentials);

      const from = location.state?.from?.pathname;
      const role = loggedUser?.role;

      // Check if previous redirected route is authorized for this role
      let canUseFrom = false;
      if (from && from !== '/login' && from !== '/') {
        if (role === 'ROLE_ADMIN') canUseFrom = true;
        else if (role === 'ROLE_HR' && from.startsWith('/hr')) canUseFrom = true;
        else if (role === 'ROLE_MENTOR' && from.startsWith('/mentor')) canUseFrom = true;
        else if (role === 'ROLE_STUDENT' && from.startsWith('/student')) canUseFrom = true;
      }

      if (canUseFrom) {
        navigate(from, { replace: true });
        return;
      }

      // Enforce default role-based routing
      switch (role) {
        case 'ROLE_ADMIN':
          navigate('/admin/users', { replace: true });
          break;
        case 'ROLE_HR':
          navigate('/hr/students', { replace: true });
          break;
        case 'ROLE_MENTOR':
          navigate('/mentor/students', { replace: true });
          break;
        case 'ROLE_STUDENT':
          navigate('/student/profile', { replace: true });
          break;
        default:
          navigate('/hr/students', { replace: true });
      }
    } catch {
      // Toast handles error notifications
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleQuickFill = (acc) => {
    setFormData({
      email: acc.email,
      password: acc.password,
    });
    setErrors({});
  };

  const validate = () => {
    const errs = {};
    if (!formData.email.trim()) {
      errs.email = 'Vui lòng nhập địa chỉ email';
    } else if (!/\S+@\S+\.\S+/.test(formData.email)) {
      errs.email = 'Định dạng email không hợp lệ';
    }

    if (!formData.password) {
      errs.password = 'Vui lòng nhập mật khẩu';
    } else if (formData.password.length < 4) {
      errs.password = 'Mật khẩu phải có ít nhất 4 ký tự';
    }

    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validate()) return;
    await executeLogin(formData);
  };

  return (
    <div className="min-h-screen flex bg-slate-950 font-sans selection:bg-indigo-500 selection:text-white">
      {/* Cột trái: Giới thiệu hệ thống với hình ảnh 3D và thương hiệu */}
      <div className="hidden lg:flex lg:w-7/12 relative bg-slate-950 overflow-hidden flex-col justify-between p-12 text-white border-r border-slate-800/80">
        {/* Nền ảnh công nghệ 3D */}
        <div className="absolute inset-0 z-0">
          <img
            src="/hero-tech.jpg"
            alt="Nền Tảng Quản Lý Thực Tập Sinh"
            className="w-full h-full object-cover object-center opacity-40 mix-blend-luminosity scale-105 transition-transform duration-1000 ease-out"
          />
          <div className="absolute inset-0 bg-gradient-to-r from-slate-950 via-slate-950/80 to-transparent" />
          <div className="absolute inset-0 bg-gradient-to-t from-slate-950 via-transparent to-slate-950/70" />
        </div>

        {/* Hiệu ứng ánh sáng ambient */}
        <div className="absolute top-1/4 left-10 w-96 h-96 bg-indigo-500/20 rounded-full blur-3xl pointer-events-none" />
        <div className="absolute bottom-10 right-10 w-80 h-80 bg-sky-500/15 rounded-full blur-3xl pointer-events-none" />

        {/* Tiêu đề Logo */}
        <div className="relative z-10 flex items-center gap-3">
          <div className="w-12 h-12 rounded-2xl bg-gradient-to-tr from-indigo-500 via-indigo-600 to-sky-400 flex items-center justify-center text-white shadow-xl shadow-indigo-500/30 ring-1 ring-white/20">
            <Layers className="w-6 h-6" />
          </div>
          <div>
            <h2 className="text-xl font-extrabold tracking-tight bg-gradient-to-r from-white via-slate-100 to-indigo-200 bg-clip-text text-transparent">
              Hệ Thống Quản Lý Thực Tập Sinh
            </h2>
            <p className="text-xs text-indigo-300 font-medium tracking-wide">
              Cổng thông tin đào tạo & thực tập sinh
            </p>
          </div>
        </div>

        {/* Nội dung trung tâm */}
        <div className="relative z-10 max-w-xl space-y-6 my-auto pt-6">
          <h1 className="text-4xl xl:text-5xl font-extrabold tracking-tight leading-[1.2] text-white">
            Nền Tảng Quản Lý & Đào Tạo Thực Tập Sinh Toàn Diện
          </h1>

          <p className="text-slate-300 text-base leading-relaxed">
            Số hóa toàn diện quy trình kết nối giữa Trường Đại học, Doanh nghiệp tiếp nhận và Thực tập sinh: quản lý hồ sơ, phân bổ Mentor hướng dẫn, theo dõi tiến độ và kiểm soát quyền truy cập RBAC đa tầng.
          </p>

          {/* Các tính năng nổi bật */}
          <div className="grid grid-cols-2 gap-3 pt-2">
            {[
              {
                title: 'Kiểm soát Phân quyền RBAC',
                desc: '4 Cấp độ: Admin, HR, Mentor, Sinh viên',
              },
              {
                title: 'Phân công Mentor Trực quan',
                desc: 'Theo dõi tải hướng dẫn & chuyên môn thực tập',
              },
              {
                title: 'Tra cứu & Lọc Nhanh Real-time',
                desc: 'Theo Trường, Chuyên ngành, Mã SV',
              },
              {
                title: 'Kiến trúc API Chuẩn Hóa',
                desc: 'Tích hợp JWT Bearer & Axios Interceptor',
              },
            ].map((item, idx) => (
              <div
                key={idx}
                className="p-3.5 rounded-2xl bg-white/[0.04] border border-white/10 backdrop-blur-md hover:bg-white/[0.07] transition-colors"
              >
                <div className="flex items-center gap-2 text-emerald-400 text-xs font-bold mb-1">
                  <CheckCircle2 className="w-4 h-4 shrink-0" />
                  <span className="text-white">{item.title}</span>
                </div>
                <p className="text-[11px] text-slate-400 pl-6 leading-relaxed">
                  {item.desc}
                </p>
              </div>
            ))}
          </div>
        </div>

        {/* Chân trang thông tin */}
        <div className="relative z-10 flex items-center justify-between text-xs text-slate-500 pt-6 border-t border-slate-800/80">
          <span className="font-medium text-slate-400">
            Trường Đại học Công nghệ Thông tin & Truyền thông — ĐHTN
          </span>
          <span>© 2026 Hệ Thống Quản Lý Thực Tập Sinh — Trường Đại học Công nghệ Thông tin & Truyền thông (ICTU)</span>
        </div>
      </div>

      {/* Cột phải: Form Đăng nhập & Lựa chọn Demo Account */}
      <div className="flex-1 flex flex-col justify-center items-center px-4 sm:px-8 lg:px-12 py-10 bg-slate-900/60 backdrop-blur-xl relative overflow-y-auto">
        <div className="max-w-md w-full my-auto space-y-6">
          {/* Logo di động */}
          <div className="lg:hidden flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-indigo-500 to-sky-400 flex items-center justify-center text-white shadow-md">
              <Layers className="w-5 h-5" />
            </div>
            <div>
              <h2 className="text-lg font-bold text-white tracking-tight">Hệ Thống Quản Lý Thực Tập Sinh</h2>
              <p className="text-xs text-slate-400">Cổng thông tin đào tạo & thực tập sinh</p>
            </div>
          </div>

          {/* Tiêu đề Form */}
          <div>
            <h2 className="text-2xl sm:text-3xl font-extrabold text-white tracking-tight">
              Đăng nhập hệ thống
            </h2>
            <p className="text-sm text-slate-400 mt-1.5 leading-relaxed">
              Vui lòng nhập email và mật khẩu hoặc chọn tài khoản mẫu bên dưới để đăng nhập.
            </p>
          </div>

          {/* Form Đăng Nhập */}
          <form onSubmit={handleSubmit} className="space-y-4">
            {/* Email Field */}
            <div>
              <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider mb-1.5">
                Email
              </label>
              <div className="relative rounded-xl">
                <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400">
                  <Mail className="w-4 h-4" />
                </div>
                <input
                  type="email"
                  value={formData.email}
                  onChange={(e) => {
                    setFormData({ ...formData, email: e.target.value });
                    if (errors.email) setErrors({ ...errors, email: null });
                  }}
                  placeholder="name@company.com"
                  className={`w-full pl-10 pr-4 py-2.5 text-sm rounded-xl border bg-slate-950/60 text-white placeholder-slate-500 focus:outline-none focus:ring-2 transition-all ${
                    errors.email
                      ? 'border-rose-500/80 focus:ring-rose-500/30'
                      : 'border-slate-700 focus:border-indigo-500 focus:ring-indigo-500/30'
                  }`}
                />
              </div>
              {errors.email && (
                <p className="mt-1 text-xs text-rose-400 font-medium">{errors.email}</p>
              )}
            </div>

            {/* Password Field */}
            <div>
              <div className="flex items-center justify-between mb-1.5">
                <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider">
                  MẬT KHẨU
                </label>
                <a
                  href="#forgot"
                  onClick={(e) => {
                    e.preventDefault();
                    alert('Tính năng khôi phục mật khẩu sẽ được hỗ trợ trong Sprint tiếp theo!');
                  }}
                  className="text-xs font-medium text-indigo-400 hover:text-indigo-300 transition-colors"
                >
                  Quên mật khẩu?
                </a>
              </div>
              <div className="relative rounded-xl">
                <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400">
                  <Lock className="w-4 h-4" />
                </div>
                <input
                  type={showPassword ? 'text' : 'password'}
                  value={formData.password}
                  onChange={(e) => {
                    setFormData({ ...formData, password: e.target.value });
                    if (errors.password) setErrors({ ...errors, password: null });
                  }}
                  placeholder="••••••••"
                  className={`w-full pl-10 pr-10 py-2.5 text-sm rounded-xl border bg-slate-950/60 text-white placeholder-slate-500 focus:outline-none focus:ring-2 transition-all ${
                    errors.password
                      ? 'border-rose-500/80 focus:ring-rose-500/30'
                      : 'border-slate-700 focus:border-indigo-500 focus:ring-indigo-500/30'
                  }`}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute inset-y-0 right-0 pr-3.5 flex items-center text-slate-400 hover:text-slate-200"
                >
                  {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                </button>
              </div>
              {errors.password && (
                <p className="mt-1 text-xs text-rose-400 font-medium">{errors.password}</p>
              )}
            </div>

            {/* Nút Đăng nhập */}
            <button
              type="submit"
              disabled={isSubmitting}
              className="w-full mt-2 inline-flex items-center justify-center gap-2 py-3 px-4 rounded-xl text-sm font-semibold text-white bg-gradient-to-r from-indigo-600 to-indigo-500 hover:from-indigo-500 hover:to-indigo-400 active:scale-98 transition-all shadow-lg shadow-indigo-600/30 disabled:opacity-50 cursor-pointer"
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" />
                  <span>Đang đăng nhập...</span>
                </>
              ) : (
                <>
                  <span>Đăng nhập</span>
                  <ArrowRight className="w-4 h-4" />
                </>
              )}
            </button>
          </form>

          {/* Khối Tài khoản Trải nghiệm Nhanh */}
          <div className="pt-5 border-t border-slate-800 notranslate" translate="no">
            <div className="flex items-center justify-between mb-3">
              <span className="text-xs font-bold text-slate-400 uppercase tracking-wider notranslate" translate="no">
                TÀI KHOẢN TRẢI NGHIỆM NHANH
              </span>
              <span className="text-[11px] font-semibold text-indigo-400 bg-indigo-500/10 px-2 py-0.5 rounded-full border border-indigo-500/20 notranslate" translate="no">
                Nhấp để điền thông tin
              </span>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-2.5 notranslate" translate="no">
              {demoAccounts.map((acc) => {
                const Icon = acc.icon;
                return (
                  <button
                    key={acc.role}
                    type="button"
                    translate="no"
                    onClick={() => handleQuickFill(acc)}
                    className="p-3 rounded-2xl border border-slate-800 bg-slate-950/70 hover:bg-slate-800/80 hover:border-slate-700 text-left transition-all group flex items-start gap-2.5 cursor-pointer notranslate"
                  >
                    <div className="w-8 h-8 rounded-lg bg-slate-800 text-indigo-400 flex items-center justify-center shrink-0 group-hover:bg-indigo-600 group-hover:text-white transition-colors">
                      <Icon className="w-4 h-4" />
                    </div>
                    <div className="min-w-0 notranslate" translate="no">
                      <p className="text-xs font-bold text-white group-hover:text-indigo-300 transition-colors truncate notranslate" translate="no">
                        {acc.title}
                      </p>
                      <p className="text-[11px] text-slate-400 truncate mt-0.5 notranslate" translate="no">
                        {acc.email}
                      </p>
                    </div>
                  </button>
                );
              })}
            </div>

            <p className="text-center text-[11px] text-slate-500 mt-4 flex items-center justify-center gap-1.5">
              <ShieldCheck className="w-3.5 h-3.5 text-emerald-400" />
              Bảo mật tiêu chuẩn JWT Token & RBAC Route Guarding
            </p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default LoginView;
