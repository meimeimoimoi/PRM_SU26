import React, { useEffect, useState } from 'react';
import { 
  DollarCircleOutlined, 
  ShoppingCartOutlined, 
  AppstoreOutlined 
} from '@ant-design/icons';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { Spin } from 'antd';
import { apiClient } from '../../services/api/client';
import '@/styles/DashboardPage.css';

interface DashboardStats {
  todayRevenue: number;
  activeOrders: number;
  availableTables: number;
  totalTables: number;
}

interface HourlySale {
  time: string;
  food: number;
  drink: number;
}

const DashboardPage: React.FC = () => {
  const [stats, setStats] = useState<DashboardStats>({
    todayRevenue: 0,
    activeOrders: 0,
    availableTables: 0,
    totalTables: 0,
  });
  const [salesData, setSalesData] = useState<HourlySale[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchDashboardData = async () => {
      setLoading(true);
      try {
        const [activeOrdersRes, todayOrdersRes, tablesRes] = await Promise.allSettled([
          apiClient.get<any>('/orders/active'),
          apiClient.get<any>('/orders/today'),
          apiClient.get<any>('/tables'),
        ]);

        const activeOrders = activeOrdersRes.status === 'fulfilled'
          ? (activeOrdersRes.value.data.data || activeOrdersRes.value.data || [])
          : [];
        const todayOrders = todayOrdersRes.status === 'fulfilled'
          ? (todayOrdersRes.value.data.data || todayOrdersRes.value.data || [])
          : [];
        const tables = tablesRes.status === 'fulfilled'
          ? (tablesRes.value.data.data || tablesRes.value.data || [])
          : [];

        const totalRevenue = todayOrders.reduce((sum: number, o: any) => sum + (o.finalAmount || 0), 0);
        const availableTables = Array.isArray(tables)
          ? tables.filter((t: any) => t.status === 'AVAILABLE').length
          : 0;

        setStats({
          todayRevenue: totalRevenue,
          activeOrders: Array.isArray(activeOrders) ? activeOrders.length : 0,
          availableTables,
          totalTables: Array.isArray(tables) ? tables.length : 0,
        });

        // Build hourly sales from today's orders
        const hourlyMap: { [key: string]: HourlySale } = {};
        const timeSlots = ['8am', '9am', '10am', '11am', '12pm', '1pm', '2pm', '3pm', '4pm', '5pm', '6pm', '7pm', '8pm', '9pm'];
        timeSlots.forEach(t => { hourlyMap[t] = { time: t, food: 0, drink: 0 }; });

        todayOrders.forEach((order: any) => {
          const hour = new Date(order.createdAt).getHours();
          const slot = hour <= 12 ? `${hour}am` : `${hour - 12}pm`;
          if (hourlyMap[slot]) {
            (order.items || []).forEach((item: any) => {
              hourlyMap[slot].food += item.unitPrice * item.quantity;
            });
          }
        });

        setSalesData(timeSlots.map(t => hourlyMap[t]).filter(s => s.food > 0));
      } catch (err) {
        console.error('Dashboard fetch error:', err);
      } finally {
        setLoading(false);
      }
    };

    fetchDashboardData();
  }, []);

  if (loading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '60vh' }}>
        <Spin size="large" />
      </div>
    );
  }

  const occupiedPct = stats.totalTables > 0
    ? Math.round(((stats.totalTables - stats.availableTables) / stats.totalTables) * 100)
    : 0;

  return (
    <div className="dashboard-overview">
      <div className="dashboard-header">
        <h2>Overview</h2>
        <p>Here's what's happening at your restaurant today.</p>
      </div>

      <div className="stat-cards-row">
        <div className="stat-card">
          <div className="stat-card-header">
            <span className="stat-card-label">TODAY'S REVENUE</span>
            <div className="stat-card-icon revenue">
              <DollarCircleOutlined />
            </div>
          </div>
          <div className="stat-card-value">{stats.todayRevenue.toLocaleString('vi-VN')}đ</div>
          <div className="stat-card-sub">
            <span>Tổng doanh thu hôm nay</span>
          </div>
        </div>

        <div className="stat-card">
          <div className="stat-card-header">
            <span className="stat-card-label">ACTIVE ORDERS</span>
            <div className="stat-card-icon orders">
              <ShoppingCartOutlined />
            </div>
          </div>
          <div className="stat-card-value">{stats.activeOrders}</div>
          <div className="stat-card-sub">
            <span>Đơn hàng đang xử lý</span>
          </div>
        </div>

        <div className="stat-card">
          <div className="stat-card-header">
            <span className="stat-card-label">AVAILABLE TABLES</span>
            <div className="stat-card-icon tables">
              <AppstoreOutlined />
            </div>
          </div>
          <div className="tables-fraction">
            <span className="big-num">{stats.availableTables}</span>
            <span className="divider">/</span>
            <span className="total-num">{stats.totalTables}</span>
          </div>
          <div className="tables-progress-row">
            <div className="tables-progress-bar">
              <div className="tables-progress-fill" style={{ width: `${occupiedPct}%` }} />
            </div>
            <span className="tables-progress-label">{occupiedPct}% Occupied</span>
          </div>
        </div>
      </div>

      <div className="dashboard-bottom-row">
        <div className="chart-card">
          <div className="chart-card-header">
            <h3>Revenue by Hour</h3>
          </div>
          {salesData.length > 0 ? (
            <ResponsiveContainer width="100%" height={280}>
              <BarChart data={salesData} barGap={4} barCategoryGap="25%">
                <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#f0f0f0" />
                <XAxis 
                  dataKey="time" 
                  axisLine={false} 
                  tickLine={false} 
                  tick={{ fill: '#718096', fontSize: 12 }} 
                />
                <YAxis 
                  axisLine={false} 
                  tickLine={false} 
                  tick={{ fill: '#718096', fontSize: 12 }}
                  tickFormatter={(value) => `${(value / 1000).toFixed(0)}k`}
                />
                <Tooltip 
                  formatter={(value: any) => [`${Number(value).toLocaleString('vi-VN')}đ`, '']}
                  contentStyle={{ borderRadius: 8, border: '1px solid #e8ecf1', boxShadow: '0 2px 8px rgba(0,0,0,0.08)' }}
                />
                <Bar dataKey="food" fill="#1890ff" radius={[4, 4, 0, 0]} barSize={24} name="Revenue" />
              </BarChart>
            </ResponsiveContainer>
          ) : (
            <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: 280, color: '#718096' }}>
              Chưa có dữ liệu doanh thu hôm nay
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default DashboardPage;
