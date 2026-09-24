import React, { useState } from 'react';
import Modal from '../common/Modal';
import { UserPlus, Hash, User, Phone, School, BookOpen, Mail, Calendar, Loader2 } from 'lucide-react';
import studentService from '../../services/studentService';
import toast from 'react-hot-toast';

export const AddStudentModal = ({ isOpen, onClose, onSuccess }) => {
  const [formData, setFormData] = useState({
    studentCode: '',
    fullName: '',
    phone: '',
    email: '',
    university: 'Đại học Công nghệ Thông tin & Truyền thông — ĐHTN',
    major: 'Kỹ thuật Phần mềm',
    internshipPeriod: 'Kỳ Thu 2026 (09/2026 - 12/2026)',
  });
  const [errors, setErrors] = useState({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  const universities = [
    'Đại học Công nghệ Thông tin & Truyền thông — ĐHTN',
    'Đại học Bách Khoa Hà Nội (HUST)',
    'Đại học Công nghệ — ĐHQGHN (VNU-UET)',
    'Đại học FPT Hà Nội',
    'Học viện Công nghệ Bưu chính Viễn thông (PTIT)',
    'Học viện Kỹ thuật Mật mã (ACT)',
  ];

  const majors = [
    'Kỹ thuật Phần mềm',
    'Khoa học Máy tính',
    'Hệ thống Thông tin Quản lý',
    'An toàn Thông tin & An ninh Mạng',
    'Trí tuệ Nhân tạo & Khoa học Dữ liệu',
    'Mạng Máy tính & Truyền thông Dữ liệu',
  ];

  const validate = () => {
    const errs = {};
    if (!formData.studentCode.trim()) {
      errs.studentCode = 'Mã sinh viên là bắt buộc';
    } else if (formData.studentCode.trim().length < 4) {
      errs.studentCode = 'Mã sinh viên tối thiểu 4 ký tự';
    }

    if (!formData.fullName.trim()) {
      errs.fullName = 'Họ và tên sinh viên là bắt buộc';
    }

    if (!formData.phone.trim()) {
      errs.phone = 'Số điện thoại liên hệ là bắt buộc';
    } else if (!/^[0-9+\s-]{8,15}$/.test(formData.phone.trim())) {
      errs.phone = 'Số điện thoại không hợp lệ';
    }

    if (!formData.university.trim()) {
      errs.university = 'Vui lòng chọn trường đại học';
    }

    if (!formData.major.trim()) {
      errs.major = 'Vui lòng chọn chuyên ngành đào tạo';
    }

    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validate()) return;

    setIsSubmitting(true);
    try {
      await studentService.createStudent(formData);
      toast.success(`Đã thêm mới hồ sơ sinh viên: ${formData.fullName}!`);
      setFormData({
        studentCode: '',
        fullName: '',
        phone: '',
        email: '',
        university: 'Đại học Công nghệ Thông tin & Truyền thông — ĐHTN',
        major: 'Kỹ thuật Phần mềm',
        internshipPeriod: 'Kỳ Thu 2026 (09/2026 - 12/2026)',
      });
      setErrors({});
      onClose();
      if (onSuccess) onSuccess();
    } catch (err) {
      toast.error(err.message || 'Lỗi khi thêm hồ sơ sinh viên');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Thêm Mới Hồ Sơ Sinh Viên Thực Tập"
      subtitle="Nhập thông tin ứng viên để quản lý theo dõi và ghép nối Mentor hướng dẫn."
      icon={UserPlus}
      maxWidth="max-w-2xl"
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {/* Mã sinh viên */}
          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
              Mã Sinh Viên (Student Code) <span className="text-rose-500">*</span>
            </label>
            <div className="relative rounded-xl shadow-xs">
              <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400">
                <Hash className="w-4 h-4" />
              </div>
              <input
                type="text"
                value={formData.studentCode}
                onChange={(e) => {
                  setFormData({ ...formData, studentCode: e.target.value.toUpperCase() });
                  if (errors.studentCode) setErrors({ ...errors, studentCode: null });
                }}
                placeholder="VD: DTC245180069"
                className={`w-full pl-10 pr-3.5 py-2.5 text-sm uppercase rounded-xl border bg-white focus:outline-none focus:ring-2 transition-all font-mono ${
                  errors.studentCode
                    ? 'border-rose-300 focus:ring-rose-400 focus:border-rose-400'
                    : 'border-slate-300 focus:ring-indigo-500 focus:border-indigo-500'
                }`}
              />
            </div>
            {errors.studentCode && (
              <p className="mt-1 text-xs text-rose-500 font-medium">{errors.studentCode}</p>
            )}
          </div>

          {/* Họ và tên */}
          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
              Họ và Tên Đầy Đủ <span className="text-rose-500">*</span>
            </label>
            <div className="relative rounded-xl shadow-xs">
              <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400">
                <User className="w-4 h-4" />
              </div>
              <input
                type="text"
                value={formData.fullName}
                onChange={(e) => {
                  setFormData({ ...formData, fullName: e.target.value });
                  if (errors.fullName) setErrors({ ...errors, fullName: null });
                }}
                placeholder="VD: Nguyễn Thành Hưng"
                className={`w-full pl-10 pr-3.5 py-2.5 text-sm rounded-xl border bg-white focus:outline-none focus:ring-2 transition-all font-medium ${
                  errors.fullName
                    ? 'border-rose-300 focus:ring-rose-400 focus:border-rose-400'
                    : 'border-slate-300 focus:ring-indigo-500 focus:border-indigo-500'
                }`}
              />
            </div>
            {errors.fullName && (
              <p className="mt-1 text-xs text-rose-500 font-medium">{errors.fullName}</p>
            )}
          </div>

          {/* Điện thoại */}
          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
              Số Điện Thoại Liên Hệ <span className="text-rose-500">*</span>
            </label>
            <div className="relative rounded-xl shadow-xs">
              <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400">
                <Phone className="w-4 h-4" />
              </div>
              <input
                type="tel"
                value={formData.phone}
                onChange={(e) => {
                  setFormData({ ...formData, phone: e.target.value });
                  if (errors.phone) setErrors({ ...errors, phone: null });
                }}
                placeholder="0987 654 321"
                className={`w-full pl-10 pr-3.5 py-2.5 text-sm rounded-xl border bg-white focus:outline-none focus:ring-2 transition-all ${
                  errors.phone
                    ? 'border-rose-300 focus:ring-rose-400 focus:border-rose-400'
                    : 'border-slate-300 focus:ring-indigo-500 focus:border-indigo-500'
                }`}
              />
            </div>
            {errors.phone && (
              <p className="mt-1 text-xs text-rose-500 font-medium">{errors.phone}</p>
            )}
          </div>

          {/* Email */}
          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
              Hòm Thư Email Sinh Viên
            </label>
            <div className="relative rounded-xl shadow-xs">
              <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400">
                <Mail className="w-4 h-4" />
              </div>
              <input
                type="email"
                value={formData.email}
                onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                placeholder="sinhvien@ictu.edu.vn"
                className="w-full pl-10 pr-3.5 py-2.5 text-sm rounded-xl border border-slate-300 bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-all"
              />
            </div>
          </div>
        </div>

        {/* Trường Đại học */}
        <div>
          <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
            Trường Đại Học / Cơ Sở Đào Tạo <span className="text-rose-500">*</span>
          </label>
          <div className="relative rounded-xl shadow-xs">
            <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400">
              <School className="w-4 h-4" />
            </div>
            <select
              value={formData.university}
              onChange={(e) => setFormData({ ...formData, university: e.target.value })}
              className="w-full pl-10 pr-8 py-2.5 text-sm rounded-xl border border-slate-300 bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-all text-slate-800"
            >
              {universities.map((u) => (
                <option key={u} value={u}>
                  {u}
                </option>
              ))}
            </select>
          </div>
        </div>

        {/* Chuyên ngành & Kỳ thực tập */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
              Chuyên Ngành Đào Tạo <span className="text-rose-500">*</span>
            </label>
            <div className="relative rounded-xl shadow-xs">
              <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400">
                <BookOpen className="w-4 h-4" />
              </div>
              <select
                value={formData.major}
                onChange={(e) => setFormData({ ...formData, major: e.target.value })}
                className="w-full pl-10 pr-8 py-2.5 text-sm rounded-xl border border-slate-300 bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-all text-slate-800"
              >
                {majors.map((m) => (
                  <option key={m} value={m}>
                    {m}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
              Kỳ Thực Tập Sinh
            </label>
            <div className="relative rounded-xl shadow-xs">
              <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400">
                <Calendar className="w-4 h-4" />
              </div>
              <input
                type="text"
                value={formData.internshipPeriod}
                onChange={(e) => setFormData({ ...formData, internshipPeriod: e.target.value })}
                placeholder="Kỳ Thu 2026 (09/2026 - 12/2026)"
                className="w-full pl-10 pr-3.5 py-2.5 text-sm rounded-xl border border-slate-300 bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-all text-slate-800"
              />
            </div>
          </div>
        </div>

        {/* Nút Hành Động */}
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
                <span>Đang Ghi Nhận...</span>
              </>
            ) : (
              <>
                <UserPlus className="w-4 h-4" />
                <span>Lưu Hồ Sơ Sinh Viên</span>
              </>
            )}
          </button>
        </div>
      </form>
    </Modal>
  );
};

export default AddStudentModal;
