import React, { useState, useEffect } from 'react';
import Modal from '../common/Modal';
import { Edit3, Hash, User, Phone, School, BookOpen, Mail, Calendar, Loader2 } from 'lucide-react';
import studentService from '../../services/studentService';
import toast from 'react-hot-toast';

export const EditStudentModal = ({ isOpen, onClose, student, onSuccess }) => {
  const [formData, setFormData] = useState({
    studentCode: '',
    fullName: '',
    phone: '',
    email: '',
    university: '',
    major: '',
    internshipPeriod: '',
  });
  const [errors, setErrors] = useState({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (student) {
      setFormData({
        studentCode: student.studentCode || '',
        fullName: student.fullName || '',
        phone: student.phone || student.phoneNumber || '',
        email: student.email || student.user?.email || '',
        university: student.university || 'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN',
        major: student.major || 'Kỹ thuật Phần mềm',
        internshipPeriod: student.internshipPeriod || 'Kỳ Thu 2026',
      });
      setErrors({});
    }
  }, [student]);

  const universities = [
    'Đại học Công nghệ Thông tin và Truyền thông — ĐHTN',
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
    'An toàn Thông tin và An ninh Mạng',
    'Trí tuệ Nhân tạo và Khoa học Dữ liệu',
    'Mạng Máy tính và Truyền thông Dữ liệu',
  ];

  const validate = () => {
    const errs = {};
    if (!formData.studentCode.trim()) errs.studentCode = 'Mã sinh viên là bắt buộc';
    if (!formData.fullName.trim()) errs.fullName = 'Họ và tên là bắt buộc';
    if (!formData.phone.trim()) errs.phone = 'Số điện thoại là bắt buộc';
    if (!formData.university.trim()) errs.university = 'Trường đại học là bắt buộc';
    if (!formData.major.trim()) errs.major = 'Chuyên ngành là bắt buộc';

    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validate() || !student) return;

    setIsSubmitting(true);
    try {
      await studentService.updateStudent(student.id, {
        ...formData,
        userId: student.userId,
        mentorId: student.mentorId,
      });
      toast.success(`Đã cập nhật hồ sơ cho ${formData.fullName}!`);
      onClose();
      if (onSuccess) onSuccess();
    } catch (err) {
      toast.error(err.message || 'Lỗi khi cập nhật hồ sơ sinh viên');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Cập Nhật Thông Tin Sinh Viên"
      subtitle={`Chỉnh sửa hồ sơ mã số: ${student?.studentCode || ''}`}
      icon={Edit3}
      maxWidth="max-w-2xl"
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {/* Mã sinh viên */}
          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
              Mã Sinh Viên <span className="text-rose-500">*</span>
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
                className="w-full pl-10 pr-3.5 py-2.5 text-sm uppercase rounded-xl border border-slate-300 bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-all font-mono"
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
                className="w-full pl-10 pr-3.5 py-2.5 text-sm rounded-xl border border-slate-300 bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-all font-medium"
              />
            </div>
            {errors.fullName && (
              <p className="mt-1 text-xs text-rose-500 font-medium">{errors.fullName}</p>
            )}
          </div>

          {/* Điện thoại */}
          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
              Số Điện Thoại <span className="text-rose-500">*</span>
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
                className="w-full pl-10 pr-3.5 py-2.5 text-sm rounded-xl border border-slate-300 bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-all"
              />
            </div>
            {errors.phone && (
              <p className="mt-1 text-xs text-rose-500 font-medium">{errors.phone}</p>
            )}
          </div>

          {/* Email */}
          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
              Hòm Thư Email
            </label>
            <div className="relative rounded-xl shadow-xs">
              <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400">
                <Mail className="w-4 h-4" />
              </div>
              <input
                type="email"
                value={formData.email}
                onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                className="w-full pl-10 pr-3.5 py-2.5 text-sm rounded-xl border border-slate-300 bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-all"
              />
            </div>
          </div>
        </div>

        {/* Trường đại học */}
        <div>
          <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
            Trường Đại Học <span className="text-rose-500">*</span>
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
              Kỳ Thực Tập
            </label>
            <div className="relative rounded-xl shadow-xs">
              <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400">
                <Calendar className="w-4 h-4" />
              </div>
              <input
                type="text"
                value={formData.internshipPeriod}
                onChange={(e) => setFormData({ ...formData, internshipPeriod: e.target.value })}
                className="w-full pl-10 pr-3.5 py-2.5 text-sm rounded-xl border border-slate-300 bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-all text-slate-800"
              />
            </div>
          </div>
        </div>

        {/* Nút hành động */}
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
                <span>Đang Lưu Thay Đổi...</span>
              </>
            ) : (
              <>
                <Edit3 className="w-4 h-4" />
                <span>Lưu Cập Nhật</span>
              </>
            )}
          </button>
        </div>
      </form>
    </Modal>
  );
};

export default EditStudentModal;
