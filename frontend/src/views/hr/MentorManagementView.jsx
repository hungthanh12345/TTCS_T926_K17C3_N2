import React, { useState, useEffect, useMemo } from 'react';
import DashboardLayout from '../../components/layout/DashboardLayout';
import TableSkeleton from '../../components/common/TableSkeleton';
import AddMentorModal from '../../components/modals/AddMentorModal';
import mentorService from '../../services/mentorService';
import {
  Briefcase,
  UserCheck,
  UserPlus,
  Search,
  RefreshCw,
  Phone,
  Mail,
  Building,
  Award,
  Users,
  Grid,
  List,
  Trash2,
} from 'lucide-react';
import toast from 'react-hot-toast';

export const MentorManagementView = () => {
  const [mentors, setMentors] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedDepartment, setSelectedDepartment] = useState('ALL');
  const [viewMode, setViewMode] = useState('grid');
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);

  useEffect(() => {
    fetchMentors();
  }, []);

  const fetchMentors = async () => {
    setIsRefreshing(true);
    try {
      const data = await mentorService.getMentors();
      const list = Array.isArray(data) ? data : data?.items || [];
      setMentors(list);
    } catch (err) {
      toast.error(err.message || 'Không thể tải danh sách Mentor');
    } finally {
      setIsLoading(false);
      setIsRefreshing(false);
    }
  };

  const handleDeleteMentor = async (id, name) => {
    if (window.confirm(`Bạn có chắc chắn muốn xóa hồ sơ Mentor "${name}" khỏi hệ thống?`)) {
      try {
        await mentorService.deleteMentor(id);
        toast.success(`Đã xóa hồ sơ Mentor ${name}.`);
        fetchMentors();
      } catch (err) {
        toast.error(err.message || 'Lỗi khi xóa Mentor');
      }
    }
  };

  const departments = useMemo(() => {
    const deps = new Set(mentors.map((m) => m.department).filter(Boolean));
    return Array.from(deps);
  }, [mentors]);

  const filteredMentors = useMemo(() => {
    return mentors.filter((m) => {
      const q = searchQuery.toLowerCase().trim();
      const matchesSearch =
        !q ||
        m.fullName.toLowerCase().includes(q) ||
        m.department.toLowerCase().includes(q) ||
        m.specialization.toLowerCase().includes(q) ||
        (m.phone && m.phone.includes(q));

      const matchesDept =
        selectedDepartment === 'ALL' || m.department === selectedDepartment;

      return matchesSearch && matchesDept;
    });
  }, [mentors, searchQuery, selectedDepartment]);

  const totalMentors = mentors.length;
  const totalMenteesAssigned = mentors.reduce((acc, m) => acc + (m.activeMentees || 0), 0);
  const activeDepartmentsCount = departments.length;

  return (
    <DashboardLayout
      title="Danh Bạ Mentor và Phân Bổ Hướng Dẫn"
      subtitle="Sprint 1 - Story 4: Quản lý danh sách chuyên gia hướng dẫn, theo dõi tải giám sát và lĩnh vực công nghệ"
    >
      <div className="space-y-6">
        {/* Hàng Thẻ Chỉ Số Thông Minh */}
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs hover:shadow-md transition-shadow">
            <div className="flex items-center justify-between">
              <span className="text-xs font-semibold text-slate-500 uppercase tracking-wider">
                Mentor Đang Phụ Trách
              </span>
              <div className="w-9 h-9 rounded-xl bg-indigo-50 text-indigo-600 flex items-center justify-center">
                <Briefcase className="w-4 h-4" />
              </div>
            </div>
            <div className="mt-2 flex items-baseline gap-2">
              <span className="text-2xl font-extrabold text-slate-900">{totalMentors}</span>
              <span className="text-xs text-indigo-600 font-semibold">Chuyên gia</span>
            </div>
          </div>

          <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs hover:shadow-md transition-shadow">
            <div className="flex items-center justify-between">
              <span className="text-xs font-semibold text-slate-500 uppercase tracking-wider">
                Sinh Viên Đang Hướng Dẫn
              </span>
              <div className="w-9 h-9 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center">
                <Users className="w-4 h-4" />
              </div>
            </div>
            <div className="mt-2 flex items-baseline gap-2">
              <span className="text-2xl font-extrabold text-emerald-600">{totalMenteesAssigned}</span>
              <span className="text-xs text-emerald-700 font-medium">Được giám sát</span>
            </div>
          </div>

          <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs hover:shadow-md transition-shadow">
            <div className="flex items-center justify-between">
              <span className="text-xs font-semibold text-slate-500 uppercase tracking-wider">
                Phòng Ban Chuyên Môn
              </span>
              <div className="w-9 h-9 rounded-xl bg-purple-50 text-purple-600 flex items-center justify-center">
                <Building className="w-4 h-4" />
              </div>
            </div>
            <div className="mt-2 flex items-baseline gap-2">
              <span className="text-2xl font-extrabold text-slate-900">{activeDepartmentsCount}</span>
              <span className="text-xs text-purple-600 font-medium">Khối kỹ thuật</span>
            </div>
          </div>
        </div>

        {/* Thanh Tìm Kiếm, Bộ Lọc & Chuyển Chế Độ Xem */}
        <div className="bg-white p-4 sm:p-5 rounded-2xl border border-slate-200/80 shadow-xs flex flex-col sm:flex-row items-center justify-between gap-3">
          {/* Ô Tìm kiếm */}
          <div className="relative flex-1 w-full max-w-md">
            <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400">
              <Search className="w-4 h-4" />
            </div>
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Tìm theo tên Mentor, phòng ban hoặc chuyên môn..."
              className="w-full pl-10 pr-4 py-2 text-sm rounded-xl border border-slate-200 bg-slate-50/50 focus:bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-all"
            />
          </div>

          <div className="flex items-center gap-2.5 w-full sm:w-auto justify-end">
            {/* Lọc Phòng Ban */}
            <select
              value={selectedDepartment}
              onChange={(e) => setSelectedDepartment(e.target.value)}
              className="py-2 px-3 text-xs font-semibold rounded-xl border border-slate-200 bg-white text-slate-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 cursor-pointer max-w-[200px] truncate"
            >
              <option value="ALL">Tất cả phòng ban</option>
              {departments.map((dept) => (
                <option key={dept} value={dept}>
                  {dept}
                </option>
              ))}
            </select>

            {/* Chuyển Chế Độ Xem Thẻ vs Bảng */}
            <div className="flex items-center border border-slate-200 rounded-xl p-0.5 bg-slate-100">
              <button
                type="button"
                onClick={() => setViewMode('grid')}
                className={`p-1.5 rounded-lg transition-all cursor-pointer ${
                  viewMode === 'grid'
                    ? 'bg-white text-indigo-600 shadow-xs'
                    : 'text-slate-500 hover:text-slate-800'
                }`}
                title="Xem dạng thẻ (Grid)"
              >
                <Grid className="w-4 h-4" />
              </button>
              <button
                type="button"
                onClick={() => setViewMode('table')}
                className={`p-1.5 rounded-lg transition-all cursor-pointer ${
                  viewMode === 'table'
                    ? 'bg-white text-indigo-600 shadow-xs'
                    : 'text-slate-500 hover:text-slate-800'
                }`}
                title="Xem dạng danh sách (Table)"
              >
                <List className="w-4 h-4" />
              </button>
            </div>

            {/* Nút Làm Mới */}
            <button
              type="button"
              onClick={fetchMentors}
              disabled={isRefreshing}
              className="p-2 text-slate-500 hover:text-slate-700 hover:bg-slate-100 rounded-xl border border-slate-200 transition-colors cursor-pointer"
              title="Làm mới danh bạ"
            >
              <RefreshCw
                className={`w-4 h-4 ${isRefreshing ? 'animate-spin text-indigo-600' : ''}`}
              />
            </button>

            {/* Nút Thêm Mới Mentor */}
            <button
              type="button"
              onClick={() => setIsAddModalOpen(true)}
              className="inline-flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-semibold text-white bg-indigo-600 hover:bg-indigo-700 active:scale-98 transition-all shadow-md shadow-indigo-200 shrink-0 cursor-pointer"
            >
              <UserPlus className="w-4 h-4" />
              <span>Thêm Mentor</span>
            </button>
          </div>
        </div>

        {/* Nội Dung Hiển Thị */}
        {isLoading ? (
          <TableSkeleton rows={4} cols={5} />
        ) : filteredMentors.length === 0 ? (
          <div className="bg-white rounded-2xl border border-slate-200/80 p-16 text-center shadow-xs">
            <div className="w-12 h-12 rounded-2xl bg-slate-100 text-slate-400 flex items-center justify-center mx-auto mb-3">
              <Briefcase className="w-6 h-6" />
            </div>
            <h3 className="text-sm font-bold text-slate-800">Không tìm thấy Mentor nào</h3>
            <p className="text-xs text-slate-500 mt-1">
              {searchQuery || selectedDepartment !== 'ALL'
                ? 'Thử xóa từ khóa tìm kiếm hoặc đặt lại bộ lọc phòng ban.'
                : 'Nhấn "Thêm Mentor" để ghi nhận chuyên gia đầu tiên.'}
            </p>
          </div>
        ) : viewMode === 'grid' ? (
          /* Dạng Thẻ Grid Cards */
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
            {filteredMentors.map((mentor) => (
              <div
                key={mentor.id}
                className="bg-white rounded-2xl border border-slate-200/80 shadow-xs hover:shadow-md transition-all p-5 flex flex-col justify-between group"
              >
                <div>
                  {/* Avatar & Phòng ban */}
                  <div className="flex items-start justify-between gap-3">
                    <div className="flex items-center gap-3">
                      <div className="w-12 h-12 rounded-2xl bg-gradient-to-tr from-purple-600 to-indigo-600 text-white font-bold text-base flex items-center justify-center shadow-xs">
                        {mentor.fullName.charAt(0)}
                      </div>
                      <div>
                        <h4 className="text-sm font-bold text-slate-900 group-hover:text-indigo-600 transition-colors">
                          {mentor.fullName}
                        </h4>
                        <span className="inline-flex items-center gap-1 text-[11px] font-semibold text-purple-700 bg-purple-50 px-2 py-0.5 rounded-md border border-purple-100 mt-0.5">
                          <Building className="w-3 h-3 text-purple-500" />
                          {mentor.department}
                        </span>
                      </div>
                    </div>

                    <button
                      type="button"
                      onClick={() => handleDeleteMentor(mentor.id, mentor.fullName)}
                      className="text-slate-300 hover:text-rose-600 p-1 rounded-lg transition-colors cursor-pointer"
                      title="Xóa Mentor"
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </div>

                  {/* Khối Chuyên môn */}
                  <div className="mt-4 p-3 rounded-xl bg-slate-50 border border-slate-100">
                    <div className="flex items-center gap-1.5 text-xs font-semibold text-slate-700 mb-1">
                      <Award className="w-3.5 h-3.5 text-indigo-500" />
                      <span>Chuyên Môn và Công Nghệ Thành Thạo</span>
                    </div>
                    <p className="text-xs text-slate-600 leading-relaxed font-normal">
                      {mentor.specialization}
                    </p>
                  </div>

                  {/* Thông tin liên hệ */}
                  <div className="mt-3.5 space-y-1.5 text-xs text-slate-500">
                    <div className="flex items-center gap-2">
                      <Phone className="w-3.5 h-3.5 text-slate-400" />
                      <span>{mentor.phone}</span>
                    </div>
                    {mentor.email && (
                      <div className="flex items-center gap-2">
                        <Mail className="w-3.5 h-3.5 text-slate-400" />
                        <span className="truncate">{mentor.email}</span>
                      </div>
                    )}
                  </div>
                </div>

                {/* Footer với số lượng sinh viên */}
                <div className="mt-5 pt-3.5 border-t border-slate-100 flex items-center justify-between text-xs">
                  <div className="flex items-center gap-1.5 text-slate-700 font-medium">
                    <Users className="w-4 h-4 text-indigo-500" />
                    <span>
                      <strong className="text-slate-900 font-bold">
                        {mentor.activeMentees || 0}
                      </strong>{' '}
                      Sinh viên đang hướng dẫn
                    </span>
                  </div>

                  <span className="text-[11px] font-semibold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-full border border-emerald-100">
                    Sẵn sàng tiếp nhận
                  </span>
                </div>
              </div>
            ))}
          </div>
        ) : (
          /* Dạng Danh Sách Table */
          <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse">
                <thead>
                  <tr className="border-b border-slate-200/80 bg-slate-50/70 text-[11px] font-bold text-slate-500 uppercase tracking-wider">
                    <th className="py-3.5 px-6">Họ Tên Mentor</th>
                    <th className="py-3.5 px-6">Số Điện Thoại</th>
                    <th className="py-3.5 px-6">Phòng Ban Chuyên Môn</th>
                    <th className="py-3.5 px-6">Định Hướng Công Nghệ</th>
                    <th className="py-3.5 px-6 text-center">Sinh Viên Phụ Trách</th>
                    <th className="py-3.5 px-6 text-right">Thao Tác</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-sm">
                  {filteredMentors.map((m) => (
                    <tr key={m.id} className="hover:bg-slate-50/80 transition-colors">
                      <td className="py-4 px-6">
                        <div className="flex items-center gap-2.5">
                          <div className="w-8 h-8 rounded-xl bg-purple-100 text-purple-700 font-bold text-xs flex items-center justify-center shrink-0">
                            {m.fullName.charAt(0)}
                          </div>
                          <div>
                            <span className="font-bold text-slate-900 block">
                              {m.fullName}
                            </span>
                            <span className="text-xs text-slate-400 block truncate max-w-[200px]">
                              {m.email}
                            </span>
                          </div>
                        </div>
                      </td>
                      <td className="py-4 px-6 font-mono text-xs text-slate-600">
                        {m.phone}
                      </td>
                      <td className="py-4 px-6 text-xs font-semibold text-slate-800">
                        {m.department}
                      </td>
                      <td className="py-4 px-6 text-xs text-slate-600 max-w-sm">
                        <span className="line-clamp-2">{m.specialization}</span>
                      </td>
                      <td className="py-4 px-6 text-center">
                        <span className="inline-flex items-center justify-center px-2.5 py-1 rounded-full text-xs font-bold bg-indigo-50 text-indigo-700 border border-indigo-100">
                          {m.activeMentees || 0} sinh viên
                        </span>
                      </td>
                      <td className="py-4 px-6 text-right">
                        <button
                          type="button"
                          onClick={() => handleDeleteMentor(m.id, m.fullName)}
                          className="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors cursor-pointer"
                          title="Xóa Mentor"
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}
      </div>

      {/* Modal Thêm Mentor */}
      <AddMentorModal
        isOpen={isAddModalOpen}
        onClose={() => setIsAddModalOpen(false)}
        onSuccess={fetchMentors}
      />
    </DashboardLayout>
  );
};

export default MentorManagementView;
