import React, { useEffect, useState } from 'react';
import Sidebar from './Sidebar';
import Header from './Header';

export const DashboardLayout = ({ children, title, subtitle }) => {
  const [isMobileOpen, setIsMobileOpen] = useState(false);
  const [isDesktopOpen, setIsDesktopOpen] = useState(true);
  const [isDesktopViewport, setIsDesktopViewport] = useState(() =>
    window.matchMedia?.('(min-width: 1024px)').matches ?? window.innerWidth >= 1024
  );

  useEffect(() => {
    const mediaQuery = window.matchMedia?.('(min-width: 1024px)');
    const updateViewport = (matches) => {
      setIsDesktopViewport(matches);
      if (matches) setIsMobileOpen(false);
    };

    if (mediaQuery) {
      updateViewport(mediaQuery.matches);
      const handleChange = (event) => updateViewport(event.matches);
      mediaQuery.addEventListener('change', handleChange);
      return () => mediaQuery.removeEventListener('change', handleChange);
    }

    const handleResize = () => updateViewport(window.innerWidth >= 1024);
    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, []);

  const toggleSidebar = () => {
    if (isDesktopViewport) {
      setIsDesktopOpen((open) => !open);
      return;
    }

    setIsMobileOpen((open) => !open);
  };

  return (
    <div className="min-h-screen bg-slate-50 flex">
      {/* Sidebar */}
      <Sidebar
        isMobileOpen={isMobileOpen}
        setIsMobileOpen={setIsMobileOpen}
        isDesktopOpen={isDesktopOpen}
      />

      {/* Main Content Area */}
      <div className={`flex-1 flex flex-col min-w-0 transition-[padding] duration-300 ${isDesktopOpen ? 'lg:pl-64' : 'lg:pl-0'}`}>
        {/* Header */}
        <Header
          onToggleSidebar={toggleSidebar}
          isDesktopViewport={isDesktopViewport}
          isDesktopSidebarOpen={isDesktopOpen}
          isMobileSidebarOpen={isMobileOpen}
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
