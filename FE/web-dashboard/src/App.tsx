import React from 'react';
import { BrowserRouter, Routes, Route, Link } from 'react-router-dom';
import { Layout, Menu, Typography } from 'antd';

const { Header, Content, Footer } = Layout;
const { Title } = Typography;

const Home: React.FC = () => (
  <div style={{ padding: 24, minHeight: 380, background: '#fff', color: '#333' }}>
    <Title level={2}>Chào mừng tới SmartDine Management Dashboard</Title>
    <p>Hệ thống quản lý thời gian thực cho nhà hàng của bạn.</p>
  </div>
);

const App: React.FC = () => {
  return (
    <BrowserRouter>
      <Layout className="layout" style={{ minWidth: '100vw', minHeight: '100vh' }}>
        <Header style={{ display: 'flex', alignItems: 'center' }}>
          <div className="demo-logo" style={{ color: '#fff', marginRight: 24, fontWeight: 'bold' }}>
            SMARTDINE ADMIN
          </div>
          <Menu
            theme="dark"
            mode="horizontal"
            defaultSelectedKeys={['1']}
            items={[
              { key: '1', label: <Link to="/">Trang chủ</Link> },
              { key: '2', label: <Link to="/orders">Đơn hàng</Link> },
              { key: '3', label: <Link to="/menu">Thực đơn</Link> },
            ]}
          />
        </Header>
        <Content style={{ padding: '0 50px', marginTop: 24 }}>
          <Routes>
            <Route path="/" element={<Home />} />
            <Route path="/orders" element={<div style={{ color: '#fff' }}>Đơn hàng (Real-time Queue)</div>} />
            <Route path="/menu" element={<div style={{ color: '#fff' }}>Quản lý thực đơn (Menu Items)</div>} />
          </Routes>
        </Content>
        <Footer style={{ textAlign: 'center' }}>SmartDine ©2026 Created by DeepMind Antigravity</Footer>
      </Layout>
    </BrowserRouter>
  );
};

export default App;
