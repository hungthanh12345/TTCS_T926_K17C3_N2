import React, { useState, useEffect, useMemo } from 'react';
import DashboardLayout from '../../components/layout/DashboardLayout';
import TableSkeleton from '../../components/common/TableSkeleton';
import AddStudentModal from '../../components/modals/AddStudentModal';
import EditStudentModal from '../../components/modals/EditStudentModal';
import AssignMentorModal from '../../components/modals/AssignMentorModal';
import studentService from '../../services/studentService';
import {
  GraduationCap,
  UserPlus,
  Search,
  Filter,
  RefreshCw,
  Edit,
  UserCheck,
  ChevronLeft,
  ChevronRight,
  School,
  BookOpen,
  Phone,
  Trash2,
  AlertCircle,
  CheckCircle2,
  X,
  Mail,
  Building,
  User,
  ExternalLink,
} from 'lucide-react';
import toast from 'react-hot-toast';

export const StudentManagementView = () => {
  const [students, setStudents] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);

  // Search & Filter State
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedUniversity, setSelectedUniversity] = useState('ALL');
  const [selectedMajor, setSelectedMajor] = useState('ALL');
  const [selectedStatus, setSelectedStatus] = useState('ALL');

  // Pagination State
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 6;

  // Modal Dialog States
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [isAssignModalOpen, setIsAssignModalOpen] = useState(false);
  const [selectedStudent, setSelectedStudent] = useState(null);

  useEffect(() => {
    fetchStudents();
  }, []);

  const fetchStudents = async () => {
    setIsRefreshing(true);
    try {
      const data = await studentService.getStudents();
      const list = Array.isArray(data) ? data : data?.items || [];
      setStudents(list);
    } catch (err) {
      toast.error(err.message || 'Không thể tải danh sách sinh viên.');
    } finally {
      setIsLoading(false);
      setIsRefreshing(false);
    }
  };

  const handleDeleteStudent = async (id, name) => {
    if (window.confirm(`Bạn có chắc chắn muốn xóa hồ sơ sinh viên "${name}"?`)) {
      try {
        await studentService.deleteStudent(id);
        toast.success(`Đã xóa thành công hồ sơ ${name}.`);
        fetchStudents();
      } catch (err) {
        toast.error(err.message || 'Lỗi khi xóa hồ sơ');
      }
    }
  };

  // Extract unique universities and majors for filter options
  const uniqueUniversities = useMemo(() => {
    const unis = new Set(students.map((s) => s.university).filter(Boolean));
    return Array.from(unis);
  }, [students]);

  const uniqueMajors = useMemo(() => {
    const mjs = new Set(students.map((s) => s.major).filter(Boolean));
    return Array.from(mjs);
  }, [students]);

  // Filter students based on all active criteria
  const filteredStudents = useMemo(() => {
    return students.filter((s) => {
      const q = searchQuery.toLowerCase().trim();
      const matchesSearch =
        !q ||
        s.fullName?.toLowerCase().includes(q) ||
        s.studentCode?.toLowerCase().includes(q) ||
        (s.phone && s.phone.includes(q)) ||
        (s.email && s.email.toLowerCase().includes(q)) ||
        (s.assignedMentor?.fullName && s.assignedMentor.fullName.toLowerCase().includes(q));

      const matchesUni = selectedUniversity === 'ALL' || s.university === selectedUniversity;
      const matchesMajor = selectedMajor === 'ALL' || s.major === selectedMajor;
      const matchesStatus =
        selectedStatus === 'ALL' ||
        (selectedStatus === 'ASSIGNED' ? !!s.mentorId : !s.mentorId);

      return matchesSearch && matchesUni && matchesMajor && matchesStatus;
    });
  }, [students, searchQuery, selectedUniversity, selectedMajor, selectedStatus]);

  // Pagination calculation
  const totalPages = Math.ceil(filteredStudents.length / itemsPerPage) || 1;
  const paginatedStudents = useMemo(() => {
    const start = (currentPage - 1) * itemsPerPage;
    return filteredStudents.slice(start, start + itemsPerPage);
  }, [filteredStudents, currentPage, itemsPerPage]);

  useEffect(() => {
    setCurrentPage(1);
  }, [searchQuery, selectedUniversity, selectedMajor, selectedStatus]);

  // Key KPI metrics
  const totalStudents = students.length;
  const assignedCount = students.filter((s) => !!s.mentorId).length;
  const unassignedCount = totalStudents - assignedCount;
  const assignedPercentage = totalStudents > 0 ? Math.round((assignedCount / totalStudents) * 100) : 0;

  const isFiltered =
    searchQuery.trim() !== '' ||
    selectedUniversity !== 'ALL' ||
    selectedMajor !== 'ALL' ||
    selectedStatus !== 'ALL';

  const resetFilters = () => {
    setSearchQuery('');
    setSelectedUniversity('ALL');
    setSelectedMajor('ALL');
    setSelectedStatus('ALL');
  };

  // Color generator for avatar initials
  const getAvatarGradient = (name = '') => {
    const gradients = [
      'from-indigo-600 to-indigo-500',
      'from-blue-600 to-cyan-500',
      'from-purple-600 to-pink-500',
      'from-emerald-600 to-teal-500',
      'from-rose-600 to-orange-500',
    ];
    let hash = 0;
    for (let i = 0; i < name.length; i++) {
      hash = name.charCodeAt(i) + ((hash << 5) - hash);
    }
    const index = Math.abs(hash) % gradients.length;
    return gradients[index];
  };

  return (
    <DashboardLayout
      title="Quản Lý Hồ Sơ Sinh Viên"
      subtitle="Theo dõi thông tin thực tập sinh, tra cứu hồ sơ và phân công người hướng dẫn doanh nghiệp"
    >
      <div className="space-y-6">
        {/* ======================================================== */}
        {/* 1. TOP METRIC CARDS (MODERN SAAS STATS WITH PROGRESS BAR) */}
        {/* ======================================================== */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          {/* Card 1: Total Candidates */}
          <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs hover:shadow-md transition-all group">
            <div className="flex items-center justify-between">
              <span className="text-xs font-semibold text-slate-500 uppercase tracking-wider">
                Tổng Số Sinh Viên
              </span>
              <div className="w-10 h-10 rounded-xl bg-indigo-50 text-indigo-600 flex items-center justify-center group-hover:scale-105 transition-transform">
                <GraduationCap className="w-5 h-5" />
              </div>
            </div>
            <div className="mt-3 flex items-baseline justify-between">
              <span className="text-3xl font-extrabold text-slate-900 tracking-tight">
                {totalStudents}
              </span>
              <span className="text-xs font-semibold text-indigo-600 bg-indigo-50 px-2 py-0.5 rounded-full border border-indigo-100">
                Kỳ 2026
              </span>
            </div>
            <p className="text-[11px] text-slate-400 mt-2">Dữ liệu thực tập sinh toàn hệ thống</p>
          </div>

          {/* Card 2: Assigned Mentors (With Progress Bar) */}
          <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs hover:shadow-md transition-all group">
            <div className="flex items-center justify-between">
              <span className="text-xs font-semibold text-slate-500 uppercase tracking-wider">
                Đã Ghép Mentor
              </span>
              <div className="w-10 h-10 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center group-hover:scale-105 transition-transform">
                <UserCheck className="w-5 h-5" />
              </div>
            </div>
            <div className="mt-3 flex items-baseline justify-between">
              <div className="flex items-baseline gap-1.5">
                <span className="text-3xl font-extrabold text-emerald-600 tracking-tight">
                  {assignedCount}
                </span>
                <span className="text-xs text-slate-400 font-medium">/ {totalStudents}</span>
              </div>
              <span className="text-xs font-bold text-emerald-700 bg-emerald-50 px-2.5 py-0.5 rounded-full border border-emerald-100">
                {assignedPercentage}%
              </span>
            </div>
            {/* Percentage Progress Bar */}
            <div className="mt-2.5 w-full bg-slate-100 rounded-full h-1.5 overflow-hidden">
              <div
                className="bg-gradient-to-r from-emerald-500 to-teal-400 h-1.5 rounded-full transition-all duration-500"
                style={{ width: `${assignedPercentage}%` }}
              />
            </div>
          </div>

          {/* Card 3: Pending Assignment */}
          <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs hover:shadow-md transition-all group">
            <div className="flex items-center justify-between">
              <span className="text-xs font-semibold text-slate-500 uppercase tracking-wider">
                Chờ Phân Bổ Mentor
              </span>
              <div className="w-10 h-10 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center group-hover:scale-105 transition-transform">
                <AlertCircle className="w-5 h-5" />
              </div>
            </div>
            <div className="mt-3 flex items-baseline justify-between">
              <span className="text-3xl font-extrabold text-amber-600 tracking-tight">
                {unassignedCount}
              </span>
              {unassignedCount > 0 ? (
                <span className="text-xs font-semibold text-amber-700 bg-amber-50 px-2 py-0.5 rounded-full border border-amber-100">
                  Cần xử lý
                </span>
              ) : (
                <span className="text-xs font-semibold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-full border border-emerald-100">
                  Hoàn tất 100%
                </span>
              )}
            </div>
            <p className="text-[11px] text-slate-400 mt-2">Sinh viên chưa được ghép người hướng dẫn</p>
          </div>

          {/* Card 4: Partner Universities */}
          <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs hover:shadow-md transition-all group">
            <div className="flex items-center justify-between">
              <span className="text-xs font-semibold text-slate-500 uppercase tracking-wider">
                Trường Đại Học
              </span>
              <div className="w-10 h-10 rounded-xl bg-sky-50 text-sky-600 flex items-center justify-center group-hover:scale-105 transition-transform">
                <School className="w-5 h-5" />
              </div>
            </div>
            <div className="mt-3 flex items-baseline justify-between">
              <span className="text-3xl font-extrabold text-slate-900 tracking-tight">
                {uniqueUniversities.length || 1}
              </span>
              <span className="text-xs font-semibold text-sky-700 bg-sky-50 px-2 py-0.5 rounded-full border border-sky-100">
                {uniqueMajors.length} Ngành
              </span>
            </div>
            <p className="text-[11px] text-slate-400 mt-2">Mạng lưới đối tác đào tạo liên kết</p>
          </div>
        </div>

        {/* ======================================================== */}
        {/* 2. SEARCH & FILTER TOOLBAR (REFINED LINEAR/STRIPE STYLE)  */}
        {/* ======================================================== */}
        <div className="bg-white p-4 sm:p-5 rounded-2xl border border-slate-200/80 shadow-xs space-y-3.5">
          <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-3">
            {/* Search Input with Embedded Icons */}
            <div className="relative flex-1 min-w-[280px]">
              <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400">
                <Search className="w-4 h-4" />
              </div>
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Tìm theo họ tên, mã sinh viên (STU...), số điện thoại hoặc tên mentor..."
                className="w-full pl-10 pr-9 py-2.5 text-xs sm:text-sm rounded-xl border border-slate-200 bg-slate-50/50 hover:bg-slate-50 focus:bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all placeholder:text-slate-400"
              />
              {searchQuery && (
                <button
                  type="button"
                  onClick={() => setSearchQuery('')}
                  className="absolute inset-y-0 right-0 pr-3 flex items-center text-slate-400 hover:text-slate-600 cursor-pointer"
                  title="Xóa tìm kiếm"
                >
                  <X className="w-4 h-4" />
                </button>
              )}
            </div>

            {/* Action Buttons */}
            <div className="flex items-center gap-2.5 shrink-0">
              <button
                type="button"
                onClick={fetchStudents}
                disabled={isRefreshing}
                className="p-2.5 text-slate-500 hover:text-slate-800 hover:bg-slate-100 rounded-xl border border-slate-200 transition-colors cursor-pointer disabled:opacity-50"
                title="Làm mới dữ liệu từ máy chủ"
              >
                <RefreshCw
                  className={`w-4 h-4 ${isRefreshing ? 'animate-spin text-indigo-600' : ''}`}
                />
              </button>

              <button
                type="button"
                onClick={() => setIsAddModalOpen(true)}
                className="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl text-xs sm:text-sm font-semibold text-white bg-gradient-to-r from-indigo-600 to-indigo-500 hover:from-indigo-500 hover:to-indigo-600 active:scale-95 transition-all shadow-md shadow-indigo-500/20 hover:shadow-indigo-500/30 cursor-pointer"
              >
                <UserPlus className="w-4 h-4" />
                <span>+ Thêm Sinh Viên Mới</span>
              </button>
            </div>
          </div>

          {/* Filter Dropdowns Line */}
          <div className="flex flex-wrap items-center gap-2.5 pt-3 border-t border-slate-100 text-xs">
            <span className="text-slate-400 font-semibold uppercase tracking-wider flex items-center gap-1.5 pr-1">
              <Filter className="w-3.5 h-3.5 text-slate-400" />
              <span>Bộ Lọc:</span>
            </span>

            {/* University Filter */}
            <select
              value={selectedUniversity}
              onChange={(e) => setSelectedUniversity(e.target.value)}
              className="py-1.5 px-3 rounded-xl border border-slate-200 bg-white text-slate-700 hover:border-slate-300 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 cursor-pointer max-w-[240px] truncate font-medium text-xs shadow-2xs"
            >
              <option value="ALL">Tất cả các Trường Đại học</option>
              {uniqueUniversities.map((uni) => (
                <option key={uni} value={uni}>
                  {uni}
                </option>
              ))}
            </select>

            {/* Major Filter */}
            <select
              value={selectedMajor}
              onChange={(e) => setSelectedMajor(e.target.value)}
              className="py-1.5 px-3 rounded-xl border border-slate-200 bg-white text-slate-700 hover:border-slate-300 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 cursor-pointer max-w-[220px] truncate font-medium text-xs shadow-2xs"
            >
              <option value="ALL">Tất cả các Chuyên ngành</option>
              {uniqueMajors.map((m) => (
                <option key={m} value={m}>
                  {m}
                </option>
              ))}
            </select>

            {/* Mentor Status Filter */}
            <select
              value={selectedStatus}
              onChange={(e) => setSelectedStatus(e.target.value)}
              className="py-1.5 px-3 rounded-xl border border-slate-200 bg-white text-slate-700 hover:border-slate-300 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 cursor-pointer font-medium text-xs shadow-2xs"
            >
              <option value="ALL">Trạng thái: Tất cả</option>
              <option value="ASSIGNED">Đã phân công Mentor</option>
              <option value="UNASSIGNED">Chờ phân công Mentor</option>
            </select>

            {/* Reset Filter Button */}
            {isFiltered && (
              <button
                type="button"
                onClick={resetFilters}
                className="text-xs text-rose-600 hover:text-rose-700 font-semibold flex items-center gap-1 ml-auto px-2 py-1 rounded-lg hover:bg-rose-50 transition-colors cursor-pointer"
              >
                <X className="w-3.5 h-3.5" />
                <span>Đặt lại bộ lọc</span>
              </button>
            )}
          </div>
        </div>

        {/* ======================================================== */}
        {/* 3. MODERN DATA TABLE & EMPTY STATE                       */}
        {/* ======================================================== */}
        <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-hidden">
          {isLoading ? (
            <TableSkeleton rows={5} cols={6} />
          ) : filteredStudents.length === 0 ? (
            /* Modern Refined Empty State */
            <div className="py-20 px-4 text-center max-w-md mx-auto">
              <div className="relative w-16 h-16 rounded-2xl bg-indigo-50 text-indigo-600 flex items-center justify-center mx-auto mb-4 ring-8 ring-indigo-50/50 shadow-inner">
                <GraduationCap className="w-8 h-8" />
              </div>
              <h3 className="text-base font-bold text-slate-900 tracking-tight">
                Không tìm thấy sinh viên phù hợp
              </h3>
              <p className="text-xs text-slate-500 mt-1.5 leading-relaxed">
                {isFiltered
                  ? 'Không có kết quả nào khớp với các tiêu chí tìm kiếm hoặc bộ lọc hiện tại của bạn.'
                  : 'Chưa có hồ sơ sinh viên thực tập nào trong hệ thống. Hãy bắt đầu bằng cách tạo hồ sơ đầu tiên.'}
              </p>
              <div className="mt-6 flex items-center justify-center gap-2.5">
                {isFiltered ? (
                  <button
                    type="button"
                    onClick={resetFilters}
                    className="px-4 py-2 rounded-xl text-xs font-semibold text-slate-700 bg-slate-100 hover:bg-slate-200 transition-colors cursor-pointer"
                  >
                    Xóa các bộ lọc
                  </button>
                ) : (
                  <button
                    type="button"
                    onClick={() => setIsAddModalOpen(true)}
                    className="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl text-xs font-semibold text-white bg-indigo-600 hover:bg-indigo-700 shadow-md shadow-indigo-200 transition-all cursor-pointer"
                  >
                    <UserPlus className="w-4 h-4" />
                    <span>Thêm Sinh Viên Đầu Tiên</span>
                  </button>
                )}
              </div>
            </div>
          ) : (
            /* Modern Data Table */
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse">
                <thead>
                  <tr className="border-b border-slate-200/80 bg-slate-50/70 text-[11px] font-bold text-slate-400 uppercase tracking-wider">
                    <th className="py-3.5 px-6">Mã SV</th>
                    <th className="py-3.5 px-6">Họ Tên & Liên Hệ</th>
                    <th className="py-3.5 px-6">Trường Đại Học</th>
                    <th className="py-3.5 px-6">Chuyên Ngành</th>
                    <th className="py-3.5 px-6">Mentor Phụ Trách</th>
                    <th className="py-3.5 px-6 text-right">Thao Tác</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-sm">
                  {paginatedStudents.map((s) => {
                    const avatarGradient = getAvatarGradient(s.fullName || '');
                    return (
                      <tr
                        key={s.id}
                        className="hover:bg-slate-50/80 transition-colors group"
                      >
                        {/* Student Code */}
                        <td className="py-4 px-6 align-middle">
                          <span className="font-mono text-xs font-bold text-indigo-700 bg-indigo-50/80 px-2.5 py-1 rounded-lg border border-indigo-200/70 shadow-2xs">
                            {s.studentCode}
                          </span>
                        </td>

                        {/* Full Name & Avatar & Contacts */}
                        <td className="py-4 px-6 align-middle">
                          <div className="flex items-center gap-3">
                            <div
                              className={`w-9 h-9 rounded-xl bg-gradient-to-tr ${avatarGradient} text-white font-bold text-xs flex items-center justify-center shrink-0 shadow-xs ring-2 ring-white`}
                            >
                              {s.fullName?.charAt(0) || 'S'}
                            </div>
                            <div className="min-w-0">
                              <span className="font-bold text-slate-900 block group-hover:text-indigo-600 transition-colors truncate">
                                {s.fullName}
                              </span>
                              <div className="flex items-center gap-2 text-xs text-slate-400 mt-0.5">
                                {s.phone && (
                                  <span className="flex items-center gap-1">
                                    <Phone className="w-3 h-3 text-slate-400" />
                                    <span>{s.phone}</span>
                                  </span>
                                )}
                                {s.email && (
                                  <span className="hidden sm:flex items-center gap-1 truncate max-w-[140px]">
                                    <Mail className="w-3 h-3 text-slate-400" />
                                    <span className="truncate">{s.email}</span>
                                  </span>
                                )}
                              </div>
                            </div>
                          </div>
                        </td>

                        {/* University */}
                        <td className="py-4 px-6 align-middle text-xs text-slate-600 max-w-xs">
                          <div className="flex items-start gap-1.5">
                            <School className="w-3.5 h-3.5 text-slate-400 shrink-0 mt-0.5" />
                            <span className="line-clamp-2 leading-relaxed font-medium">
                              {s.university}
                            </span>
                          </div>
                        </td>

                        {/* Major */}
                        <td className="py-4 px-6 align-middle text-xs">
                          <div className="flex items-center gap-1.5 font-semibold text-slate-800">
                            <BookOpen className="w-3.5 h-3.5 text-indigo-500 shrink-0" />
                            <span className="truncate max-w-[160px]">{s.major}</span>
                          </div>
                        </td>

                        {/* Assigned Mentor Status */}
                        <td className="py-4 px-6 align-middle">
                          {s.assignedMentor || s.mentor ? (
                            (() => {
                              const mentorInfo = s.assignedMentor || s.mentor;
                              return (
                                <div className="flex items-center gap-2.5">
                                  <div className="relative">
                                    <div className="w-8 h-8 rounded-lg bg-emerald-100 text-emerald-800 font-bold text-xs flex items-center justify-center shrink-0">
                                      {mentorInfo.fullName?.charAt(0) || 'M'}
                                    </div>
                                    <span className="absolute -bottom-0.5 -right-0.5 w-2 h-2 rounded-full bg-emerald-500 ring-1 ring-white" />
                                  </div>
                                  <div className="min-w-0">
                                    <span className="text-xs font-bold text-slate-900 block truncate max-w-[140px]">
                                      {mentorInfo.fullName}
                                    </span>
                                    <span className="text-[10px] text-emerald-700 font-medium block truncate max-w-[140px]">
                                      {mentorInfo.department || 'Mentor Doanh Nghiệp'}
                                    </span>
                                  </div>
                                </div>
                              );
                            })()
                          ) : (
                            <button
                              type="button"
                              onClick={() => {
                                setSelectedStudent(s);
                                setIsAssignModalOpen(true);
                              }}
                              className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg text-xs font-semibold bg-amber-50 text-amber-700 border border-amber-200/80 hover:bg-amber-100 hover:border-amber-300 transition-all cursor-pointer shadow-2xs"
                            >
                              <span className="relative flex h-2 w-2">
                                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-amber-400 opacity-75" />
                                <span className="relative inline-flex rounded-full h-2 w-2 bg-amber-500" />
                              </span>
                              <span>Chờ ghép Mentor</span>
                            </button>
                          )}
                        </td>

                        {/* Row Actions */}
                        <td className="py-4 px-6 align-middle text-right">
                          <div className="flex items-center justify-end gap-1">
                            {/* Assign Mentor Button */}
                            <button
                              type="button"
                              onClick={() => {
                                setSelectedStudent(s);
                                setIsAssignModalOpen(true);
                              }}
                              className="p-1.5 text-slate-400 hover:text-indigo-600 hover:bg-indigo-50 rounded-lg transition-colors cursor-pointer"
                              title="Phân công hoặc đổi Mentor"
                            >
                              <UserCheck className="w-4 h-4" />
                            </button>

                            {/* Edit Student Button */}
                            <button
                              type="button"
                              onClick={() => {
                                setSelectedStudent(s);
                                setIsEditModalOpen(true);
                              }}
                              className="p-1.5 text-slate-400 hover:text-blue-600 hover:bg-blue-50 rounded-lg transition-colors cursor-pointer"
                              title="Chỉnh sửa hồ sơ"
                            >
                              <Edit className="w-4 h-4" />
                            </button>

                            {/* Delete Student Button */}
                            <button
                              type="button"
                              onClick={() => handleDeleteStudent(s.id, s.fullName)}
                              className="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors cursor-pointer"
                              title="Xóa hồ sơ sinh viên"
                            >
                              <Trash2 className="w-4 h-4" />
                            </button>
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}

          {/* ======================================================== */}
          {/* 4. PAGINATION FOOTER                                     */}
          {/* ======================================================== */}
          {filteredStudents.length > 0 && (
            <div className="px-6 py-4 border-t border-slate-100 bg-slate-50/60 flex flex-col sm:flex-row items-center justify-between gap-3 text-xs text-slate-600">
              <div>
                Đang hiển thị{' '}
                <span className="font-bold text-slate-900">
                  {(currentPage - 1) * itemsPerPage + 1}
                </span>{' '}
                -{' '}
                <span className="font-bold text-slate-900">
                  {Math.min(currentPage * itemsPerPage, filteredStudents.length)}
                </span>{' '}
                trong tổng số{' '}
                <span className="font-bold text-slate-900">
                  {filteredStudents.length}
                </span>{' '}
                hồ sơ sinh viên
              </div>

              <div className="flex items-center gap-1.5">
                <button
                  type="button"
                  onClick={() => setCurrentPage((p) => Math.max(p - 1, 1))}
                  disabled={currentPage === 1}
                  className="p-1.5 rounded-lg border border-slate-200 bg-white hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed transition-all cursor-pointer"
                  title="Trang trước"
                >
                  <ChevronLeft className="w-4 h-4" />
                </button>

                {Array.from({ length: totalPages }).map((_, idx) => (
                  <button
                    key={idx + 1}
                    type="button"
                    onClick={() => setCurrentPage(idx + 1)}
                    className={`min-w-[32px] h-8 px-2.5 rounded-lg text-xs font-bold transition-all cursor-pointer ${
                      currentPage === idx + 1
                        ? 'bg-indigo-600 text-white shadow-xs'
                        : 'border border-slate-200 bg-white hover:bg-slate-50 text-slate-700'
                    }`}
                  >
                    {idx + 1}
                  </button>
                ))}

                <button
                  type="button"
                  onClick={() => setCurrentPage((p) => Math.min(p + 1, totalPages))}
                  disabled={currentPage === totalPages}
                  className="p-1.5 rounded-lg border border-slate-200 bg-white hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed transition-all cursor-pointer"
                  title="Trang sau"
                >
                  <ChevronRight className="w-4 h-4" />
                </button>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* ======================================================== */}
      {/* 5. MODAL DIALOGS                                         */}
      {/* ======================================================== */}
      <AddStudentModal
        isOpen={isAddModalOpen}
        onClose={() => setIsAddModalOpen(false)}
        onSuccess={fetchStudents}
      />

      <EditStudentModal
        isOpen={isEditModalOpen}
        student={selectedStudent}
        onClose={() => {
          setIsEditModalOpen(false);
          setSelectedStudent(null);
        }}
        onSuccess={fetchStudents}
      />

      <AssignMentorModal
        isOpen={isAssignModalOpen}
        student={selectedStudent}
        onClose={() => {
          setIsAssignModalOpen(false);
          setSelectedStudent(null);
        }}
        onSuccess={fetchStudents}
      />
    </DashboardLayout>
  );
};

export default StudentManagementView;
