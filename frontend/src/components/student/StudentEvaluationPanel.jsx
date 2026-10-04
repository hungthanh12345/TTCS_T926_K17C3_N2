import React, { useEffect, useState } from 'react';
import { Award, Loader2 } from 'lucide-react';
import internshipEvaluationService from '../../services/internshipEvaluationService';

const formatDate = (value) => value ? new Date(value).toLocaleDateString('vi-VN') : '—';

export const StudentEvaluationPanel = () => {
  const [evaluations, setEvaluations] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    let isActive = true;
    internshipEvaluationService.getMine()
      .then((items) => { if (isActive) setEvaluations(items); })
      .catch((loadError) => { if (isActive) setError(loadError.message || 'Không thể tải đánh giá.'); })
      .finally(() => { if (isActive) setIsLoading(false); });
    return () => { isActive = false; };
  }, []);

  return (
    <section className="space-y-4 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
      <div>
        <h3 className="flex items-center gap-2 text-base font-bold text-slate-900"><Award className="h-5 w-5 text-indigo-600" /> Đánh giá thực tập</h3>
        <p className="mt-1 text-xs text-slate-500">Kết quả do Mentor phụ trách ghi nhận.</p>
      </div>
      {isLoading ? <div role="status" className="flex items-center gap-2 py-5 text-sm text-slate-500"><Loader2 className="h-4 w-4 animate-spin" /> Đang tải đánh giá...</div> : error ? (
        <div role="alert" className="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800">{error}</div>
      ) : evaluations.length === 0 ? (
        <p role="status" className="rounded-xl border border-dashed border-slate-300 px-4 py-7 text-center text-sm text-slate-500">Mentor chưa gửi đánh giá cho bạn.</p>
      ) : evaluations.map((evaluation) => (
        <article key={evaluation.id} className="rounded-xl border border-slate-200 p-4 sm:p-5">
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div><h4 className="text-sm font-bold text-slate-900">{evaluation.mentorName}</h4><p className="mt-1 text-xs text-slate-500">Đánh giá ngày {formatDate(evaluation.evaluatedAt)}</p></div>
            <span className="rounded-full bg-indigo-50 px-3 py-1.5 text-xs font-bold text-indigo-700">Tổng điểm {evaluation.overallScore}/10</span>
          </div>
          <div className="mt-4 grid gap-3 sm:grid-cols-2">
            <div className="rounded-lg bg-slate-50 p-3"><p className="text-xs text-slate-500">Kỹ năng</p><p className="mt-1 text-lg font-bold text-slate-900">{evaluation.skillsScore}/10</p></div>
            <div className="rounded-lg bg-slate-50 p-3"><p className="text-xs text-slate-500">Thái độ</p><p className="mt-1 text-lg font-bold text-slate-900">{evaluation.attitudeScore}/10</p></div>
          </div>
          <div className="mt-4"><h5 className="text-xs font-bold text-slate-500">NHẬN XÉT</h5><p className="mt-1 whitespace-pre-wrap text-sm leading-6 text-slate-700">{evaluation.comments}</p></div>
        </article>
      ))}
    </section>
  );
};

export default StudentEvaluationPanel;
