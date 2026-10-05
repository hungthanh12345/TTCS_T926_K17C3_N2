import React, { useEffect, useState } from 'react';
import Modal from '../common/Modal';
import { UserPlus, Hash, User, Phone, School, BookOpen, Loader2 } from 'lucide-react';
import studentService from '../../services/studentService';
import toast from 'react-hot-toast';

const ACCOUNT_STATUS_LABELS = {
  ACTIVE: 'Đang hoạt động',
  INACTIVE: 'Không hoạt động',
  LOCKED: 'Đã khóa',
  PENDING_APPROVAL: 'Đang chờ duyệt',
  REJECTED: 'Đã từ chối',
};

export const AddStudentModal = ({ isOpen, onClose, onSuccess }) => {
  const [formData, setFormData] = useState({
    userId: '',
    studentCode: '',
    fullName: '',
    phone: '',
    university: '',
    major: '',
  });
  const [errors, setErrors] = useState({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [accountLinks, setAccountLinks] = useState(null);
  const [isLoadingAccounts, setIsLoadingAccounts] = useState(false);

  useEffect(() => {
    if (!isOpen) return undefined;

    let active = true;
    setIsLoadingAccounts(true);
    studentService.getStudentAccountLinks()
      .then((summary) => { if (active) setAccountLinks(summary); })
      .catch((error) => {
        if (active) {
          setAccountLinks(null);
          toast.error(error.message || 'Không thể tải tài khoản sinh viên.');
        }
      })
      .finally(() => { if (active) setIsLoadingAccounts(false); });

    return () => { active = false; };
  }, [isOpen]);

  const validate = () => {
    const errs = {};
    if (!formData.userId) {
      errs.userId = 'Vui lòng chọn tài khoản ROLE_STUDENT đã tồn tại';
    }
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
      errs.university = 'Vui lòng nhập trường đại học';
    }

    if (!formData.major.trim()) {
      errs.major = 'Vui lòng nhập chuyên ngành đào tạo';
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
      toast.success(`Đã liên kết tài khoản với hồ sơ sinh viên: ${formData.fullName}!`);
      setFormData({
        userId: '',
        studentCode: '',
        fullName: '',
        phone: '',
        university: '',
        major: '',
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
      title="Liên Kết Tài Khoản Sinh Viên"
      subtitle="Chọn tài khoản ROLE_STUDENT có sẵn rồi bổ sung hồ sơ. Thao tác này không tạo tài khoản mới."
      icon={UserPlus}
      maxWidth="max-w-2xl"
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label htmlFor="student-account" className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
            Tài Khoản Sinh Viên <span className="text-rose-500">*</span>
          </label>
          <select
            id="student-account"
            value={formData.userId}
            disabled={isLoadingAccounts || !accountLinks?.accounts?.length}
            onChange={(event) => {
              const selected = accountLinks?.accounts?.find((account) => String(account.userId) === event.target.value);
              if (selected?.hasStudentProfile) {
                setFormData((current) => ({ ...current, userId: '' }));
                toast.error('Tài khoản này đã có hồ sơ sinh viên');
                return;
              }
              setFormData((current) => ({ ...current, userId: event.target.value }));
              if (errors.userId) setErrors((current) => ({ ...current, userId: null }));
            }}
            className={`w-full rounded-xl border bg-white px-3.5 py-2.5 text-sm focus:outline-none focus:ring-2 ${errors.userId ? 'border-rose-300 focus:ring-rose-400' : 'border-slate-300 focus:ring-indigo-500'}`}
          >
            <option value="">{isLoadingAccounts ? 'Đang tải tài khoản...' : 'Chọn email tài khoản sinh viên'}</option>
            {(accountLinks?.accounts || []).map((account) => (
              <option key={account.userId} value={account.userId}>
                {account.email}{account.hasStudentProfile ? ' — Đã có hồ sơ sinh viên' : ` — ${ACCOUNT_STATUS_LABELS[account.status] || 'Không xác định'}`}
              </option>
            ))}
          </select>
          {errors.userId && <p className="mt-1 text-xs text-rose-500 font-medium">{errors.userId}</p>}
          {accountLinks && accountLinks.unlinkedAccountCount === 0 && (
            <p className="mt-1.5 text-xs text-amber-700">
              Không có tài khoản ROLE_STUDENT nào đang chờ liên kết. Sinh viên cần đăng ký tài khoản trước.
            </p>
          )}
          {accountLinks?.unlinkedAccountCount > 0 && (
            <p className="mt-1.5 text-xs text-slate-500">
              {accountLinks.unlinkedAccountCount} tài khoản chưa có hồ sơ; tài khoản đã liên kết sẽ bị từ chối nếu gửi lại.
            </p>
          )}
        </div>

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
            <input
              type="text"
              value={formData.university}
              onChange={(e) => {
                setFormData({ ...formData, university: e.target.value });
                if (errors.university) setErrors({ ...errors, university: null });
              }}
              maxLength={150}
              placeholder="Nhập trường đại học"
              className={`w-full pl-10 pr-3.5 py-2.5 text-sm rounded-xl border bg-white focus:outline-none focus:ring-2 transition-all text-slate-800 ${errors.university ? 'border-rose-300 focus:ring-rose-400 focus:border-rose-400' : 'border-slate-300 focus:ring-indigo-500 focus:border-indigo-500'}`}
            />
          </div>
          {errors.university && <p className="mt-1 text-xs font-medium text-rose-500">{errors.university}</p>}
        </div>

        {/* Chuyên ngành */}
        <div className="grid grid-cols-1 gap-4">
          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
              Chuyên Ngành Đào Tạo <span className="text-rose-500">*</span>
            </label>
            <div className="relative rounded-xl shadow-xs">
              <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400">
                <BookOpen className="w-4 h-4" />
              </div>
              <input
                type="text"
                value={formData.major}
                onChange={(e) => {
                  setFormData({ ...formData, major: e.target.value });
                  if (errors.major) setErrors({ ...errors, major: null });
                }}
                maxLength={100}
                placeholder="Nhập chuyên ngành"
                className={`w-full pl-10 pr-3.5 py-2.5 text-sm rounded-xl border bg-white focus:outline-none focus:ring-2 transition-all text-slate-800 ${errors.major ? 'border-rose-300 focus:ring-rose-400 focus:border-rose-400' : 'border-slate-300 focus:ring-indigo-500 focus:border-indigo-500'}`}
              />
            </div>
            {errors.major && <p className="mt-1 text-xs font-medium text-rose-500">{errors.major}</p>}
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
            disabled={isSubmitting || isLoadingAccounts || !formData.userId}
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
