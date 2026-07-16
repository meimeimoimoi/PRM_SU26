import React, { useMemo } from 'react';
import { Link, useLocation, Outlet } from 'react-router-dom';
import { Layout, Menu, Typography, Avatar, Dropdown, Space } from 'antd';
import { 
  AppstoreOutlined, 
  TableOutlined, 
  CoffeeOutlined, 
  TeamOutlined, 
  SettingOutlined, 
  LogoutOutlined,
  UserOutlined,
  HistoryOutlined,
  FireOutlined
} from '@ant-design/icons';
import { useSelector, useDispatch } from 'react-redux';
import { selectCurrentUser, logout } from '@/store/slices/authSlice';
import NotificationBell from '@/components/NotificationBell';

const { Header, Content, Sider } = Layout;
const { Title } = Typography;

const DashboardLayout: React.FC = () => {
  const user = useSelector(selectCurrentUser);
  const dispatch = useDispatch();
  const location = useLocation();

  const sidebarMenuItems = useMemo(() => {
    if (!user) return [];
    const role = user.role.toUpperCase();
    const items = [];

    if (role === 'MANAGER') {
      items.push({ 
        key: 'dashboard', 
        icon: <AppstoreOutlined style={{ fontSize: 16 }} />, 
        label: <Link to="/dashboard">Dashboard</Link> 
      });
    }

    if (role === 'MANAGER') {
      items.push({ 
        key: 'tables', 
        icon: <TableOutlined style={{ fontSize: 16 }} />, 
        label: <Link to="/tables">Table Management</Link> 
      });
    }

    if (role === 'MANAGER' || role === 'CUSTOMER') {
      items.push({ 
        key: 'menu', 
        icon: <CoffeeOutlined style={{ fontSize: 16 }} />, 
        label: <Link to="/menu">Menu Management</Link> 
      });
    }

    if (role === 'MANAGER') {
      items.push({ 
        key: 'staff', 
        icon: <TeamOutlined style={{ fontSize: 16 }} />, 
        label: <Link to="/staff">Staff</Link> 
      });
    }

    if (role === 'MANAGER') {
      items.push({ 
        key: 'transactions', 
        icon: <HistoryOutlined style={{ fontSize: 16 }} />, 
        label: <Link to="/transactions">Transactions</Link> 
      });
    }

    if (role === 'MANAGER' || role === 'STAFF') {
      items.push({ 
        key: 'staff-dashboard', 
        icon: <FireOutlined style={{ fontSize: 16 }} />, 
        label: <Link to="/staff-dashboard">Kitchen & Billing</Link> 
      });
    }

    return items;
  }, [user]);

  const getSelectedKey = () => {
    const path = location.pathname;
    if (path.includes('/dashboard')) return 'dashboard';
    if (path.includes('/tables')) return 'tables';
    if (path.includes('/menu')) return 'menu';
    if (path.includes('/staff')) return 'staff';
    if (path.includes('/transactions')) return 'transactions';
    if (path.includes('/staff-dashboard')) return 'staff-dashboard';
    if (path.includes('/settings')) return 'settings';
    return 'dashboard'; // Default key
  };

  const getPageTitle = () => {
    const path = location.pathname;
    if (path.includes('/dashboard')) return 'RestoAdmin Dashboard';
    if (path.includes('/tables')) return 'Table Management';
    if (path.includes('/menu')) return 'Menu Management';
    if (path.includes('/staff')) return 'Staff Management';
    if (path.includes('/transactions')) return 'Order & Transaction History';
    if (path.includes('/staff-dashboard')) return 'Kitchen & Billing Dashboard';
    if (path.includes('/settings')) return 'System Settings';
    return 'SmartDine Admin';
  };

  const selectedKey = getSelectedKey();

  const userMenuItems = [
    {
      key: 'profile',
      label: <span>Thông tin cá nhân</span>,
      icon: <UserOutlined />,
    },
    {
      key: 'logout',
      label: <span>Đăng xuất</span>,
      icon: <LogoutOutlined />,
      danger: true,
      onClick: () => dispatch(logout()),
    }
  ];

  // RestoAdmin styling condition for transactions page
  const isTransactions = location.pathname.includes('/transactions');

  return (
    <Layout style={{ minHeight: '100vh', width: '100vw', background: '#f0f2f5' }}>
      {/* Left Sidebar (Sider) */}
      <Sider 
        theme="light" 
        width={250} 
        style={{ 
          borderRight: '1px solid #f0f0f0', 
          height: '100vh', 
          position: 'fixed', 
          left: 0, 
          top: 0,
          zIndex: 100
        }}
      >
        <div style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
          {/* Brand Logo & Profile Header */}
          <div style={{ 
            padding: '20px 24px', 
            borderBottom: '1px solid #f5f5f5',
            display: 'flex',
            alignItems: 'center',
            gap: 12
          }}>
            {isTransactions ? (
              <Avatar 
                style={{ backgroundColor: '#1890ff', fontWeight: 'bold' }}
              >
                RA
              </Avatar>
            ) : (
              <Avatar 
                src="https://api.dicebear.com/7.x/miniavs/svg?seed=SmartDineAdmin" 
                style={{ backgroundColor: '#1890ff' }}
              />
            )}
            <div>
              <div style={{ fontWeight: 700, color: '#1a202c', fontSize: '15px', lineHeight: 1.2 }}>
                {user?.fullName || 'SmartDine User'}
              </div>
              <div style={{ color: '#718096', fontSize: '11px', marginTop: 2, textTransform: 'capitalize' }}>
                {user?.role ? user.role.toLowerCase() : 'Restaurant Guest'}
              </div>
            </div>
          </div>
          
          {/* Top navigation menu items */}
          <div style={{ flex: 1, paddingTop: 16 }}>
            <Menu
              theme="light"
              mode="inline"
              selectedKeys={[selectedKey]}
              style={{ borderRight: 0 }}
              items={sidebarMenuItems}
            />
          </div>
          
          <div style={{ borderTop: '1px solid #f5f5f5', paddingTop: 8 }}>
            {/* BE SettingsController chỉ Manager truy cập được — STAFF không thấy link này. */}
            {user && user.role === 'MANAGER' && (
              <Menu
                theme="light"
                mode="inline"
                selectedKeys={[selectedKey]}
                style={{ borderRight: 0 }}
                items={[
                  { 
                    key: 'settings', 
                    icon: <SettingOutlined style={{ fontSize: 16 }} />, 
                    label: <Link to="/settings">Settings</Link> 
                  },
                ]}
              />
            )}
            {/* Custom Logout button matching mockup 3 */}
            <div 
              onClick={() => dispatch(logout())}
              style={{ 
                padding: '12px 24px', 
                display: 'flex', 
                alignItems: 'center', 
                gap: 10, 
                color: '#ff4d4f', 
                cursor: 'pointer',
                fontWeight: 500,
                fontSize: '14px',
                marginTop: 4,
                marginBottom: 16
              }}
              onMouseEnter={(e) => { e.currentTarget.style.backgroundColor = '#fff1f0'; }}
              onMouseLeave={(e) => { e.currentTarget.style.backgroundColor = 'transparent'; }}
            >
              <LogoutOutlined />
              <span>Logout</span>
            </div>
          </div>
        </div>
      </Sider>

      {/* Right Content Pane */}
      <Layout style={{ marginLeft: 250, background: '#f5f7fa' }}>
        {/* Top Header */}
        <Header style={{ 
          background: '#fff', 
          padding: '0 24px', 
          display: 'flex', 
          alignItems: 'center', 
          justifyContent: 'space-between',
          borderBottom: '1px solid #e8e8e8',
          height: 64
        }}>
          {/* "Search orders, tables..." decorative box đã bỏ — không nối vào đâu, trong khi
              trang Transactions đã có ô tìm kiếm thật riêng (searchText) ở toolbar của trang. */}
          <Title level={4} style={{ margin: 0, fontWeight: 600, color: '#1a202c' }}>
            {isTransactions ? 'RestoAdmin Dashboard' : getPageTitle()}
          </Title>

          <Space size={16} style={{ marginLeft: 16 }}>
            {user && (user.role === 'MANAGER' || user.role === 'STAFF') && <NotificationBell />}
            <Dropdown menu={{ items: userMenuItems }} placement="bottomRight" trigger={['click']}>
              <Space style={{ cursor: 'pointer' }}>
                <span style={{ color: '#4a5568', fontWeight: 500 }}>
                  {user?.fullName || 'Quản trị viên'}
                </span>
                <Avatar 
                  src="https://api.dicebear.com/7.x/miniavs/svg?seed=SmartDineAdmin" 
                  style={{ backgroundColor: '#1890ff' }}
                />
              </Space>
            </Dropdown>
          </Space>
        </Header>

        {/* Content Body */}
        <Content style={{ padding: 24, minHeight: 'calc(100vh - 64px)' }}>
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  );
};

export default DashboardLayout;
