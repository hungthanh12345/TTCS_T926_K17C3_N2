import React, { useState } from 'react';
import Sidebar from './Sidebar';
import Header from './Header';

export const DashboardLayout = ({ children, title, subtitle }) => {
  const [isMobileOpen, setIsMobileOpen] = useState(false);

  return (
    <div className="min-h-screen bg-slate-50 flex">
      {/* Sidebar */}
      <Sidebar isMobileOpen={isMobileOpen} setIsMobileOpen={setIsMobileOpen} />

      {/* Main Content Area */}
      <div className="flex-1 flex flex-col lg:pl-64 min-w-0">
        {/* Header */}
        <Header
          onOpenMobileSidebar={() => setIsMobileOpen(true)}
          title={title}
          subtitle={subtitle}
        />

        {/* Dynamic Page Body */}
        <main className="flex-1 p-4 sm:p-6 lg:p-8 max-w-7xl w-full mx-auto">
          {children}
        </main>
      </div>
    </div>
  );
};

export default DashboardLayout;
