import React, { useEffect, useState, useCallback } from 'react';
import { ChefHat, CheckCircle2, AlertCircle, Clock, Search } from 'lucide-react';
import { apiClient } from '@/services/api/client';

interface OrderItem {
  id: number; menuItemId: number; name: string; unitPrice: number; quantity: number; notes?: string; status: string;
}

interface OrderResponse {
  id: number; customerId?: number; customerName?: string; tableNumber: number;
  totalAmount: number; discountAmount: number; finalAmount: number;
  status: string; sessionStatus?: string; createdAt: string; items: OrderItem[];
}

const statusConfig: Record<string, { label: string; icon: React.ElementType; color: string }> = {
  PENDING: { label: 'Pending', icon: Clock, color: 'text-blue-600' },
  PROCESSING: { label: 'Processing', icon: ChefHat, color: 'text-amber-600' },
  COMPLETED: { label: 'Completed', icon: CheckCircle2, color: 'text-emerald-600' },
  CANCELLED: { label: 'Cancelled', icon: AlertCircle, color: 'text-red-500' },
  SERVED: { label: 'Served', icon: CheckCircle2, color: 'text-emerald-600' },
};

const defaultStatus = { label: 'Unknown', icon: AlertCircle, color: 'text-slate-400' };

function AdminOrders() {
  const [orders, setOrders] = useState<OrderResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');

  const fetchOrders = useCallback(async () => {
    setLoading(true);
    try {
      const res = await apiClient.get<any>('/orders/today');
      const data = res.data.data || res.data || [];
      setOrders(Array.isArray(data) ? data : []);
    } catch { setOrders([]); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { fetchOrders(); }, [fetchOrders]);

  const statusCounts = {
    pending: orders.filter((o) => o.status === 'PENDING').length,
    processing: orders.filter((o) => o.status === 'PROCESSING').length,
    completed: orders.filter((o) => o.status === 'COMPLETED' || o.status === 'SERVED').length,
  };

  const filtered = orders.filter((o) => {
    if (!search) return true;
    const q = search.toLowerCase();
    return `#ORD-${o.id}`.includes(q) || `T${o.tableNumber}`.includes(q) || o.status.toLowerCase().includes(q);
  });

  const formatTime = (iso: string) => {
    const diff = Date.now() - new Date(iso).getTime();
    const m = Math.floor(diff / 60000);
    if (m < 1) return 'Just now';
    if (m < 60) return `${m} min ago`;
    return `${Math.floor(m / 60)}h ago`;
  };

  return (
    <div className="space-y-5">
      <p className="text-sm text-slate-500 font-body">Track and manage all orders today.</p>

      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="bg-white rounded-xl border border-slate-200 p-4 flex items-center gap-4 shadow-sm">
          <div className="w-10 h-10 rounded-xl bg-blue-50 flex items-center justify-center text-blue-600"><Clock size={20} /></div>
          <div>
            <p className="text-2xl font-bold text-slate-800 font-heading">{statusCounts.pending}</p>
            <p className="text-xs text-slate-400 font-body">Pending</p>
          </div>
        </div>
        <div className="bg-white rounded-xl border border-slate-200 p-4 flex items-center gap-4 shadow-sm">
          <div className="w-10 h-10 rounded-xl bg-amber-50 flex items-center justify-center text-amber-600"><ChefHat size={20} /></div>
          <div>
            <p className="text-2xl font-bold text-slate-800 font-heading">{statusCounts.processing}</p>
            <p className="text-xs text-slate-400 font-body">Processing</p>
          </div>
        </div>
        <div className="bg-white rounded-xl border border-slate-200 p-4 flex items-center gap-4 shadow-sm">
          <div className="w-10 h-10 rounded-xl bg-emerald-50 flex items-center justify-center text-emerald-600"><CheckCircle2 size={20} /></div>
          <div>
            <p className="text-2xl font-bold text-slate-800 font-heading">{statusCounts.completed}</p>
            <p className="text-xs text-slate-400 font-body">Completed Today</p>
          </div>
        </div>
      </div>

      <div className="flex items-center gap-2 px-3 py-2 rounded-lg bg-white border border-slate-200 max-w-sm shadow-sm">
        <Search size={14} className="text-slate-400" />
        <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Search orders..."
          className="bg-transparent border-none outline-none text-sm text-slate-700 placeholder:text-slate-400 w-full font-body" />
      </div>

      <div className="bg-white rounded-2xl border border-slate-200 overflow-hidden shadow-sm">
        {loading ? (
          <div className="flex items-center justify-center h-40">
            <div className="w-6 h-6 border-2 border-accent/30 border-t-accent rounded-full animate-spin" />
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm font-body">
              <thead>
                <tr className="border-b border-slate-200 bg-slate-50">
                  <th className="text-left py-3.5 px-5 text-[11px] font-semibold text-slate-500 uppercase tracking-wider">Order</th>
                  <th className="text-left py-3.5 px-4 text-[11px] font-semibold text-slate-500 uppercase tracking-wider">Table</th>
                  <th className="text-left py-3.5 px-4 text-[11px] font-semibold text-slate-500 uppercase tracking-wider hidden sm:table-cell">Customer</th>
                  <th className="text-right py-3.5 px-4 text-[11px] font-semibold text-slate-500 uppercase tracking-wider">Total</th>
                  <th className="text-right py-3.5 px-4 text-[11px] font-semibold text-slate-500 uppercase tracking-wider hidden md:table-cell">Time</th>
                  <th className="text-right py-3.5 px-5 text-[11px] font-semibold text-slate-500 uppercase tracking-wider">Status</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map((order) => {
                  const s = statusConfig[order.status] || defaultStatus;
                  const Icon = s.icon;
                  return (
                    <tr key={order.id} className="border-b border-slate-100 hover:bg-slate-50 transition-colors cursor-pointer">
                      <td className="py-3.5 px-5">
                        <span className="text-slate-700 font-medium font-heading text-xs">#ORD-{order.id}</span>
                      </td>
                      <td className="py-3.5 px-4 text-slate-500 text-xs">T{order.tableNumber}</td>
                      <td className="py-3.5 px-4 text-slate-500 text-xs hidden sm:table-cell">{order.customerName || 'Walk-in'}</td>
                      <td className="py-3.5 px-4 text-right text-slate-800 font-heading text-xs font-semibold">{order.finalAmount.toLocaleString('vi-VN')}đ</td>
                      <td className="py-3.5 px-4 text-right text-slate-400 text-xs hidden md:table-cell">{formatTime(order.createdAt)}</td>
                      <td className="py-3.5 px-5 text-right">
                        <span className={`inline-flex items-center gap-1.5 text-[11px] font-medium ${s.color}`}>
                          <Icon size={12} /> {s.label}
                        </span>
                      </td>
                    </tr>
                  );
                })}
                {filtered.length === 0 && (
                  <tr><td colSpan={6} className="text-center py-10 text-slate-400 text-sm font-body">No orders found</td></tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}

export default AdminOrders;
