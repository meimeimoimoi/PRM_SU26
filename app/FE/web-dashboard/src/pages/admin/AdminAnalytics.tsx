import React, { useEffect, useState } from 'react';
import { DollarSign, ShoppingCart, ArrowUpRight } from 'lucide-react';
import {
  LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
} from 'recharts';
import { orderService, ChartPeriod } from '@/services/orderService';
import { apiClient } from '@/services/api/client';

function CustomTooltip({ active, payload, label }: any) {
  if (active && payload && payload.length) {
    return (
      <div className="bg-white rounded-lg border border-slate-200 p-3 shadow-lg">
        <p className="text-xs text-slate-400 font-body mb-1">{label}</p>
        {payload.map((p: any, i: number) => (
          <p key={i} className="text-sm font-semibold text-slate-800 font-heading" style={{ color: p.color }}>
            {p.name}: {Number(p.value).toLocaleString('vi-VN')}đ
          </p>
        ))}
      </div>
    );
  }
  return null;
}

function AdminAnalytics() {
  const [period, setPeriod] = useState<ChartPeriod>('month');
  const [orderData, setOrderData] = useState<{ label: string; value: number }[]>([]);
  const [revenueData, setRevenueData] = useState<{ label: string; value: number }[]>([]);
  const [loading, setLoading] = useState(true);
  const [summary, setSummary] = useState({ totalRevenue: 0, totalOrders: 0 });

  useEffect(() => {
    const fetch = async () => {
      setLoading(true);
      try {
        const [orders, revenue, revenueRes] = await Promise.all([
          orderService.getOrderChart(period),
          orderService.getRevenueChart(period),
          apiClient.get<any>('/payments/revenue-summary'),
        ]);
        setOrderData(orders);
        setRevenueData(revenue);
        const r = revenueRes.data.data || revenueRes.data || {};
        setSummary({ totalRevenue: r.monthRevenue || 0, totalOrders: orders.reduce((a, b) => a + b.value, 0) });
      } catch {
        setOrderData([]); setRevenueData([]);
      } finally { setLoading(false); }
    };
    fetch();
  }, [period]);

  return (
    <div className="space-y-5">
      <p className="text-sm text-slate-500 font-body">Deep insights into your restaurant performance.</p>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div className="bg-white rounded-xl border border-slate-200 p-4 shadow-sm">
          <div className="flex items-center gap-3 mb-2">
            <DollarSign size={16} className="text-accent" />
            <span className="text-[11px] text-slate-400 font-body uppercase tracking-wider">Month Revenue</span>
          </div>
          <p className="text-2xl font-bold text-slate-800 font-heading">{summary.totalRevenue.toLocaleString('vi-VN')}đ</p>
        </div>
        <div className="bg-white rounded-xl border border-slate-200 p-4 shadow-sm">
          <div className="flex items-center gap-3 mb-2">
            <ShoppingCart size={16} className="text-blue-500" />
            <span className="text-[11px] text-slate-400 font-body uppercase tracking-wider">Period Orders</span>
          </div>
          <p className="text-2xl font-bold text-slate-800 font-heading">{summary.totalOrders.toLocaleString('vi-VN')}</p>
        </div>
      </div>

      <div className="flex items-center gap-2">
        {(['day', 'week', 'month'] as ChartPeriod[]).map((p) => (
          <button key={p}
            onClick={() => setPeriod(p)}
            className={`px-3 py-1.5 rounded-md text-[11px] font-medium transition-all font-body cursor-pointer ${
              period === p
                ? 'bg-accentlight text-accent border border-accent/20'
                : 'text-slate-500 hover:text-slate-800 hover:bg-slate-100 border border-transparent'
            }`}>
            {p === 'day' ? 'Today' : p === 'week' ? '7 Days' : 'Month'}
          </button>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 md:gap-5">
        <div className="bg-white rounded-xl border border-slate-200 p-5 md:p-6 shadow-sm">
          <h3 className="text-sm font-semibold text-slate-800 font-heading mb-5">Order Chart</h3>
          {loading ? (
            <div className="flex items-center justify-center h-[260px]">
              <div className="w-6 h-6 border-2 border-accent/30 border-t-accent rounded-full animate-spin" />
            </div>
          ) : (
            <ResponsiveContainer width="100%" height={260}>
              <LineChart data={orderData}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#E2E8F0" />
                <XAxis dataKey="label" axisLine={false} tickLine={false} tick={{ fill: '#94A3B8', fontSize: 12 }} />
                <YAxis axisLine={false} tickLine={false} tick={{ fill: '#94A3B8', fontSize: 12 }}
                  tickFormatter={(v) => `${(v / 1000).toFixed(0)}k`} />
                <Tooltip content={<CustomTooltip />} />
                <Line type="monotone" dataKey="value" stroke="#2563EB" strokeWidth={2.5} dot={{ fill: '#2563EB', r: 4 }} name="Orders" />
              </LineChart>
            </ResponsiveContainer>
          )}
        </div>
        <div className="bg-white rounded-xl border border-slate-200 p-5 md:p-6 shadow-sm">
          <h3 className="text-sm font-semibold text-slate-800 font-heading mb-5">Revenue Chart</h3>
          {loading ? (
            <div className="flex items-center justify-center h-[260px]">
              <div className="w-6 h-6 border-2 border-accent/30 border-t-accent rounded-full animate-spin" />
            </div>
          ) : (
            <ResponsiveContainer width="100%" height={260}>
              <LineChart data={revenueData}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#E2E8F0" />
                <XAxis dataKey="label" axisLine={false} tickLine={false} tick={{ fill: '#94A3B8', fontSize: 12 }} />
                <YAxis axisLine={false} tickLine={false} tick={{ fill: '#94A3B8', fontSize: 12 }}
                  tickFormatter={(v) => `${(v / 1000).toFixed(0)}k`} />
                <Tooltip content={<CustomTooltip />} />
                <Line type="monotone" dataKey="value" stroke="#0EA5E9" strokeWidth={2.5} dot={{ fill: '#0EA5E9', r: 4 }} name="Revenue" />
              </LineChart>
            </ResponsiveContainer>
          )}
        </div>
      </div>
    </div>
  );
}

export default AdminAnalytics;