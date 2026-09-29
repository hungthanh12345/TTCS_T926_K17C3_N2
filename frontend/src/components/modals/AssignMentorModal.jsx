import React, { useState, useEffect } from 'react';
import Modal from '../common/Modal';
import { UserCheck, User, Building, Award, Users, AlertCircle, Loader2 } from 'lucide-react';
import mentorService from '../../services/mentorService';
import studentService from '../../services/studentService';
import toast from 'react-hot-toast';

export const AssignMentorModal = ({ isOpen, onClose, student, onSuccess }) => {
  const [mentors, setMentors] = useState([]);
  const [selectedMentorId, setSelectedMentorId] = useState('');
  const [isLoadingMentors, setIsLoadingMentors] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    if (isOpen) {
      loadMentors();
      setSelectedMentorId(student?.mentorId || '');
      setError(null);
    }
  }, [isOpen, student]);

  const loadMentors = async () => {
    setIsLoadingMentors(true);
    try {
      const data = await mentorService.getMentors();
      setMentors(Array.isArray(data) ? data : []);
    } catch (err) {
      console.error('Lỗi khi tải danh bạ mentor', err);
      toast.error('Không thể tải danh sách Mentor');
    } finally {
      setIsLoadingMentors(false);
    }
  };

  const handleAssign = async (e) => {
    e.preventDefault();
    if (!selectedMentorId) {
      setError('Vui lòng chọn một Mentor doanh nghiệp từ danh sách.');
      return;
    }

    if (!student?.id) return;

    setIsSubmitting(true);
    try {
      await studentService.assignMentor(student.id, selectedMentorId);
      const chosenMentor = mentors.find((m) => m.id === selectedMentorId);
      toast.success(
        `Đã phân công ${chosenMentor ? chosenMentor.fullName : 'Mentor'} hướng dẫn sinh viên ${student.fullName}!`,
        { icon: '🎯' }
      );
      onClose();
      if (onSuccess) onSuccess();
    } catch (err) {
      setError(err.message || 'Lỗi khi phân công mentor');
      toast.error(err.message || 'Phân công Mentor không thành công');
    } finally {
      setIsSubmitting(false);
    }
  };

  const currentSelectedMentor = mentors.find((m) => m.id === selectedMentorId);

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Phân Công Mentor Doanh Nghiệp Hướng Dẫn"
      subtitle={`Ghép nối mentor chuyên môn cho sinh viên: ${student?.fullName || ''} (#${student?.studentCode || ''})`}
      icon={UserCheck}
      maxWidth="max-w-xl"
    >
      <form onSubmit={handleAssign} className="space-y-4">
        {/* Khối Thông Tin Sinh Viên Được Ghép */}
        <div className="p-4 rounded-2xl bg-indigo-50/60 border border-indigo-100">
          <div className="flex items-start justify-between">
            <div>
              <span className="text-[10px] font-bold text-indigo-600 uppercase tracking-wider">
                Ứng Viên Thực Tập Cần Hướng Dẫn
              </span>
              <h4 className="text-base font-bold text-slate-900 mt-0.5">
                {student?.fullName}
              </h4>
              <p className="text-xs text-slate-600 mt-0.5">
                Mã SV: <span className="font-mono font-bold text-indigo-700">{student?.studentCode}</span> • Chuyên ngành: {student?.major}
              </p>
              <p className="text-xs text-slate-500 mt-0.5">{student?.university}</p>
            </div>
            {student?.assignedMentor && (
              <div className="text-right">
                <span className="text-[10px] font-bold uppercase text-slate-400 block">
                  Mentor Hiện Tại
                </span>
                <span className="text-xs font-bold text-purple-700">
                  {student.assignedMentor.fullName}
                </span>
              </div>
            )}
          </div>
        </div>

        {/* Hộp Chọn Mentor */}
        <div>
          <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
            Lựa Chọn Mentor Doanh Nghiệp <span className="text-rose-500">*</span>
          </label>

          {isLoadingMentors ? (
            <div className="py-8 flex flex-col items-center justify-center text-slate-500 gap-2">
              <Loader2 className="w-5 h-5 animate-spin text-indigo-600" />
              <p className="text-xs">Đang tải danh sách Mentor khả dụng...</p>
            </div>
          ) : (
            <div className="relative rounded-xl shadow-xs">
              <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400">
                <UserCheck className="w-4 h-4" />
              </div>
              <select
                value={selectedMentorId}
                onChange={(e) => {
                  setSelectedMentorId(e.target.value);
                  setError(null);
                }}
                className={`w-full pl-10 pr-8 py-2.5 text-sm rounded-xl border bg-white focus:outline-none focus:ring-2 transition-all font-medium text-slate-800 ${
                  error
                    ? 'border-rose-300 focus:ring-rose-400 focus:border-rose-400'
                    : 'border-slate-300 focus:ring-indigo-500 focus:border-indigo-500'
                }`}
              >
                <option value="">-- Chọn một Mentor Doanh nghiệp sẵn sàng --</option>
                {mentors.map((m) => (
                  <option key={m.id} value={m.id}>
                    {m.fullName} — {m.department} (Hiện có {m.activeMentees || 0} sinh viên)
                  </option>
                ))}
              </select>
            </div>
          )}

          {error && (
            <div className="mt-2 flex items-center gap-1.5 text-xs text-rose-500 font-medium">
              <AlertCircle className="w-4 h-4 shrink-0" />
              <span>{error}</span>
            </div>
          )}
        </div>

        {/* Xem trước hồ sơ Mentor khi được chọn */}
        {currentSelectedMentor && (
          <div className="p-3.5 rounded-2xl bg-purple-50/70 border border-purple-100 flex items-start gap-3">
            <div className="w-10 h-10 rounded-xl bg-purple-600 text-white font-bold text-sm flex items-center justify-center shrink-0 shadow-xs">
              {currentSelectedMentor.fullName.charAt(0)}
            </div>
            <div className="text-xs flex-1">
              <div className="flex items-center justify-between">
                <span className="font-bold text-purple-950 text-sm">
                  {currentSelectedMentor.fullName}
                </span>
                <span className="text-[11px] font-semibold text-purple-700 bg-white px-2 py-0.5 rounded-full border border-purple-200">
                  {currentSelectedMentor.activeMentees || 0} Sinh viên đang hướng dẫn
                </span>
              </div>
              <p className="text-purple-900 mt-1 flex items-center gap-1 font-medium">
                <Building className="w-3.5 h-3.5 text-purple-500 shrink-0" />
                {currentSelectedMentor.department}
              </p>
              <p className="text-purple-800/80 mt-0.5 flex items-center gap-1">
                <Award className="w-3.5 h-3.5 text-purple-500 shrink-0" />
                {currentSelectedMentor.specialization}
              </p>
            </div>
          </div>
        )}

        {/* Nút Thao Tác */}
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
            disabled={isSubmitting || !selectedMentorId}
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold text-white bg-indigo-600 hover:bg-indigo-700 active:scale-98 transition-all shadow-md shadow-indigo-200 disabled:opacity-50 disabled:pointer-events-none cursor-pointer"
          >
            {isSubmitting ? (
              <>
                <Loader2 className="w-4 h-4 animate-spin" />
                <span>Đang Gán Mentor...</span>
              </>
            ) : (
              <>
                <UserCheck className="w-4 h-4" />
                <span>Xác Nhận Phân Công</span>
              </>
            )}
          </button>
        </div>
      </form>
    </Modal>
  );
};

export default AssignMentorModal;
