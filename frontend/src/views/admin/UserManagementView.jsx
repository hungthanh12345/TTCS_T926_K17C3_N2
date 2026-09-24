import React, { useState, useEffect } from 'react';
import DashboardLayout from '../../components/layout/DashboardLayout';
import Badge from '../../components/common/Badge';
import TableSkeleton from '../../components/common/TableSkeleton';
import CreateUserModal from '../../components/modals/CreateUserModal';
import userService from '../../services/userService';
import {
  Users,
  UserPlus,
  Search,
  Filter,
  RefreshCw,
  Trash2,
  Shield,
  Clock,
  CheckCircle2,
  Mail,
  UserCheck,
} from 'lucide-react';
import toast from 'react-hot-toast';

export const UserManagementView = () => {
  const [users, setUsers] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedRole, setSelectedRole] = useState('ALL');
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);

  useEffect(() => {
    fetchUsers();
  }, []);

  const fetchUsers = async () => {
    setIsRefreshing(true);
    try {
      const data = await userService.getUsers();
      const rawList = Array.isArray(data) ? data : data?.data || [];
      const normalized = rawList.map((u) => {
        const role =
          u.roleName ||
          u.role ||
          (u.roleId === 1
            ? 'ROLE_ADMIN'
            : u.roleId === 2
            ? 'ROLE_HR'
            : u.roleId === 3
            ? 'ROLE_MENTOR'
            : 'ROLE_STUDENT');
        return {
          ...u,
          id: String(u.id),
          email: u.email,
          role,
          roleName: role,
          roleId:
            u.roleId ||
            (role === 'ROLE_ADMIN' ? 1 : role === 'ROLE_HR' ? 2 : role === 'ROLE_MENTOR' ? 3 : 4),
          status: u.status || 'ACTIVE',
        };
      });
      setUsers(normalized);
    } catch (err) {
      toast.error(err.message || 'Không thể tải danh sách tài khoản');
    } finally {
      setIsLoading(false);
      setIsRefreshing(false);
    }
  };

  const handleDeleteUser = async (id, email) => {
    if (window.confirm(`Bạn có chắc chắn muốn xóa tài khoản "${email}" khỏi hệ thống?`)) {
      try {
        await userService.deleteUser(id);
        toast.success(`Đã xóa tài khoản ${email}.`);
        fetchUsers();
      } catch (err) {
        toast.error(err.message || 'Lỗi khi xóa người dùng');
      }
    }
  };

  // Lọc theo từ khóa tìm kiếm và vai trò
  const filteredUsers = users.filter((u) => {
    const matchesSearch =
      u.email.toLowerCase().includes(searchQuery.toLowerCase().trim()) ||
      u.id.toLowerCase().includes(searchQuery.toLowerCase().trim());
    const matchesRole = selectedRole === 'ALL' || u.role === selectedRole;
    return matchesSearch && matchesRole;
  });

  // Chỉ số tổng quan
  const totalCount = users.length;
  const adminCount = users.filter((u) => u.role === 'ROLE_ADMIN').length;
  const hrCount = users.filter((u) => u.role === 'ROLE_HR').length;
  const mentorCount = users.filter((u) => u.role === 'ROLE_MENTOR').length;
  const studentCount = users.filter((u) => u.role === 'ROLE_STUDENT').length;

  return (
    <DashboardLayout
      title="Quản Lý Tài Khoản Hệ Thống"
      subtitle="Sprint 1 - Story 2: Cấp phát tài khoản, phân quyền RBAC và kiểm soát truy cập"
    >
      <div className="space-y-6">
        {/* Hàng Thẻ Chỉ Số (Metric Cards) */}
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
          <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs hover:shadow-md transition-shadow">
            <div className="flex items-center justify-between">
              <span className="text-xs font-semibold text-slate-500 uppercase tracking-wider">
                Tổng Người Dùng
              </span>
              <div className="w-9 h-9 rounded-xl bg-indigo-50 text-indigo-600 flex items-center justify-center">
                <Users className="w-4 h-4" />
              </div>
            </div>
            <div className="mt-2 flex items-baseline gap-2">
              <span className="text-2xl font-extrabold text-slate-900">{totalCount}</span>
              <span className="text-xs text-indigo-600 font-semibold">Tài khoản</span>
            </div>
          </div>

          <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs hover:shadow-md transition-shadow">
            <div className="flex items-center justify-between">
              <span className="text-xs font-semibold text-slate-500 uppercase tracking-wider">
                Chuyên Viên HR
              </span>
              <div className="w-9 h-9 rounded-xl bg-indigo-50 text-indigo-600 flex items-center justify-center">
                <Shield className="w-4 h-4" />
              </div>
            </div>
            <div className="mt-2 flex items-baseline gap-2">
              <span className="text-2xl font-extrabold text-slate-900">{hrCount}</span>
              <span className="text-xs text-slate-500 font-medium">Nhân sự</span>
            </div>
          </div>

          <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs hover:shadow-md transition-shadow">
            <div className="flex items-center justify-between">
              <span className="text-xs font-semibold text-slate-500 uppercase tracking-wider">
                Mentor Doanh Nghiệp
              </span>
              <div className="w-9 h-9 rounded-xl bg-purple-50 text-purple-600 flex items-center justify-center">
                <UserCheck className="w-4 h-4" />
              </div>
            </div>
            <div className="mt-2 flex items-baseline gap-2">
              <span className="text-2xl font-extrabold text-slate-900">{mentorCount}</span>
              <span className="text-xs text-purple-600 font-medium">Chuyên gia</span>
            </div>
          </div>

          <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs hover:shadow-md transition-shadow">
            <div className="flex items-center justify-between">
              <span className="text-xs font-semibold text-slate-500 uppercase tracking-wider">
                Sinh Viên Thực Tập
              </span>
              <div className="w-9 h-9 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center">
                <Users className="w-4 h-4" />
              </div>
            </div>
            <div className="mt-2 flex items-baseline gap-2">
              <span className="text-2xl font-extrabold text-slate-900">{studentCount}</span>
              <span className="text-xs text-emerald-600 font-medium">Ứng viên</span>
            </div>
          </div>
        </div>

        {/* Thanh Công Cụ Tìm Kiếm & Nút Thao Tác */}
        <div className="bg-white p-4 sm:p-5 rounded-2xl border border-slate-200/80 shadow-xs">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            {/* Thanh Tìm kiếm */}
            <div className="relative flex-1 max-w-md">
              <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400">
                <Search className="w-4 h-4" />
              </div>
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Tìm kiếm theo email hoặc ID người dùng..."
                className="w-full pl-10 pr-4 py-2 text-sm rounded-xl border border-slate-200 bg-slate-50/50 focus:bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-all"
              />
            </div>

            {/* Bộ Lọc & Nút Thêm Mới */}
            <div className="flex items-center gap-3">
              {/* Lọc theo Vai Trò */}
              <select
                value={selectedRole}
                onChange={(e) => setSelectedRole(e.target.value)}
                className="pl-3 pr-8 py-2 text-xs font-semibold rounded-xl border border-slate-200 bg-white text-slate-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-all cursor-pointer"
              >
                <option value="ALL">Tất cả vai trò</option>
                <option value="ROLE_ADMIN">ROLE_ADMIN (Quản trị viên)</option>
                <option value="ROLE_HR">ROLE_HR (Quản lý Nhân sự)</option>
                <option value="ROLE_MENTOR">ROLE_MENTOR (Mentor)</option>
                <option value="ROLE_STUDENT">ROLE_STUDENT (Sinh viên)</option>
              </select>

              {/* Nút Làm mới */}
              <button
                type="button"
                onClick={fetchUsers}
                disabled={isRefreshing}
                className="p-2 text-slate-500 hover:text-slate-700 hover:bg-slate-100 rounded-xl border border-slate-200 transition-colors cursor-pointer"
                title="Làm mới danh sách"
              >
                <RefreshCw
                  className={`w-4 h-4 ${isRefreshing ? 'animate-spin text-indigo-600' : ''}`}
                />
              </button>

              {/* Nút Tạo Người Dùng */}
              <button
                type="button"
                onClick={() => setIsCreateModalOpen(true)}
                className="inline-flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-semibold text-white bg-indigo-600 hover:bg-indigo-700 active:scale-98 transition-all shadow-md shadow-indigo-200 cursor-pointer shrink-0"
              >
                <UserPlus className="w-4 h-4" />
                <span>Tạo Người Dùng Mới</span>
              </button>
            </div>
          </div>
        </div>

        {/* Bảng Dữ Liệu Người Dùng */}
        <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-hidden">
          {isLoading ? (
            <TableSkeleton rows={5} cols={5} />
          ) : filteredUsers.length === 0 ? (
            <div className="py-16 text-center">
              <div className="w-12 h-12 rounded-2xl bg-slate-100 text-slate-400 flex items-center justify-center mx-auto mb-3">
                <Users className="w-6 h-6" />
              </div>
              <h3 className="text-sm font-bold text-slate-800">Không tìm thấy tài khoản nào</h3>
              <p className="text-xs text-slate-500 mt-1">
                {searchQuery || selectedRole !== 'ALL'
                  ? 'Thử điều chỉnh lại từ khóa tìm kiếm hoặc bộ lọc vai trò.'
                  : 'Bắt đầu bằng việc tạo tài khoản đầu tiên.'}
              </p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse">
                <thead>
                  <tr className="border-b border-slate-200/80 bg-slate-50/70 text-[11px] font-bold text-slate-500 uppercase tracking-wider">
                    <th className="py-3.5 px-6">Mã ID</th>
                    <th className="py-3.5 px-6">Tài Khoản Email</th>
                    <th className="py-3.5 px-6">Vai Trò Hệ Thống</th>
                    <th className="py-3.5 px-6">Trạng Thái</th>
                    <th className="py-3.5 px-6">Thời Gian Tạo</th>
                    <th className="py-3.5 px-6 text-right">Thao Tác</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-sm">
                  {filteredUsers.map((u) => (
                    <tr
                      key={u.id}
                      className="hover:bg-slate-50/80 transition-colors group"
                    >
                      <td className="py-4 px-6 font-mono text-xs font-semibold text-slate-700">
                        {u.id}
                      </td>
                      <td className="py-4 px-6">
                        <div className="flex items-center gap-2.5">
                          <div className="w-8 h-8 rounded-lg bg-indigo-50 border border-indigo-100 text-indigo-600 font-bold text-xs flex items-center justify-center shrink-0">
                            {u.email.charAt(0).toUpperCase()}
                          </div>
                          <div>
                            <span className="font-semibold text-slate-900 block">
                              {u.email}
                            </span>
                            <span className="text-[11px] text-slate-400">
                              Xác thực chuẩn JWT Bearer
                            </span>
                          </div>
                        </div>
                      </td>
                      <td className="py-4 px-6">
                        <Badge variant={u.role} roleId={u.roleId} />
                      </td>
                      <td className="py-4 px-6">
                        <span className="inline-flex items-center gap-1.5 text-xs font-medium text-emerald-700">
                          <CheckCircle2 className="w-3.5 h-3.5 text-emerald-500" />
                          {u.status === 'ACTIVE' ? 'Đang hoạt động' : 'Tạm khóa'}
                        </span>
                      </td>
                      <td className="py-4 px-6 text-xs text-slate-500">
                        <div className="flex items-center gap-1.5">
                          <Clock className="w-3.5 h-3.5 text-slate-400" />
                          <span>
                            {u.createdAt
                              ? new Date(u.createdAt).toLocaleDateString('vi-VN', {
                                  year: 'numeric',
                                  month: '2-digit',
                                  day: '2-digit',
                                })
                              : 'Gần đây'}
                          </span>
                        </div>
                      </td>
                      <td className="py-4 px-6 text-right">
                        {u.role !== 'ROLE_ADMIN' ? (
                          <button
                            type="button"
                            onClick={() => handleDeleteUser(u.id, u.email)}
                            className="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors cursor-pointer"
                            title="Xóa tài khoản"
                          >
                            <Trash2 className="w-4 h-4" />
                          </button>
                        ) : (
                          <span className="text-[11px] text-slate-400 italic">
                            Bảo vệ Admin
                          </span>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>

      {/* Modal Tạo Người Dùng */}
      <CreateUserModal
        isOpen={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
        onSuccess={fetchUsers}
      />
    </DashboardLayout>
  );
};

export default UserManagementView;
