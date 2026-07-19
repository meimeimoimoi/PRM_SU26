import React, { useEffect, useState, useMemo, useCallback, useRef } from 'react';
import { useSelector } from 'react-redux';
import { selectCurrentUser } from '@/store/slices/authSlice';
import { useOrderHub } from '@/hooks/useOrderHub';
import {
  Clock, ChefHat, CheckCircle2, Printer, DollarSign,
  Search, Table, X, Play, RefreshCw, AlertCircle, Sparkles
} from 'lucide-react';
import { KitchenItem, KitchenItemStatus, KitchenOrderGroup } from '@/types/chef';
import { orderService, OrderResponse, PendingCashPayment } from '@/services/orderService';
import { tableService } from '@/services/tableService';
import { settingsService } from '@/services/settingsService';
import { Table as RestaurantTable } from '@/types/table';

const groupByOrder = (items: KitchenItem[]): KitchenOrderGroup[] => {
  const map = new Map<number, KitchenOrderGroup>();
  items.forEach((item) => {
    const existing = map.get(item.orderId);
    if (existing) {
      existing.items.push(item);
      existing.elapsedSeconds = Math.max(existing.elapsedSeconds, item.elapsedSeconds);
    } else {
      map.set(item.orderId, {
        orderId: item.orderId, tableNumber: item.tableNumber,
        status: item.status, orderedAt: item.orderedAt,
        elapsedSeconds: item.elapsedSeconds, items: [item],
      });
    }
  });
  return Array.from(map.values()).sort((a, b) => new Date(a.orderedAt).getTime() - new Date(b.orderedAt).getTime());
};

function formatTime(s: number) {
  return `${Math.floor(s / 60).toString().padStart(2, '0')}:${(s % 60).toString().padStart(2, '0')}`;
}

function timerColor(seconds: number, status: KitchenItemStatus) {
  if (status === 'DONE') return 'text-emerald-600';
  if (seconds >= 900) return 'text-red-500';
  if (seconds >= 480) return 'text-amber-500';
  return 'text-slate-400';
}

function AdminKitchen() {
  const user = useSelector(selectCurrentUser);
  const isManagerView = user?.role === 'MANAGER';

  const [kitchenItems, setKitchenItems] = useState<KitchenItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeOrders, setActiveOrders] = useState<OrderResponse[]>([]);
  const [activeTab, setActiveTab] = useState<'kitchen' | 'billing'>('kitchen');
  const [searchTable, setSearchTable] = useState('');
  const [pendingCashTables, setPendingCashTables] = useState<Set<number>>(new Set());
  const [pendingCashPayments, setPendingCashPayments] = useState<PendingCashPayment[]>([]);
  const [maintenanceTables, setMaintenanceTables] = useState<RestaurantTable[]>([]);
  const [taxRate, setTaxRate] = useState(8);
  const [serviceCharge, setServiceCharge] = useState(0);
  const [selectedTable, setSelectedTable] = useState<number | null>(null);
  const [showBill, setShowBill] = useState(false);

  const loadData = useCallback(async (spinner = true) => {
    if (spinner) setLoading(true);
    try {
      const [orders, items, tables, pendingCash, settings] = await Promise.all([
        orderService.getActiveOrders(),
        orderService.getKitchenItems(),
        tableService.getAllTables(),
        orderService.getPendingCashPayments().catch(() => [] as PendingCashPayment[]),
        settingsService.getSettings().catch(() => null),
      ]);
      setActiveOrders(orders);
      setKitchenItems(items);
      setMaintenanceTables(
        (tables || []).filter((t) => String(t.status).toUpperCase() === 'MAINTENANCE')
      );
      setPendingCashPayments(pendingCash);
      if (settings) { setTaxRate(Number(settings.taxRate) || 0); setServiceCharge(Number(settings.serviceChargeRate) || 0); }
      if (pendingCash.length > 0) {
        setPendingCashTables((prev) => {
          const next = new Set(prev);
          pendingCash.forEach((p) => next.add(p.tableNumber));
          return next;
        });
      }
    } catch {} finally { if (spinner) setLoading(false); }
  }, []);

  useEffect(() => { loadData(); }, [loadData]);

  useEffect(() => {
    const poll = setInterval(() => loadData(false), 5000);
    return () => clearInterval(poll);
  }, [loadData]);

  const reloadTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const scheduleReload = useCallback(() => {
    if (reloadTimer.current) clearTimeout(reloadTimer.current);
    reloadTimer.current = setTimeout(() => loadData(false), 200);
  }, [loadData]);

  const hubEvents = useMemo(() => ({
    ReceiveNewOrder: () => scheduleReload(),
    ReceiveOrderStatusUpdate: () => scheduleReload(),
    ReceiveCashPaymentPending: (data: any) => {
      const tn = data.tableNumber ?? data.TableNumber;
      if (tn != null) { setPendingCashTables((prev) => new Set(prev).add(tn)); setActiveTab('billing'); }
      scheduleReload();
    },
    ReceivePaymentSuccess: (data: any) => {
      const tn = data.tableNumber ?? data.TableNumber;
      if (tn != null) setPendingCashTables((prev) => { const n = new Set(prev); n.delete(tn); return n; });
      scheduleReload();
    },
  }), [scheduleReload]);
  useOrderHub(hubEvents, { joinKitchenGroup: true });

  useEffect(() => {
    const interval = setInterval(() => {
      setKitchenItems((prev) => prev.map((item) =>
        item.status === 'WAITING' || item.status === 'DOING'
          ? { ...item, elapsedSeconds: item.elapsedSeconds + 1 } : item
      ));
    }, 1000);
    return () => clearInterval(interval);
  }, []);

  const handleOrderAction = async (group: KitchenOrderGroup, newStatus: KitchenItemStatus) => {
    try {
      const ids = group.items.map((i) => i.id);
      await orderService.updateOrderItemsStatus(ids, newStatus);
      if (newStatus === 'SERVED') {
        setKitchenItems((prev) => prev.filter((i) => !ids.includes(i.id)));
      } else {
        setKitchenItems((prev) => prev.map((i) => ids.includes(i.id) ? { ...i, status: newStatus } : i));
      }
      scheduleReload();
    } catch {}
  };

  const handlePrintTicket = (group: KitchenOrderGroup) => {
    orderService.printKitchenOrderTicket({
      orderId: group.orderId, tableNumber: group.tableNumber,
      orderedAt: group.orderedAt,
      items: group.items.map((i) => ({ name: i.name, quantity: i.quantity, notes: i.notes })),
    });
  };

  const handlePrintBill = (tableNo: number) => {
    orderService.printCheckoutBill(tableNo, activeOrders);
  };

  const handleCheckout = async (tableNo: number) => {
    if (!confirm(`Xác nhận đã thu tiền Bàn ${tableNo}? Bàn sẽ chuyển sang cần dọn (MAINTENANCE).`)) return;
    try {
      await orderService.completePaymentByTable(tableNo);
      setActiveOrders((prev) => prev.filter((o) => o.tableNumber !== tableNo));
      setPendingCashTables((prev) => { const n = new Set(prev); n.delete(tableNo); return n; });
      setShowBill(false); setSelectedTable(null);
      await loadData(false);
    } catch {
      alert('Xác nhận thanh toán thất bại.');
    }
  };

  const handleMarkCleaned = async (t: RestaurantTable) => {
    if (!confirm(`Đã dọn xong Bàn ${t.tableNumber}? Bàn sẽ chuyển thành Trống (AVAILABLE).`)) return;
    try {
      await tableService.updateTableStatus(t.id, 'AVAILABLE');
      setMaintenanceTables((prev) => prev.filter((x) => x.id !== t.id));
    } catch {
      alert('Không cập nhật được trạng thái bàn. Thử lại.');
    }
  };

  const maintenanceSection = (
    <div className={`bg-white rounded-xl border p-5 shadow-sm ${
      maintenanceTables.length > 0 ? 'border-amber-300 ring-1 ring-amber-100' : 'border-slate-200'
    }`}>
      <div className="flex items-start justify-between gap-3 mb-4">
        <div>
          <h3 className="text-sm font-semibold text-slate-800 font-heading flex items-center gap-2">
            <Sparkles size={16} className="text-amber-500" />
            Bàn cần dọn ({maintenanceTables.length})
          </h3>
          <p className="text-xs text-slate-400 font-body mt-1">
            Sau thanh toán bàn chuyển MAINTENANCE. Dọn xong → đánh dấu Trống (AVAILABLE).
          </p>
        </div>
      </div>
      {maintenanceTables.length === 0 ? (
        <p className="text-sm text-slate-400 font-body py-2">Không có bàn đang chờ dọn.</p>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-3">
          {maintenanceTables.map((t) => (
            <div
              key={t.id}
              className="flex items-center justify-between gap-3 px-4 py-3 rounded-lg bg-amber-50/80 border border-amber-200"
            >
              <div>
                <div className="text-sm font-semibold text-slate-800 font-heading">
                  Bàn {t.tableNumber}
                </div>
                <div className="text-xs text-slate-500 font-body">
                  {t.locationName || '—'} · MAINTENANCE
                </div>
              </div>
              <button
                onClick={() => handleMarkCleaned(t)}
                className="shrink-0 px-3 py-1.5 rounded-lg text-[11px] font-medium bg-emerald-600 text-white hover:bg-emerald-700 transition-all cursor-pointer font-body"
              >
                Đã dọn xong
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );

  const pendingGroups = useMemo(() => groupByOrder(kitchenItems.filter((i) => i.status === 'WAITING')), [kitchenItems]);
  const doingGroups = useMemo(() => groupByOrder(kitchenItems.filter((i) => i.status === 'DOING')), [kitchenItems]);
  const doneGroups = useMemo(() => groupByOrder(kitchenItems.filter((i) => i.status === 'DONE')), [kitchenItems]);

  const activeTableList = useMemo(() => {
    const map: Record<number, { tableNumber: number; totalAmount: number; itemsCount: number; awaitingCash: boolean }> = {};
    activeOrders.forEach((o) => {
      const count = o.items.reduce((s, i) => s + i.quantity, 0);
      if (map[o.tableNumber]) { map[o.tableNumber].totalAmount += o.finalAmount; map[o.tableNumber].itemsCount += count; }
      else { map[o.tableNumber] = { tableNumber: o.tableNumber, totalAmount: o.finalAmount, itemsCount: count, awaitingCash: false }; }
    });
    // Card list phải khớp bill modal: cộng VAT + phí DV theo snapshot phiên (fallback settings)
    Object.values(map).forEach((t) => {
      const tableOrders = activeOrders.filter((o) => o.tableNumber === t.tableNumber);
      const snap = tableOrders.find((o) => o.taxRate != null || o.serviceChargeRate != null);
      const billTax = snap?.taxRate != null ? Number(snap.taxRate) : taxRate;
      const billService = snap?.serviceChargeRate != null ? Number(snap.serviceChargeRate) : serviceCharge;
      const net = t.totalAmount;
      const service = Math.round(net * billService / 100);
      const tax = Math.round(net * billTax / 100);
      t.totalAmount = net + service + tax;
    });
    pendingCashPayments.forEach((p) => {
      // amount từ payment PENDING đã gồm thuế/phí (khớp hóa đơn)
      if (map[p.tableNumber]) { map[p.tableNumber].awaitingCash = true; map[p.tableNumber].totalAmount = p.amount; }
      else { map[p.tableNumber] = { tableNumber: p.tableNumber, totalAmount: p.amount, itemsCount: 0, awaitingCash: true }; }
    });
    return Object.values(map).filter((t) => !searchTable || t.tableNumber.toString().includes(searchTable))
      .sort((a, b) => Number(b.awaitingCash) - Number(a.awaitingCash) || a.tableNumber - b.tableNumber);
  }, [activeOrders, searchTable, pendingCashPayments, taxRate, serviceCharge]);

  const billDetails = useMemo(() => {
    if (selectedTable === null) return null;
    const tblOrders = activeOrders.filter((o) => o.tableNumber === selectedTable);
    const itemMap: Record<string, { name: string; unitPrice: number; quantity: number; total: number }> = {};
    let subtotal = 0;
    tblOrders.forEach((o) => {
      o.items.forEach((item) => {
        const key = item.name + '_' + item.unitPrice;
        if (itemMap[key]) { itemMap[key].quantity += item.quantity; itemMap[key].total += item.unitPrice * item.quantity; }
        else { itemMap[key] = { name: item.name, unitPrice: item.unitPrice, quantity: item.quantity, total: item.unitPrice * item.quantity }; }
        subtotal += item.unitPrice * item.quantity;
      });
    });
    const discount = tblOrders.reduce((s, o) => s + o.discountAmount, 0);
    const net = subtotal - discount;
    // Ưu tiên snapshot thuế/phí của phiên; fallback settings live
    const snap = tblOrders.find((o) => o.taxRate != null || o.serviceChargeRate != null);
    const billTax = snap?.taxRate != null ? Number(snap.taxRate) : taxRate;
    const billService = snap?.serviceChargeRate != null ? Number(snap.serviceChargeRate) : serviceCharge;
    return {
      items: Object.values(itemMap), subtotal, discount, net,
      tax: Math.round(net * billTax / 100),
      service: Math.round(net * billService / 100),
      total: Math.round(net * (1 + billTax / 100 + billService / 100)),
      taxRate: billTax,
      serviceCharge: billService,
    };
  }, [selectedTable, activeOrders, taxRate, serviceCharge]);

  const KanbanColumn = ({ title, groups, status, color, icon: Icon, emptyText }: {
    title: string; groups: KitchenOrderGroup[]; status: KitchenItemStatus; color: string; icon: React.ElementType; emptyText: string;
  }) => (
    <div className="flex-1 min-w-[250px] max-w-[400px]">
      <div className={`flex items-center gap-2 mb-3 px-1`}>
        <Icon size={16} className={color} />
        <span className="text-xs font-semibold text-slate-500 uppercase tracking-wider font-body">{title}</span>
        <span className={`ml-auto text-xs font-bold font-heading ${color}`}>{groups.length}</span>
      </div>
      <div className="space-y-3 max-h-[60vh] overflow-y-auto scrollbar-thin pr-1">
        {groups.length === 0 ? (
          <div className="bg-white rounded-xl border border-slate-200 p-6 text-center shadow-sm">
            <p className="text-xs text-slate-400 font-body">{emptyText}</p>
          </div>
        ) : groups.map((g) => (
          <div key={g.orderId} className="bg-white rounded-xl border border-slate-200 p-4 shadow-sm hover:shadow-md transition-all"
            style={{ borderLeft: `3px solid ${status === 'WAITING' ? '#3B82F6' : status === 'DOING' ? '#F59E0B' : '#22C55E'}` }}>
            <div className="flex items-center justify-between mb-2">
              <span className="text-xs font-bold text-slate-700 font-heading">Table {g.tableNumber} · #{g.orderId}</span>
              <span className={`text-xs font-mono font-medium ${timerColor(g.elapsedSeconds, g.status)}`}>
                <Clock size={11} className="inline mr-1" />{formatTime(g.elapsedSeconds)}
              </span>
            </div>
            <div className="space-y-1 mb-3">
              {g.items.map((item) => (
                <div key={item.id} className="flex items-center justify-between text-xs">
                  <span className="text-slate-600 font-body">{item.name}</span>
                  <span className="text-slate-400 font-heading">x{item.quantity}{item.notes ? ` · ${item.notes}` : ''}</span>
                </div>
              ))}
            </div>
            <div className="flex items-center gap-2 pt-2 border-t border-slate-100">
              <button onClick={() => handlePrintTicket(g)}
                className="flex items-center gap-1 px-2.5 py-1.5 rounded-md text-[10px] font-medium text-slate-500 hover:text-slate-800 hover:bg-slate-100 transition-all cursor-pointer font-body">
                <Printer size={12} /> Print
              </button>
              {!isManagerView && (
                status === 'WAITING' ? (
                  <button onClick={() => handleOrderAction(g, 'DOING')}
                    className="flex items-center gap-1 px-2.5 py-1.5 rounded-md text-[10px] font-medium bg-blue-50 text-blue-600 hover:bg-blue-100 transition-all cursor-pointer font-body ml-auto">
                    <Play size={12} /> Cook ({g.items.length})
                  </button>
                ) : status === 'DOING' ? (
                  <button onClick={() => handleOrderAction(g, 'DONE')}
                    className="flex items-center gap-1 px-2.5 py-1.5 rounded-md text-[10px] font-medium bg-amber-50 text-amber-600 hover:bg-amber-100 transition-all cursor-pointer font-body ml-auto">
                    <CheckCircle2 size={12} /> Done ({g.items.length})
                  </button>
                ) : (
                  <button onClick={() => handleOrderAction(g, 'SERVED')}
                    className="flex items-center gap-1 px-2.5 py-1.5 rounded-md text-[10px] font-medium bg-emerald-50 text-emerald-600 hover:bg-emerald-100 transition-all cursor-pointer font-body ml-auto">
                    <CheckCircle2 size={12} /> Served ({g.items.length})
                  </button>
                )
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );

  return (
    <div className="space-y-5">
      <div className="flex items-center justify-between">
        <p className="text-sm text-slate-500 font-body">Kitchen operations & billing dashboard.</p>
        <button onClick={() => loadData()}
          className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-[11px] font-medium text-slate-500 hover:text-slate-800 hover:bg-slate-100 transition-all cursor-pointer font-body">
          <RefreshCw size={13} /> Refresh
        </button>
      </div>

      <div className="flex items-center gap-2 border-b border-slate-200 pb-3">
        <button onClick={() => setActiveTab('kitchen')}
          className={`px-4 py-2 rounded-lg text-sm font-medium transition-all cursor-pointer font-body ${
            activeTab === 'kitchen' ? 'bg-accentlight text-accent' : 'text-slate-500 hover:text-slate-800 hover:bg-slate-100'
          }`}>
          Kitchen ({pendingGroups.length + doingGroups.length + doneGroups.length})
        </button>
        <button onClick={() => setActiveTab('billing')}
          className={`px-4 py-2 rounded-lg text-sm font-medium transition-all cursor-pointer font-body ${
            activeTab === 'billing' ? 'bg-accentlight text-accent' : 'text-slate-500 hover:text-slate-800 hover:bg-slate-100'
          }`}>
          Billing ({activeTableList.length}
          {pendingCashTables.size > 0 ? ` · ${pendingCashTables.size} pending` : ''}
          {maintenanceTables.length > 0 ? ` · ${maintenanceTables.length} dọn` : ''})
        </button>
      </div>

      {maintenanceSection}

      {activeTab === 'kitchen' ? (
        <div className="flex flex-col lg:flex-row gap-5 overflow-x-auto">
          <KanbanColumn title="Waiting" groups={pendingGroups} status="WAITING" color="text-blue-500" icon={Clock}
            emptyText="No pending orders." />
          <KanbanColumn title="Cooking" groups={doingGroups} status="DOING" color="text-amber-500" icon={ChefHat}
            emptyText="Kitchen is idle." />
          <KanbanColumn title="Ready" groups={doneGroups} status="DONE" color="text-emerald-500" icon={CheckCircle2}
            emptyText="No items ready." />
        </div>
      ) : (
        <div className="space-y-5">
          <div className="flex items-center gap-2 px-3 py-2 rounded-lg bg-white border border-slate-200 max-w-xs shadow-sm">
            <Search size={14} className="text-slate-400" />
            <input value={searchTable} onChange={(e) => setSearchTable(e.target.value)} placeholder="Search table..."
              className="bg-transparent border-none outline-none text-sm text-slate-700 placeholder:text-slate-400 w-full font-body" />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4">
            {activeTableList.map((t) => (
              <div key={t.tableNumber} className={`bg-white rounded-xl border p-5 shadow-sm hover:shadow-md transition-all ${t.awaitingCash ? 'border-accent/40 ring-1 ring-accent/20' : 'border-slate-200'}`}>
                <div className="flex items-center justify-between mb-3">
                  <div className="flex items-center gap-2">
                    <Table size={16} className="text-slate-400" />
                    <span className="text-base font-bold text-slate-800 font-heading">Table {t.tableNumber}</span>
                  </div>
                  {t.awaitingCash && <span className="text-[10px] font-medium text-amber-600 bg-amber-50 px-2 py-0.5 rounded font-body">Cash pending</span>}
                </div>
                <div className="text-lg font-bold text-slate-800 font-heading mb-1">{t.totalAmount.toLocaleString('vi-VN')}đ</div>
                <div className="text-xs text-slate-400 font-body mb-4">{t.itemsCount} items</div>
                <div className="flex items-center gap-2">
                  <button onClick={() => { setSelectedTable(t.tableNumber); setShowBill(true); }}
                    className="flex-1 px-3 py-1.5 rounded-lg text-[11px] font-medium text-slate-600 hover:text-slate-800 hover:bg-slate-100 transition-all cursor-pointer font-body border border-slate-200">
                    Details
                  </button>
                  {t.awaitingCash && !isManagerView ? (
                    <button onClick={() => handleCheckout(t.tableNumber)}
                      className="flex-1 px-3 py-1.5 rounded-lg text-[11px] font-medium bg-accent text-white hover:bg-accent/80 transition-all cursor-pointer font-body">
                      Confirm Cash
                    </button>
                  ) : (
                    <button onClick={() => handlePrintBill(t.tableNumber)}
                      className="px-3 py-1.5 rounded-lg text-[11px] font-medium text-slate-600 hover:text-slate-800 hover:bg-slate-100 transition-all cursor-pointer font-body border border-slate-200">
                      <Printer size={12} />
                    </button>
                  )}
                </div>
              </div>
            ))}
            {activeTableList.length === 0 && (
              <div className="col-span-full text-center py-10 text-slate-400 text-sm font-body">No active tables.</div>
            )}
          </div>
        </div>
      )}

      {showBill && billDetails && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/30" onClick={() => { setShowBill(false); setSelectedTable(null); }}>
          <div className="bg-white rounded-2xl border border-slate-200 w-full max-w-lg p-6 animate-slide-up shadow-xl max-h-[90vh] overflow-y-auto" onClick={(e) => e.stopPropagation()}>
            <div className="flex items-center justify-between mb-5">
              <h3 className="text-base font-semibold text-slate-800 font-heading">Bill — Table {selectedTable}</h3>
              <button onClick={() => { setShowBill(false); setSelectedTable(null); }} className="p-1 text-slate-400 hover:text-slate-600 transition-colors cursor-pointer">
                <X size={18} />
              </button>
            </div>
            <div className="text-xs text-slate-400 font-body mb-4">
              {new Date().toLocaleString('vi-VN')} · {user?.fullName || 'Staff'}
            </div>
            <div className="space-y-3 mb-4">
              {billDetails.items.map((item, i) => (
                <div key={i} className="flex items-center justify-between text-sm border-b border-slate-100 pb-2">
                  <div>
                    <span className="text-slate-700 font-body">{item.name}</span>
                    <span className="text-slate-400 ml-2 font-heading">x{item.quantity}</span>
                  </div>
                  <span className="text-slate-800 font-heading">{item.total.toLocaleString('vi-VN')}đ</span>
                </div>
              ))}
            </div>
            <div className="space-y-1.5 text-sm border-t border-slate-200 pt-3">
              <div className="flex justify-between text-slate-500 font-body"><span>Subtotal</span><span className="font-heading">{billDetails.subtotal.toLocaleString('vi-VN')}đ</span></div>
              {billDetails.discount > 0 && <div className="flex justify-between text-red-500 font-body"><span>Discount</span><span className="font-heading">-{billDetails.discount.toLocaleString('vi-VN')}đ</span></div>}
              {billDetails.service > 0 && <div className="flex justify-between text-slate-500 font-body"><span>Service ({billDetails.serviceCharge}%)</span><span className="font-heading">{billDetails.service.toLocaleString('vi-VN')}đ</span></div>}
              <div className="flex justify-between text-slate-500 font-body"><span>VAT ({billDetails.taxRate}%)</span><span className="font-heading">{billDetails.tax.toLocaleString('vi-VN')}đ</span></div>
              <div className="flex justify-between text-slate-800 font-bold font-heading text-base pt-2 border-t border-slate-200">
                <span>TOTAL</span><span>{billDetails.total.toLocaleString('vi-VN')}đ</span>
              </div>
            </div>
            <div className="flex justify-end gap-3 mt-5 pt-3 border-t border-slate-200">
              <button onClick={() => selectedTable && handlePrintBill(selectedTable)}
                className="flex items-center gap-1.5 px-4 py-2 rounded-lg text-sm font-medium text-slate-600 hover:text-slate-800 hover:bg-slate-100 transition-all cursor-pointer font-body">
                <Printer size={15} /> Print
              </button>
              {/* Chỉ hiện khi khách đã tạo yêu cầu thanh toán tiền mặt (CHECKOUT / Cash pending) */}
              {!isManagerView && selectedTable != null && activeTableList.some((t) => t.tableNumber === selectedTable && t.awaitingCash) && (
                <button onClick={() => selectedTable && handleCheckout(selectedTable)}
                  className="flex items-center gap-1.5 px-4 py-2 rounded-lg bg-accent text-white text-sm font-medium hover:bg-accent/80 transition-all cursor-pointer font-body">
                  <DollarSign size={15} /> Confirm Payment
                </button>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default AdminKitchen;
