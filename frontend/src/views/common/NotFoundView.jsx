import React from 'react';
import { Link } from 'react-router-dom';
import { Layers, ArrowLeft } from 'lucide-react';

export const NotFoundView = () => {
  return (
    <div className="min-h-screen bg-slate-950 flex items-center justify-center p-4 text-white">
      <div className="max-w-md w-full bg-slate-900 rounded-3xl border border-slate-800 p-8 text-center shadow-2xl">
        <div className="w-16 h-16 rounded-2xl bg-indigo-500/10 text-indigo-400 border border-indigo-500/20 flex items-center justify-center mx-auto mb-4">
          <Layers className="w-8 h-8" />
        </div>
        <h1 className="text-4xl font-extrabold tracking-tight text-white">404</h1>
        <h2 className="text-lg font-bold text-slate-200 mt-2">Trang Không Tồn Tại</h2>
        <p className="text-sm text-slate-400 mt-1 leading-relaxed">
          Đường dẫn bạn yêu cầu không tồn tại hoặc đã được thay đổi trong Hệ thống Quản lý Thực tập sinh.
        </p>

        <Link
          to="/login"
          className="mt-6 inline-flex items-center gap-2 px-5 py-3 rounded-xl text-sm font-bold text-white bg-indigo-600 hover:bg-indigo-500 transition-all shadow-lg shadow-indigo-600/30"
        >
          <ArrowLeft className="w-4 h-4" />
          <span>Về Cổng Thông Tin</span>
        </Link>
      </div>
    </div>
  );
};

export default NotFoundView;
