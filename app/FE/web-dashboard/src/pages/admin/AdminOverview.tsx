import React, { useEffect, useState } from 'react';
import {
  DollarSign,
  ShoppingCart,
  Table,
  ArrowUpRight,
  Download,
} from 'lucide-react';
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
} from 'recharts';
import { apiClient } from '@/services/api/client';
import { orderService, ChartPoint, ChartPeriod } from '@/services/orderService';
import { downloadCsv } from '@/utils/csvExport';

interface DashboardStats {
  todayRevenue: number;
  monthRevenue: number;
  activeOrders: number;
  availableTables: number;
  totalTables: number;
}

const PERIOD_OPTIONS: { label: string; value: ChartPeriod }[] = [
  { label: 'Today', value: 'day' },
  { label: '7 Days', value: 'week' },
  { label: 'Month', value: 'month' },
];

function StatCard({ icon: Icon, label, value, sub }: {
  icon: React.ElementType; label: string; value: string; sub: string;
}) {
  return (
    <div className="bg-white rounded-2xl border border-slate-200 p-5 md:p-6 animate-slide-up group cursor-default shadow-sm hover:shadow-md hover:border-slate-300 transition-all">
      <div className="flex items-start justify-between mb-3">
        <span className="text-[10px] md:text-[11px] font-semibold tracking-[0.5px] uppercase text-slate-400 font-body">
          {label}
        </span>
        <div className="w-9 h-9 md:w-10 md:h-10 rounded-xl bg-accentlight flex items-center justify-center text-accent transition-all duration-300">
          <Icon size={18} />
        </div>
      </div>
      <div className="stat-value text-slate-800 mb-1">{value}</div>
      <div className="text-xs text-slate-400 font-body">{sub}</div>
    </div>
  );
}

function AdminOverview() {
  const [stats, setStats] = useState<DashboardStats>({
    todayRevenue: 0, monthRevenue: 0, activeOrders: 0, availableTables: 0, totalTables: 0,
  });
  const [loading, setLoading] = useState(true);
  const [period, setPeriod] = useState<ChartPeriod>('day');
  const [orderChartData, setOrderChartData] = useState<ChartPoint[]>([]);
  const [revenueChartData, setRevenueChartData] = useState<ChartPoint[]>([]);
  const [chartLoading, setChartLoading] = useState(true);

  useEffect(() => {
    const fetchStats = async () => {
      setLoading(true);
      try {
        const [activeOrdersRes, tablesRes, revenueRes] = await Promise.allSettled([
          apiClient.get<any>('/orders/active'),
          apiClient.get<any>('/tables'),
          apiClient.get<any>('/payments/revenue-summary'),
        ]);

        const activeOrders = activeOrdersRes.status === 'fulfilled'
          ? (activeOrdersRes.value.data.data || activeOrdersRes.value.data || []) : [];
        const tables = tablesRes.status === 'fulfilled'
          ? (tablesRes.value.data.data || tablesRes.value.data || []) : [];
        const revenue = revenueRes.status === 'fulfilled'
          ? (revenueRes.value.data.data || revenueRes.value.data || {}) : {};

        const availableTables = Array.isArray(tables)
          ? tables.filter((t: any) => t.status === 'AVAILABLE').length : 0;

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

  useEffect(() => {
    const fetchCharts = async () => {
      setChartLoading(true);
      try {
        const [orderChart, revenueChart] = await Promise.all([
          orderService.getOrderChart(period),
          orderService.getRevenueChart(period),
        ]);
        setOrderChartData(orderChart);
        setRevenueChartData(revenueChart);
      } catch {
        setOrderChartData([]);
        setRevenueChartData([]);
      } finally {
        setChartLoading(false);
      }
    };
    fetchCharts();
  }, [period]);

  const periodLabel = PERIOD_OPTIONS.find((p) => p.value === period)?.label || period;

  const handleExportReport = () => {
    if (chartLoading) return;
    const rows: (string | number)[][] = [
      [`SmartDine Dashboard Report - ${new Date().toLocaleString('vi-VN')}`],
      [],
      ['METRICS'],
      ['Metric', 'Value'],
      ['Today Revenue (VND)', stats.todayRevenue],
      ['Month Revenue (VND)', stats.monthRevenue],
      ['Active Orders', stats.activeOrders],
      ['Available Tables', stats.availableTables],
      ['Total Tables', stats.totalTables],
      [],
      [`ORDER CHART (${periodLabel})`],
      ['Time', 'Value'],
      ...orderChartData.map((d) => [d.label, d.value]),
      [],
      [`REVENUE CHART (${periodLabel})`],
      ['Time', 'Value'],
      ...revenueChartData.map((d) => [d.label, d.value]),
    ];
    const dateStr = new Date().toISOString().slice(0, 10);
    downloadCsv(`dashboard_report_${dateStr}.csv`, rows);
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-[40vh]">
        <div className="w-8 h-8 border-2 border-accent/30 border-t-accent rounded-full animate-spin" />
      </div>
    );
  }

  const occupiedPct = stats.totalTables > 0
    ? Math.round(((stats.totalTables - stats.availableTables) / stats.totalTables) * 100) : 0;

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 md:gap-5">
        <StatCard icon={DollarSign} label="Today's Revenue"
          value={`${stats.todayRevenue.toLocaleString('vi-VN')}đ`}
          sub="Total revenue today" />
        <StatCard icon={ShoppingCart} label="Active Orders"
          value={`${stats.activeOrders}`}
          sub="Orders currently in progress" />
        <StatCard icon={Table} label="Tables"
          value={`${stats.availableTables}/${stats.totalTables}`}
          sub={`${occupiedPct}% occupied`} />
        <StatCard icon={ArrowUpRight} label="Month's Revenue"
          value={`${stats.monthRevenue.toLocaleString('vi-VN')}đ`}
          sub="Total revenue this month" />
      </div>

      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          {PERIOD_OPTIONS.map((opt) => (
            <button key={opt.value}
              onClick={() => setPeriod(opt.value)}
              className={`px-3 py-1.5 rounded-md text-[11px] font-medium transition-all font-body cursor-pointer ${
                period === opt.value
                  ? 'bg-accentlight text-accent border border-accent/20'
                  : 'text-slate-500 hover:text-slate-800 hover:bg-slate-100 border border-transparent'
              }`}>
              {opt.label}
            </button>
          ))}
        </div>
        <button onClick={handleExportReport}
          className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-[11px] font-medium text-slate-500 hover:text-slate-800 hover:bg-slate-100 transition-all cursor-pointer font-body">
          <Download size={14} />
          Export CSV
        </button>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-7 gap-4 md:gap-5">
        <div className="lg:col-span-4 bg-white rounded-2xl border border-slate-200 p-5 md:p-6 shadow-sm">
          <h3 className="text-sm font-semibold text-slate-800 font-heading mb-5">Order Chart</h3>
          <div className="h-[260px]">
            {chartLoading ? (
              <div className="flex items-center justify-center h-full">
                <div className="w-6 h-6 border-2 border-accent/30 border-t-accent rounded-full animate-spin" />
              </div>
            ) : orderChartData.some((d) => d.value > 0) ? (
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={orderChartData} barGap={4} barCategoryGap="25%">
                  <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#E2E8F0" />
                  <XAxis dataKey="label" axisLine={false} tickLine={false} tick={{ fill: '#94A3B8', fontSize: 12 }} />
                  <YAxis axisLine={false} tickLine={false} tick={{ fill: '#94A3B8', fontSize: 12 }}
                    tickFormatter={(v) => `${(v / 1000).toFixed(0)}k`} />
                  <Tooltip contentStyle={{ background: 'white', border: '1px solid #E2E8F0', borderRadius: 8, fontSize: 13 }} />
                  <Bar dataKey="value" fill="#2563EB" radius={[4, 4, 0, 0]} barSize={period === 'day' ? 12 : 24} name="Orders" />
                </BarChart>
              </ResponsiveContainer>
            ) : (
              <div className="flex items-center justify-center h-full text-slate-400 text-sm font-body">No orders in this period</div>
            )}
          </div>
        </div>
        <div className="lg:col-span-3 bg-white rounded-2xl border border-slate-200 p-5 md:p-6 shadow-sm">
          <h3 className="text-sm font-semibold text-slate-800 font-heading mb-5">Revenue Chart</h3>
          <div className="h-[260px]">
            {chartLoading ? (
              <div className="flex items-center justify-center h-full">
                <div className="w-6 h-6 border-2 border-accent/30 border-t-accent rounded-full animate-spin" />
              </div>
            ) : revenueChartData.some((d) => d.value > 0) ? (
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={revenueChartData} barGap={4} barCategoryGap="25%">
                  <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#E2E8F0" />
                  <XAxis dataKey="label" axisLine={false} tickLine={false} tick={{ fill: '#94A3B8', fontSize: 12 }} />
                  <YAxis axisLine={false} tickLine={false} tick={{ fill: '#94A3B8', fontSize: 12 }}
                    tickFormatter={(v) => `${(v / 1000).toFixed(0)}k`} />
                  <Tooltip contentStyle={{ background: 'white', border: '1px solid #E2E8F0', borderRadius: 8, fontSize: 13 }} />
                  <Bar dataKey="value" fill="#0EA5E9" radius={[4, 4, 0, 0]} barSize={period === 'day' ? 12 : 24} name="Revenue" />
                </BarChart>
              </ResponsiveContainer>
            ) : (
              <div className="flex items-center justify-center h-full text-slate-400 text-sm font-body">No payments in this period</div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

export default AdminOverview;
