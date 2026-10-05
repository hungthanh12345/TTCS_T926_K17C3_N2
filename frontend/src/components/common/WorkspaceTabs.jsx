import React from 'react';

const WorkspaceTabs = ({ tabs, activeTab, onChange, label }) => (
  <nav
    aria-label={label}
    className="flex gap-1 overflow-x-auto rounded-xl border border-slate-200 bg-white p-1.5 shadow-sm"
  >
    {tabs.map(({ id, label: tabLabel, icon: Icon, count }) => {
      const isActive = activeTab === id;

      return (
        <button
          key={id}
          type="button"
          onClick={() => onChange(id)}
          aria-current={isActive ? 'page' : undefined}
          className={`inline-flex shrink-0 items-center gap-2 rounded-lg px-3.5 py-2.5 text-xs font-semibold transition-colors ${
            isActive
              ? 'bg-indigo-600 text-white shadow-sm'
              : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
          }`}
        >
          {Icon && <Icon className="h-4 w-4" aria-hidden="true" />}
          <span>{tabLabel}</span>
          {count !== undefined && (
            <span
              className={`rounded-full px-1.5 py-0.5 text-[10px] font-bold ${
                isActive ? 'bg-white/15 text-white' : 'bg-slate-100 text-slate-500'
              }`}
            >
              {count}
            </span>
          )}
        </button>
      );
    })}
  </nav>
);

export default WorkspaceTabs;
