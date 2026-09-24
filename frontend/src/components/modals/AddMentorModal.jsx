import React, { useState } from 'react';
import Modal from '../common/Modal';
import { UserCheck, User, Phone, Mail, Building, Award, Loader2 } from 'lucide-react';
import mentorService from '../../services/mentorService';
import toast from 'react-hot-toast';

export const AddMentorModal = ({ isOpen, onClose, onSuccess }) => {
  const [formData, setFormData] = useState({
    fullName: '',
    phone: '',
    email: '',
    department: 'Kỹ thuật Phần mềm & Cloud',
    specialization: '',
  });
  const [errors, setErrors] = useState({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  const departments = [
    'Kỹ thuật Phần mềm & Cloud',
    'Trí tuệ Nhân tạo & Xử lý Dữ liệu lớn (AI/ML)',
    'Kiến trúc Frontend & Trải nghiệm Người dùng (UI/UX)',
    'An toàn Thông tin & Bảo mật Hạ tầng',
    'DevOps & Hệ thống Đám mây (AWS/Azure)',
    'Phát triển Ứng dụng Di động (Mobile App)',
  ];

  const validate = () => {
    const errs = {};
    if (!formData.fullName.trim()) errs.fullName = 'Họ và tên Mentor là bắt buộc';
    if (!formData.phone.trim()) errs.phone = 'Số điện thoại liên hệ là bắt buộc';
    if (!formData.department.trim()) errs.department = 'Vui lòng chọn phòng ban chuyên môn';
    if (!formData.specialization.trim()) {
      errs.specialization = 'Vui lòng nhập định hướng chuyên môn hoặc công nghệ thành thạo';
    }

    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validate()) return;

    setIsSubmitting(true);
    try {
      await mentorService.createMentor(formData);
      toast.success(`Đã thêm thành công Mentor: ${formData.fullName}!`);
      setFormData({
        fullName: '',
        phone: '',
        email: '',
        department: 'Kỹ thuật Phần mềm & Cloud',
        specialization: '',
      });
      setErrors({});
      onClose();
      if (onSuccess) onSuccess();
    } catch (err) {
      toast.error(err.message || 'Lỗi khi tạo hồ sơ Mentor');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Đăng Ký Hồ Sơ Mentor Doanh Nghiệp"
      subtitle="Thêm chuyên gia công nghệ hoặc trưởng nhóm kỹ thuật tham gia hướng dẫn sinh viên."
      icon={UserCheck}
      maxWidth="max-w-xl"
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        {/* Họ tên Mentor */}
        <div>
          <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
            Họ và Tên Mentor <span className="text-rose-500">*</span>
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
              placeholder="VD: TS. Alex Morgan, KS. Nguyễn Văn Nam..."
              className={`w-full pl-10 pr-3.5 py-2.5 text-sm rounded-xl border bg-white focus:outline-none focus:ring-2 transition-all ${
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

        {/* Email & Số điện thoại */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
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
                placeholder="+84 987 654 321"
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

          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
              Email Doanh Nghiệp
            </label>
            <div className="relative rounded-xl shadow-xs">
              <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400">
                <Mail className="w-4 h-4" />
              </div>
              <input
                type="email"
                value={formData.email}
                onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                placeholder="mentor@ictu.edu.vn"
                className="w-full pl-10 pr-3.5 py-2.5 text-sm rounded-xl border border-slate-300 bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-all"
              />
            </div>
          </div>
        </div>

        {/* Phòng ban kỹ thuật */}
        <div>
          <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
            Phòng Ban Chuyên Môn <span className="text-rose-500">*</span>
          </label>
          <div className="relative rounded-xl shadow-xs">
            <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400">
              <Building className="w-4 h-4" />
            </div>
            <select
              value={formData.department}
              onChange={(e) => setFormData({ ...formData, department: e.target.value })}
              className="w-full pl-10 pr-8 py-2.5 text-sm rounded-xl border border-slate-300 bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-all text-slate-800"
            >
              {departments.map((d) => (
                <option key={d} value={d}>
                  {d}
                </option>
              ))}
            </select>
          </div>
        </div>

        {/* Chuyên môn / Tech Stack */}
        <div>
          <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
            Chuyên Môn & Tech Stack Nổi Bật <span className="text-rose-500">*</span>
          </label>
          <div className="relative rounded-xl shadow-xs">
            <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400">
              <Award className="w-4 h-4" />
            </div>
            <input
              type="text"
              value={formData.specialization}
              onChange={(e) => {
                setFormData({ ...formData, specialization: e.target.value });
                if (errors.specialization) setErrors({ ...errors, specialization: null });
              }}
              placeholder="VD: .NET Core 8, C#, Microservices, Azure, Docker, React TypeScript..."
              className={`w-full pl-10 pr-3.5 py-2.5 text-sm rounded-xl border bg-white focus:outline-none focus:ring-2 transition-all ${
                errors.specialization
                  ? 'border-rose-300 focus:ring-rose-400 focus:border-rose-400'
                  : 'border-slate-300 focus:ring-indigo-500 focus:border-indigo-500'
              }`}
            />
          </div>
          {errors.specialization && (
            <p className="mt-1 text-xs text-rose-500 font-medium">{errors.specialization}</p>
          )}
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
                <UserCheck className="w-4 h-4" />
                <span>Lưu Hồ Sơ Mentor</span>
              </>
            )}
          </button>
        </div>
      </form>
    </Modal>
  );
};

export default AddMentorModal;
