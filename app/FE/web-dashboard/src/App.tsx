import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { ConfigProvider, theme } from 'antd';
import { useSelector } from 'react-redux';
import { selectIsAuthenticated } from '@/store/slices/authSlice';
import LoginPage from '@/pages/auth/LoginPage';
import DashboardLayout from '@/layouts/DashboardLayout';
import TableManagementPage from '@/pages/dashboard/TableManagementPage';
import DashboardPage from '@/pages/dashboard/DashboardPage';
import MenuManagementPage from '@/pages/dashboard/MenuManagementPage';
import StaffManagementPage from '@/pages/dashboard/StaffManagementPage';
import TransactionsPage from '@/pages/dashboard/TransactionsPage';
import SettingsPage from '@/pages/dashboard/SettingsPage';
import StaffDashboardPage from '@/pages/dashboard/StaffDashboardPage';
import AdminDashboard from '@/pages/admin/AdminDashboard';
import AdminOverview from '@/pages/admin/AdminOverview';
import AdminMenu from '@/pages/admin/AdminMenu';
import AdminOrders from '@/pages/admin/AdminOrders';
import AdminTable from '@/pages/admin/AdminTable';
import AdminKitchen from '@/pages/admin/AdminKitchen';
import AdminStaff from '@/pages/admin/AdminStaff';
import AdminAnalytics from '@/pages/admin/AdminAnalytics';
import AdminSettings from '@/pages/admin/AdminSettings';
import StaffDashboard from '@/pages/admin/StaffDashboard';

import { selectCurrentUser } from '@/store/slices/authSlice';
import { getDefaultRoute } from '@/utils/roleUtils';
import RestaurantDrawPage from '@/pages/draw_map/RestaurantDrawPage';


// Protected Route Guard checking both login and roles
const RoleProtectedRoute: React.FC<{ children: React.ReactNode; allowedRoles: string[] }> = ({ children, allowedRoles }) => {
  const isAuthenticated = useSelector(selectIsAuthenticated);
  const currentUser = useSelector(selectCurrentUser);

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (currentUser && !allowedRoles.includes(currentUser.role)) {
    // Redirect unauthorized roles to their default home page
    return <Navigate to={getDefaultRoute(currentUser.role)} replace />;
  }

  return <>{children}</>;
};

// Dynamic index redirection based on user's role
const RoleIndexRedirect: React.FC = () => {
  const currentUser = useSelector(selectCurrentUser);
  if (currentUser) {
    return <Navigate to={getDefaultRoute(currentUser.role)} replace />;
  }
  return <Navigate to="/login" replace />;
};

const App: React.FC = () => {
  return (
    <ConfigProvider
      theme={{
        algorithm: theme.defaultAlgorithm, // Light theme matching the screenshot
        token: {
          colorPrimary: '#2563EB',
          borderRadius: 6,
        },
      }}
    >
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />

          <Route path="/admin-v2" element={<AdminDashboard />}>
            <Route index element={<AdminOverview />} />
            <Route path="menu" element={<AdminMenu />} />
            <Route path="tables" element={<AdminTable />} />
            <Route path="staff" element={<AdminStaff />} />
            <Route path="analytics" element={<AdminAnalytics />} />
            <Route path="settings" element={<AdminSettings />} />
          </Route>

          <Route path="/staffboard" element={<StaffDashboard />}>
            <Route index element={<Navigate to="/staffboard/kitchen" replace />} />
            <Route path="kitchen" element={<AdminKitchen />} />
            <Route path="orders" element={<AdminOrders />} />
            <Route path="draw-map" element={<RestaurantDrawPage />} />
          </Route>

          {/* Nested Dashboard Routes inside DashboardLayout layout */}
          <Route
            path="/*"
            element={
              <RoleProtectedRoute allowedRoles={['MANAGER', 'STAFF', 'CUSTOMER']}>
                <DashboardLayout />
              </RoleProtectedRoute>
            }
          >
            <Route index element={<RoleIndexRedirect />} />

            <Route
              path="dashboard"
              element={
                <RoleProtectedRoute allowedRoles={['MANAGER']}>
                  <DashboardPage />
                </RoleProtectedRoute>
              }
            />
            <Route
              path="tables"
              element={
                <RoleProtectedRoute allowedRoles={['MANAGER']}>
                  <TableManagementPage />
                </RoleProtectedRoute>
              }
            />
            <Route
              path="menu"
              element={
                <RoleProtectedRoute allowedRoles={['MANAGER', 'CUSTOMER']}>
                  <MenuManagementPage />
                </RoleProtectedRoute>
              }
            />
            <Route
              path="staff"
              element={
                <RoleProtectedRoute allowedRoles={['MANAGER']}>
                  <StaffManagementPage />
                </RoleProtectedRoute>
              }
            />
            <Route
              path="transactions"
              element={
                <RoleProtectedRoute allowedRoles={['MANAGER']}>
                  <TransactionsPage />
                </RoleProtectedRoute>
              }
            />
            <Route
              path="settings"
              element={
                // BE SettingsController chỉ [Authorize(Roles = Roles.Manager)] — STAFF gọi sẽ bị 403.
                <RoleProtectedRoute allowedRoles={['MANAGER']}>
                  <SettingsPage />
                </RoleProtectedRoute>
              }
            />
            {/* Fallback route within dashboard layout */}
            <Route path="*" element={<RoleIndexRedirect />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </ConfigProvider>
  );
};


export default App;
