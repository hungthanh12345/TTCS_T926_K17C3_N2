import React from 'react';

export const TableSkeleton = ({ rows = 5, cols = 6 }) => {
  return (
    <div className="w-full bg-white divide-y divide-slate-100 animate-pulse">
      {Array.from({ length: rows }).map((_, rIdx) => (
        <div key={rIdx} className="px-6 py-4 flex items-center gap-4">
          {Array.from({ length: cols }).map((_, cIdx) => (
            <div
              key={cIdx}
              className={`h-4 bg-slate-200 rounded ${
                cIdx === 0
                  ? 'w-16'
                  : cIdx === 1
                  ? 'w-48'
                  : cIdx === cols - 1
                  ? 'w-20 ml-auto'
                  : 'w-28'
              }`}
            />
          ))}
        </div>
      ))}
    </div>
  );
};

export default TableSkeleton;
