import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Toaster } from 'react-hot-toast';
import { AuthProvider } from './context/AuthContext';
import ProtectedRoute from './routes/ProtectedRoute';

// Views
import LoginView from './views/auth/LoginView';
import UserManagementView from './views/admin/UserManagementView';
import StudentManagementView from './views/hr/StudentManagementView';
import MentorManagementView from './views/hr/MentorManagementView';
import MentorDashboardView from './views/mentor/MentorDashboardView';
import StudentDashboardView from './views/student/StudentDashboardView';
import NotFoundView from './views/common/NotFoundView';

export const App = () => {
  return (
    <AuthProvider>
      <BrowserRouter>
        {/* Global Toast Provider with Enterprise Aesthetics */}
        <Toaster
          position="top-right"
          toastOptions={{
            duration: 4000,
            style: {
              background: '#0f172a',
              color: '#f8fafc',
              fontSize: '13px',
              fontWeight: 500,
              borderRadius: '12px',
              padding: '12px 16px',
              boxShadow: '0 20px 25px -5px rgb(0 0 0 / 0.1), 0 8px 10px -6px rgb(0 0 0 / 0.1)',
            },
            success: {
              iconTheme: {
                primary: '#10b981',
                secondary: '#ffffff',
              },
            },
            error: {
              iconTheme: {
                primary: '#ef4444',
                secondary: '#ffffff',
              },
            },
          }}
        />

        <Routes>
          {/* Public Authentication Route */}
          <Route path="/login" element={<LoginView />} />

          {/* Root Route: Always navigate to /login on app launch / root visit */}
          <Route path="/" element={<Navigate to="/login" replace />} />

          {/* Story 2: Admin Dashboard & Account Management */}
          <Route
            path="/admin/users"
            element={
              <ProtectedRoute allowedRoles={['ROLE_ADMIN']}>
                <UserManagementView />
              </ProtectedRoute>
            }
          />

          {/* Story 3 & 4: HR Student Profile & Mentor Assignment */}
          <Route
            path="/hr/students"
            element={
              <ProtectedRoute allowedRoles={['ROLE_ADMIN', 'ROLE_HR']}>
                <StudentManagementView />
              </ProtectedRoute>
            }
          />

          {/* Story 4: HR Mentor Management & Directory */}
          <Route
            path="/hr/mentors"
            element={
              <ProtectedRoute allowedRoles={['ROLE_ADMIN', 'ROLE_HR']}>
                <MentorManagementView />
              </ProtectedRoute>
            }
          />

          {/* Mentor Supervisory View */}
          <Route
            path="/mentor/students"
            element={
              <ProtectedRoute allowedRoles={['ROLE_ADMIN', 'ROLE_MENTOR']}>
                <MentorDashboardView />
              </ProtectedRoute>
            }
          />
          <Route path="/mentor/dashboard" element={<Navigate to="/mentor/students" replace />} />

          {/* Student Internship Profile View */}
          <Route
            path="/student/profile"
            element={
              <ProtectedRoute allowedRoles={['ROLE_ADMIN', 'ROLE_STUDENT']}>
                <StudentDashboardView />
              </ProtectedRoute>
            }
          />
          <Route path="/student/dashboard" element={<Navigate to="/student/profile" replace />} />

          {/* Fallback 404 Route */}
          <Route path="*" element={<NotFoundView />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
};

export default App;
