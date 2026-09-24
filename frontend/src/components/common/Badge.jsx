import React from 'react';

/**
 * Standardized semantic role and status badge configuration
 * Aligned with Sprint 1 design specifications:
 * - ROLE_ADMIN / 1: "Quản trị viên" (Indigo/Blue badge)
 * - ROLE_HR / 2: "Nhân sự (HR)" (Purple badge)
 * - ROLE_MENTOR / 3: "Mentor Doanh nghiệp" (Amber/Orange badge)
 * - ROLE_STUDENT / 4: "Thực tập sinh" (Emerald/Green badge)
 */
const ROLE_MAP = {
  ROLE_ADMIN: {
    label: 'Quản trị viên',
    style: 'bg-indigo-50 text-indigo-700 border-indigo-200/80 ring-indigo-500/10',
    dot: 'bg-indigo-500',
  },
  1: {
    label: 'Quản trị viên',
    style: 'bg-indigo-50 text-indigo-700 border-indigo-200/80 ring-indigo-500/10',
    dot: 'bg-indigo-500',
  },
  '1': {
    label: 'Quản trị viên',
    style: 'bg-indigo-50 text-indigo-700 border-indigo-200/80 ring-indigo-500/10',
    dot: 'bg-indigo-500',
  },

  ROLE_HR: {
    label: 'Nhân sự (HR)',
    style: 'bg-purple-50 text-purple-700 border-purple-200/80 ring-purple-500/10',
    dot: 'bg-purple-500',
  },
  2: {
    label: 'Nhân sự (HR)',
    style: 'bg-purple-50 text-purple-700 border-purple-200/80 ring-purple-500/10',
    dot: 'bg-purple-500',
  },
  '2': {
    label: 'Nhân sự (HR)',
    style: 'bg-purple-50 text-purple-700 border-purple-200/80 ring-purple-500/10',
    dot: 'bg-purple-500',
  },

  ROLE_MENTOR: {
    label: 'Mentor Doanh nghiệp',
    style: 'bg-amber-50 text-amber-700 border-amber-200/80 ring-amber-500/10',
    dot: 'bg-amber-500',
  },
  3: {
    label: 'Mentor Doanh nghiệp',
    style: 'bg-amber-50 text-amber-700 border-amber-200/80 ring-amber-500/10',
    dot: 'bg-amber-500',
  },
  '3': {
    label: 'Mentor Doanh nghiệp',
    style: 'bg-amber-50 text-amber-700 border-amber-200/80 ring-amber-500/10',
    dot: 'bg-amber-500',
  },

  ROLE_STUDENT: {
    label: 'Thực tập sinh',
    style: 'bg-emerald-50 text-emerald-700 border-emerald-200/80 ring-emerald-500/10',
    dot: 'bg-emerald-500',
  },
  4: {
    label: 'Thực tập sinh',
    style: 'bg-emerald-50 text-emerald-700 border-emerald-200/80 ring-emerald-500/10',
    dot: 'bg-emerald-500',
  },
  '4': {
    label: 'Thực tập sinh',
    style: 'bg-emerald-50 text-emerald-700 border-emerald-200/80 ring-emerald-500/10',
    dot: 'bg-emerald-500',
  },
};

const STATUS_MAP = {
  ACTIVE: {
    label: 'Đang hoạt động',
    style: 'bg-emerald-50 text-emerald-700 border-emerald-200/80 ring-emerald-500/10',
    dot: 'bg-emerald-500',
  },
  INACTIVE: {
    label: 'Tạm khóa',
    style: 'bg-slate-100 text-slate-600 border-slate-200 ring-slate-400/10',
    dot: 'bg-slate-400',
  },
  LOCKED: {
    label: 'Đã khóa',
    style: 'bg-rose-50 text-rose-700 border-rose-200 ring-rose-500/10',
    dot: 'bg-rose-500',
  },
  PENDING_ASSIGNMENT: {
    label: 'Chờ ghép Mentor',
    style: 'bg-amber-50 text-amber-700 border-amber-200/80 ring-amber-500/10',
    dot: 'bg-amber-500',
  },
  COMPLETED: {
    label: 'Đã hoàn thành',
    style: 'bg-blue-50 text-blue-700 border-blue-200/80 ring-blue-500/10',
    dot: 'bg-blue-500',
  },
};

const GENERIC_VARIANTS = {
  indigo: {
    style: 'bg-indigo-50 text-indigo-700 border-indigo-200/80 ring-indigo-500/10',
    dot: 'bg-indigo-500',
  },
  purple: {
    style: 'bg-purple-50 text-purple-700 border-purple-200/80 ring-purple-500/10',
    dot: 'bg-purple-500',
  },
  amber: {
    style: 'bg-amber-50 text-amber-700 border-amber-200/80 ring-amber-500/10',
    dot: 'bg-amber-500',
  },
  emerald: {
    style: 'bg-emerald-50 text-emerald-700 border-emerald-200/80 ring-emerald-500/10',
    dot: 'bg-emerald-500',
  },
  rose: {
    style: 'bg-rose-50 text-rose-700 border-rose-200/80 ring-rose-500/10',
    dot: 'bg-rose-500',
  },
  slate: {
    style: 'bg-slate-100 text-slate-700 border-slate-200 ring-slate-400/10',
    dot: 'bg-slate-400',
  },
  sky: {
    style: 'bg-sky-50 text-sky-700 border-sky-200/80 ring-sky-500/10',
    dot: 'bg-sky-500',
  },
};

export const Badge = ({
  variant = 'slate',
  role,
  roleId,
  roleName,
  children,
  dot = true,
  className = '',
}) => {
  // Determine lookup key from role / roleName / roleId / variant
  const key = role || roleName || roleId || variant;
  const config =
    ROLE_MAP[key] ||
    STATUS_MAP[key] ||
    GENERIC_VARIANTS[variant] ||
    GENERIC_VARIANTS.slate;

  const styleClass = config.style || GENERIC_VARIANTS.slate.style;
  const dotClass = config.dot || 'bg-slate-400';
  const label = children || config.label || (typeof key === 'string' && key !== 'slate' ? key : 'Chưa phân quyền');

  return (
    <span
      className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold border shadow-2xs tracking-tight ${styleClass} ${className}`}
    >
      {dot && <span className={`w-1.5 h-1.5 rounded-full ${dotClass}`} />}
      {label}
    </span>
  );
};

export default Badge;
