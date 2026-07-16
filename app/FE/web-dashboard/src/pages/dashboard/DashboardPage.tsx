import React, { useEffect, useState } from 'react';
import {
  DollarCircleOutlined,
  ShoppingCartOutlined,
  AppstoreOutlined,
  DownloadOutlined
} from '@ant-design/icons';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { Spin, Segmented, Button, message } from 'antd';
import { apiClient } from '../../services/api/client';
import { orderService, ChartPoint, ChartPeriod } from '@/services/orderService';
import { downloadCsv } from '@/utils/csvExport';
import '@/styles/DashboardPage.css';

interface DashboardStats {
  todayRevenue: number;
  monthRevenue: number;
  activeOrders: number;
  availableTables: number;
  totalTables: number;
}

const PERIOD_OPTIONS: { label: string; value: ChartPeriod }[] = [
  { label: 'Hôm nay', value: 'day' },
  { label: '7 ngày', value: 'week' },
  { label: 'Tháng này', value: 'month' }
];

const DashboardPage: React.FC = () => {
  const [stats, setStats] = useState<DashboardStats>({
    todayRevenue: 0,
    monthRevenue: 0,
    activeOrders: 0,
    availableTables: 0,
    totalTables: 0,
  });
  const [loading, setLoading] = useState(true);

  const [period, setPeriod] = useState<ChartPeriod>('day');
  const [orderChartData, setOrderChartData] = useState<ChartPoint[]>([]);
  const [revenueChartData, setRevenueChartData] = useState<ChartPoint[]>([]);
  const [chartLoading, setChartLoading] = useState(true);

  // Stat cards — chỉ cần tải 1 lần, không phụ thuộc period của 2 chart bên dưới.
  useEffect(() => {
    const fetchStats = async () => {
      setLoading(true);
      try {
        const [activeOrdersRes, tablesRes, revenueRes] = await Promise.allSettled([
          apiClient.get<any>('/orders/active'),
          apiClient.get<any>('/tables'),
          // Doanh thu thực thu — chỉ tính payment SUCCESS (xem PaymentService.GetRevenueSummaryAsync),
          // không lấy từ finalAmount của order vì order có thể chưa thanh toán/bị hủy.
          apiClient.get<any>('/payments/revenue-summary'),
        ]);

        const activeOrders = activeOrdersRes.status === 'fulfilled'
          ? (activeOrdersRes.value.data.data || activeOrdersRes.value.data || [])
          : [];
        const tables = tablesRes.status === 'fulfilled'
          ? (tablesRes.value.data.data || tablesRes.value.data || [])
          : [];
        const revenue = revenueRes.status === 'fulfilled'
          ? (revenueRes.value.data.data || revenueRes.value.data || {})
          : {};

        const availableTables = Array.isArray(tables)
          ? tables.filter((t: any) => t.status === 'AVAILABLE').length
          : 0;

        setStats({
          todayRevenue: revenue.todayRevenue || 0,
          monthRevenue: revenue.monthRevenue || 0,
          activeOrders: Array.isArray(activeOrders) ? activeOrders.length : 0,
          availableTables,
          totalTables: Array.isArray(tables) ? tables.length : 0,
        });
      } catch (err) {
        console.error('Dashboard fetch error:', err);
      } finally {
        setLoading(false);
      }
    };

    fetchStats();
  }, []);

  // 2 chart theo giờ/tuần/tháng — tải lại mỗi khi đổi period.
  useEffect(() => {
    const fetchCharts = async () => {
      setChartLoading(true);
      try {
        const [orderChart, revenueChart] = await Promise.all([
          orderService.getOrderChart(period),
          orderService.getRevenueChart(period)
        ]);
        setOrderChartData(orderChart);
        setRevenueChartData(revenueChart);
      } catch (err) {
        console.error('Chart fetch error:', err);
        setOrderChartData([]);
        setRevenueChartData([]);
      } finally {
        setChartLoading(false);
      }
    };

    fetchCharts();
  }, [period]);

  const periodLabel = PERIOD_OPTIONS.find((p) => p.value === period)?.label || period;

  // Gộp chỉ số tổng quan + 2 chart hiện đang xem thành 1 file CSV nhiều phần.
  const handleExportReport = () => {
    if (chartLoading) {
      message.warning('Vui lòng chờ biểu đồ tải xong trước khi xuất báo cáo.');
      return;
    }

    const rows: (string | number)[][] = [
      [`Báo cáo tổng quan Dashboard - ${new Date().toLocaleString('vi-VN')}`],
      [],
      ['CHỈ SỐ TỔNG QUAN'],
      ['Chỉ số', 'Giá trị'],
      ['Doanh thu hôm nay (VND)', stats.todayRevenue],
      ['Doanh thu tháng này (VND)', stats.monthRevenue],
      ['Đơn hàng đang xử lý', stats.activeOrders],
      ['Bàn trống', stats.availableTables],
      ['Tổng số bàn', stats.totalTables],
      [],
      [`DOANH SỐ ĐƠN HÀNG (theo ${periodLabel})`],
      ['Thời điểm', 'Giá trị (VND)'],
      ...orderChartData.map((d) => [d.label, d.value]),
      [],
      [`DOANH THU THỰC NHẬN (theo ${periodLabel})`],
      ['Thời điểm', 'Giá trị (VND)'],
      ...revenueChartData.map((d) => [d.label, d.value]),
    ];

    const dateStr = new Date().toISOString().slice(0, 10);
    downloadCsv(`dashboard_report_${dateStr}.csv`, rows);
    message.success('Đã xuất báo cáo dashboard ra file CSV.');
  };

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

  const renderChart = (data: ChartPoint[], color: string, emptyText: string) => {
    if (chartLoading) {
      return (
        <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: 280 }}>
          <Spin />
        </div>
      );
    }
    const hasData = data.some((d) => d.value > 0);
    if (!hasData) {
      return (
        <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: 280, color: '#718096' }}>
          {emptyText}
        </div>
      );
    }
    return (
      <ResponsiveContainer width="100%" height={280}>
        <BarChart data={data} barGap={4} barCategoryGap="25%">
          <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#f0f0f0" />
          <XAxis
            dataKey="label"
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
          <Bar dataKey="value" fill={color} radius={[4, 4, 0, 0]} barSize={period === 'day' ? 12 : 24} name="Doanh thu" />
        </BarChart>
      </ResponsiveContainer>
    );
  };

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
            <span className="stat-card-label">MONTH'S REVENUE</span>
            <div className="stat-card-icon revenue">
              <DollarCircleOutlined />
            </div>
          </div>
          <div className="stat-card-value">{stats.monthRevenue.toLocaleString('vi-VN')}đ</div>
          <div className="stat-card-sub">
            <span>Tổng doanh thu tháng này</span>
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

      <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 12, margin: '20px 0 4px' }}>
        <Button icon={<DownloadOutlined />} onClick={handleExportReport}>
          Xuất báo cáo
        </Button>
        <Segmented options={PERIOD_OPTIONS} value={period} onChange={(val) => setPeriod(val as ChartPeriod)} />
      </div>

      <div className="dashboard-charts-row">
        <div className="chart-card">
          <div className="chart-card-header">
            <h3>Doanh số đơn hàng</h3>
          </div>
          {renderChart(orderChartData, '#1890ff', 'Chưa có đơn hàng nào trong khoảng thời gian này')}
        </div>

        <div className="chart-card">
          <div className="chart-card-header">
            <h3>Doanh thu thực nhận</h3>
          </div>
          {renderChart(revenueChartData, '#52c41a', 'Chưa có thanh toán thành công nào trong khoảng thời gian này')}
        </div>
      </div>
    </div>
  );
};

export default DashboardPage;
