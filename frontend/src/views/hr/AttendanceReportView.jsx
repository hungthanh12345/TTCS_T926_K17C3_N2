import { useState, useEffect } from 'react';
import api from '../../services/api';
import toast from 'react-hot-toast';

export default function AttendanceReportView() {
  const [reports, setReports] = useState([]);
  const [loading, setLoading] = useState(false);
  
  const [filters, setFilters] = useState({
    startDate: '',
    endDate: '',
    studentId: '',
    page: 1,
    pageSize: 20
  });

  const [students, setStudents] = useState([]);
  const [pagination, setPagination] = useState({ totalCount: 0, totalPages: 1 });

  useEffect(() => {
    fetchStudents();
  }, []);

  useEffect(() => {
    fetchReports();
  }, [filters.page, filters.pageSize]);

  const fetchStudents = async () => {
    try {
      const res = await api.get('/Students');
      setStudents(res.data.data?.items || res.data.data || []);
    } catch (error) {
      console.error(error);
    }
  };

  const fetchReports = async () => {
    // Validate dates
    if (filters.startDate && filters.endDate && filters.startDate > filters.endDate) {
      toast.error('Ngày bắt đầu không được lớn hơn ngày kết thúc');
      return;
    }

    try {
      setLoading(true);
      const params = {
        page: filters.page,
        pageSize: filters.pageSize
      };
      if (filters.startDate) params.startDate = filters.startDate;
      if (filters.endDate) params.endDate = filters.endDate;
      if (filters.studentId) params.studentId = filters.studentId;

      const res = await api.get('/AttendanceReports', { params });
      
      const { items, totalCount, page, pageSize } = res.data.data;
      setReports(items || []);
      setPagination({
        totalCount,
        totalPages: Math.ceil(totalCount / pageSize)
      });
    } catch (error) {
      toast.error('Lỗi khi tải báo cáo chấm công');
    } finally {
      setLoading(false);
    }
  };

  const handleFilterSubmit = (e) => {
    e.preventDefault();
    setFilters({ ...filters, page: 1 });
    fetchReports();
  };

  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold text-gray-800 mb-6">Báo Cáo Đi Làm & Nghỉ Phép</h1>

      <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-5 mb-6">
        <form onSubmit={handleFilterSubmit} className="flex flex-wrap gap-4 items-end">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Từ ngày</label>
            <input
              type="date"
              className="border border-gray-300 rounded-lg p-2 focus:ring-2 focus:ring-blue-500"
              value={filters.startDate}
              onChange={e => setFilters({...filters, startDate: e.target.value})}
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Đến ngày</label>
            <input
              type="date"
              className="border border-gray-300 rounded-lg p-2 focus:ring-2 focus:ring-blue-500"
              value={filters.endDate}
              onChange={e => setFilters({...filters, endDate: e.target.value})}
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Thực tập sinh</label>
            <select
              className="border border-gray-300 rounded-lg p-2 focus:ring-2 focus:ring-blue-500 min-w-[200px]"
              value={filters.studentId}
              onChange={e => setFilters({...filters, studentId: e.target.value})}
            >
              <option value="">-- Tất cả --</option>
              {students.map(s => (
                <option key={s.id} value={s.id}>{s.fullName} ({s.studentCode})</option>
              ))}
            </select>
          </div>
          <button
            type="submit"
            className="bg-blue-600 hover:bg-blue-700 text-white px-5 py-2 rounded-lg font-medium transition-colors"
          >
            Lọc Báo Cáo
          </button>
        </form>
      </div>

      <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
        <table className="w-full text-left border-collapse">
          <thead>
            <tr className="bg-gray-50 border-b border-gray-100">
              <th className="p-4 font-semibold text-gray-600">Mã Sinh Viên</th>
              <th className="p-4 font-semibold text-gray-600">Họ Tên</th>
              <th className="p-4 font-semibold text-gray-600 text-center">Số ngày đi làm</th>
              <th className="p-4 font-semibold text-gray-600 text-center">Số lần đi muộn</th>
              <th className="p-4 font-semibold text-gray-600 text-center">Số ngày vắng mặt</th>
              <th className="p-4 font-semibold text-gray-600 text-center">Nghỉ phép (đã duyệt)</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr><td colSpan="6" className="p-8 text-center text-gray-500">Đang tải dữ liệu...</td></tr>
            ) : reports.length === 0 ? (
              <tr><td colSpan="6" className="p-8 text-center text-gray-500">Không có dữ liệu phù hợp với bộ lọc</td></tr>
            ) : (
              reports.map(r => (
                <tr key={r.studentId} className="border-b border-gray-50 hover:bg-gray-50 transition-colors">
                  <td className="p-4 text-gray-600 font-medium">{r.studentCode}</td>
                  <td className="p-4 font-medium text-gray-900">{r.fullName}</td>
                  <td className="p-4 text-center text-emerald-600 font-medium">{r.totalPresent}</td>
                  <td className="p-4 text-center text-orange-500 font-medium">{r.totalLate}</td>
                  <td className="p-4 text-center text-red-600 font-medium">{r.totalAbsent}</td>
                  <td className="p-4 text-center text-blue-600 font-medium">{r.totalLeaveApproved}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
        
        {/* Pagination */}
        {pagination.totalPages > 1 && (
          <div className="p-4 border-t flex justify-end gap-2">
            <button 
              disabled={filters.page === 1}
              onClick={() => setFilters(f => ({ ...f, page: f.page - 1 }))}
              className="px-3 py-1 border rounded disabled:opacity-50"
            >
              Trước
            </button>
            <span className="px-3 py-1 text-sm font-medium">Trang {filters.page} / {pagination.totalPages}</span>
            <button 
              disabled={filters.page === pagination.totalPages}
              onClick={() => setFilters(f => ({ ...f, page: f.page + 1 }))}
              className="px-3 py-1 border rounded disabled:opacity-50"
            >
              Sau
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
