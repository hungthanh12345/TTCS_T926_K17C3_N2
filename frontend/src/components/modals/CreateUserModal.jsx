import React, { useState } from 'react';
import Modal from '../common/Modal';
import { UserPlus, Mail, Lock, Shield, Eye, EyeOff, Loader2 } from 'lucide-react';
import userService from '../../services/userService';
import toast from 'react-hot-toast';

export const CreateUserModal = ({ isOpen, onClose, onSuccess }) => {
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    role: 'ROLE_HR',
  });
  const [showPassword, setShowPassword] = useState(false);
  const [errors, setErrors] = useState({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  const validate = () => {
    const errs = {};
    if (!formData.email.trim()) {
      errs.email = 'Vui lòng nhập địa chỉ email';
    } else if (!/\S+@\S+\.\S+/.test(formData.email)) {
      errs.email = 'Địa chỉ email không đúng định dạng';
    }

    if (!formData.password) {
      errs.password = 'Vui lòng nhập mật khẩu khởi tạo';
    } else if (formData.password.length < 6) {
      errs.password = 'Mật khẩu phải có ít nhất 6 ký tự';
    }

    if (!formData.role) {
      errs.role = 'Vui lòng chọn vai trò cho tài khoản';
    }

    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validate()) return;

    setIsSubmitting(true);
    try {
      await userService.createUser({
        email: formData.email,
        password: formData.password,
        role: formData.role,
      });

      toast.success(`Đã tạo thành công tài khoản: ${formData.email}!`);
      // Đặt lại form
      setFormData({
        email: '',
        password: '',
        role: 'ROLE_HR',
      });
      setErrors({});
      onClose();
      if (onSuccess) onSuccess();
    } catch (err) {
      toast.error(err.message || 'Không thể tạo người dùng');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Khởi Tạo Tài Khoản Người Dùng Mới"
      subtitle="Cấp phát tài khoản phân quyền cho Chuyên viên HR, Mentor hoặc Sinh viên."
      icon={UserPlus}
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        {/* Email */}
        <div>
          <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
            Địa chỉ Email Đăng nhập <span className="text-rose-500">*</span>
          </label>
          <div className="relative rounded-xl shadow-xs">
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
              placeholder="vidu@ictu.edu.vn"
              className={`w-full pl-10 pr-3.5 py-2.5 text-sm rounded-xl border bg-white focus:outline-none focus:ring-2 transition-all ${
                errors.email
                  ? 'border-rose-300 focus:ring-rose-400 focus:border-rose-400'
                  : 'border-slate-300 focus:ring-indigo-500 focus:border-indigo-500'
              }`}
            />
          </div>
          {errors.email && (
            <p className="mt-1 text-xs text-rose-500 font-medium">{errors.email}</p>
          )}
        </div>

        {/* Mật khẩu */}
        <div>
          <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
            Mật khẩu Khởi tạo <span className="text-rose-500">*</span>
          </label>
          <div className="relative rounded-xl shadow-xs">
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
              className={`w-full pl-10 pr-10 py-2.5 text-sm rounded-xl border bg-white focus:outline-none focus:ring-2 transition-all ${
                errors.password
                  ? 'border-rose-300 focus:ring-rose-400 focus:border-rose-400'
                  : 'border-slate-300 focus:ring-indigo-500 focus:border-indigo-500'
              }`}
            />
            <button
              type="button"
              onClick={() => setShowPassword(!showPassword)}
              className="absolute inset-y-0 right-0 pr-3.5 flex items-center text-slate-400 hover:text-slate-600"
            >
              {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
            </button>
          </div>
          {errors.password && (
            <p className="mt-1 text-xs text-rose-500 font-medium">{errors.password}</p>
          )}
        </div>

        {/* Phân quyền Vai trò */}
        <div>
          <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
            Vai Trò Hệ Thống (RBAC) <span className="text-rose-500">*</span>
          </label>
          <div className="relative rounded-xl shadow-xs">
            <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400">
              <Shield className="w-4 h-4" />
            </div>
            <select
              value={formData.role}
              onChange={(e) => setFormData({ ...formData, role: e.target.value })}
              className="w-full pl-10 pr-8 py-2.5 text-sm rounded-xl border border-slate-300 bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-all font-medium text-slate-800"
            >
              <option value="ROLE_HR">ROLE_HR — Quản lý Nhân sự (HR Manager)</option>
              <option value="ROLE_MENTOR">ROLE_MENTOR — Mentor Doanh nghiệp</option>
              <option value="ROLE_STUDENT">ROLE_STUDENT — Sinh viên Thực tập</option>
            </select>
          </div>
          <p className="mt-1 text-[11px] text-slate-500">
            Chỉ Quản trị viên (Super Admin) mới có quyền cấp phát và định danh vai trò cho người dùng.
          </p>
        </div>

        {/* Nút thao tác */}
        <div className="pt-4 flex items-center justify-end gap-3 border-t border-slate-100 mt-6">
          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            className="px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100 rounded-xl transition-colors cursor-pointer"
          >
            Hủy Bỏ
          </button>
          <button
            type="submit"
            disabled={isSubmitting}
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold text-white bg-indigo-600 hover:bg-indigo-700 active:scale-98 transition-all shadow-md shadow-indigo-200 disabled:opacity-50 cursor-pointer"
          >
            {isSubmitting ? (
              <>
                <Loader2 className="w-4 h-4 animate-spin" />
                <span>Đang Khởi Tạo...</span>
              </>
            ) : (
              <>
                <UserPlus className="w-4 h-4" />
                <span>Xác Nhận Tạo Tài Khoản</span>
              </>
            )}
          </button>
        </div>
      </form>
    </Modal>
  );
};

export default CreateUserModal;
