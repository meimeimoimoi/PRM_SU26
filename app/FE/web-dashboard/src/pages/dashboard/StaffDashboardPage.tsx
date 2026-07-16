import React, { useEffect, useState, useMemo, useCallback, useRef } from 'react';
import { useSelector } from 'react-redux';
import { selectCurrentUser } from '@/store/slices/authSlice';
import { useOrderHub } from '@/hooks/useOrderHub';
import { 
  Button, 
  message, 
  Spin, 
  Tabs, 
  Card, 
  Row, 
  Col, 
  Modal, 
  List, 
  Input, 
  Empty 
} from 'antd';
import { 
  PlayCircleOutlined, 
  CheckCircleOutlined, 
  ClockCircleOutlined, 
  FireOutlined, 
  CoffeeOutlined, 
  DoubleRightOutlined,
  CheckOutlined,
  PrinterOutlined,
  DollarCircleOutlined,
  SearchOutlined,
  TableOutlined
} from '@ant-design/icons';
import { KitchenItem, KitchenItemStatus, KitchenOrderGroup } from '@/types/chef';
import { orderService, OrderResponse, PendingCashPayment } from '@/services/orderService';
import { tableService } from '@/services/tableService';
import { settingsService } from '@/services/settingsService';
import { Table as RestaurantTable } from '@/types/table';
import '@/styles/StaffDashboardPage.css';

const { TabPane } = Tabs;

/** Gộp các món cùng orderId trong cùng cột trạng thái thành 1 thẻ đơn. */
const groupKitchenItemsByOrder = (items: KitchenItem[]): KitchenOrderGroup[] => {
  const map = new Map<number, KitchenOrderGroup>();
  items.forEach((item) => {
    const existing = map.get(item.orderId);
    if (existing) {
      existing.items.push(item);
      existing.elapsedSeconds = Math.max(existing.elapsedSeconds, item.elapsedSeconds);
    } else {
      map.set(item.orderId, {
        orderId: item.orderId,
        tableNumber: item.tableNumber,
        status: item.status,
        orderedAt: item.orderedAt,
        elapsedSeconds: item.elapsedSeconds,
        items: [item],
      });
    }
  });
  return Array.from(map.values()).sort(
    (a, b) => new Date(a.orderedAt).getTime() - new Date(b.orderedAt).getTime()
  );
};

const StaffDashboardPage: React.FC = () => {
  const user = useSelector(selectCurrentUser);
  // Manager chỉ được xem Kitchen & Billing ở mức tối thiểu, không thao tác — tránh
  // manager vô tình can thiệp vào ca làm việc thực tế của staff/bếp.
  const isManagerViewOnly = user?.role === 'MANAGER';

  // Kitchen Queue State
  const [kitchenItems, setKitchenItems] = useState<KitchenItem[]>([]);
  const [loading, setLoading] = useState<boolean>(false);


  // Billing State
  const [activeOrders, setActiveOrders] = useState<OrderResponse[]>([]);
  const [selectedTable, setSelectedTable] = useState<number | null>(null);
  const [billingModalVisible, setBillingModalVisible] = useState<boolean>(false);
  const [searchTableText, setSearchTableText] = useState<string>('');
  const [activeTab, setActiveTab] = useState<string>('queue');
  /** Bàn vừa báo thanh toán tiền mặt — highlight trên tab hóa đơn */
  const [pendingCashTables, setPendingCashTables] = useState<Set<number>>(new Set());
  const [pendingCashPayments, setPendingCashPayments] = useState<PendingCashPayment[]>([]);
  const [maintenanceTables, setMaintenanceTables] = useState<RestaurantTable[]>([]);
  /** % từ RestaurantSettings — Manager chỉnh ở Settings */
  const [taxRatePercent, setTaxRatePercent] = useState<number>(8);
  const [serviceChargePercent, setServiceChargePercent] = useState<number>(0);

  // Load active orders and kitchen items
  const loadData = useCallback(async (showSpinner = true) => {
    if (showSpinner) setLoading(true);
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
      setMaintenanceTables((tables || []).filter((t) => t.status === 'MAINTENANCE'));
      setPendingCashPayments(pendingCash);
      if (settings) {
        setTaxRatePercent(Number(settings.taxRate) || 0);
        setServiceChargePercent(Number(settings.serviceChargeRate) || 0);
      }
      if (pendingCash.length > 0) {
        setPendingCashTables((prev) => {
          const next = new Set(prev);
          pendingCash.forEach((p) => next.add(p.tableNumber));
          return next;
        });
      }
    } catch (err) {
      if (showSpinner) message.error('Không thể tải danh sách dữ liệu.');
    } finally {
      if (showSpinner) setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadData(true);
  }, [loadData]);

  // Polling nhẹ mỗi 5s — đảm bảo realtime kể cả khi SignalR gián đoạn (không hiện spinner)
  useEffect(() => {
    const poll = setInterval(() => {
      loadData(false);
    }, 5000);
    return () => clearInterval(poll);
  }, [loadData]);

  // Nhóm nhiều sự kiện realtime đến gần nhau (vd. một đơn nhiều món) thành 1 lần reload.
  const reloadTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const scheduleReload = useCallback(() => {
    if (reloadTimerRef.current) clearTimeout(reloadTimerRef.current);
    reloadTimerRef.current = setTimeout(() => loadData(false), 200);
  }, [loadData]);

  // Tham gia KitchenGroup qua SignalR để nhận đơn mới / cập nhật trạng thái /
  // yêu cầu thu tiền mặt theo thời gian thực.
  const kitchenHubEvents = useMemo(
    () => ({
      ReceiveNewOrder: () => scheduleReload(),
      ReceiveOrderStatusUpdate: () => scheduleReload(),
      ReceiveCashPaymentPending: (data: { tableNumber?: number; TableNumber?: number; invoiceId?: string; InvoiceId?: string; amount?: number; Amount?: number }) => {
        const tableNo = data.tableNumber ?? data.TableNumber;
        if (tableNo != null) {
          setPendingCashTables((prev) => new Set(prev).add(tableNo));
          setActiveTab('billing');
          message.info(`Bàn ${tableNo} yêu cầu thanh toán tiền mặt — vui lòng xác nhận thu tiền.`);
        }
        scheduleReload();
      },
      ReceivePaymentSuccess: (data: { tableNumber?: number; TableNumber?: number }) => {
        const tableNo = data.tableNumber ?? data.TableNumber;
        if (tableNo != null) {
          setPendingCashTables((prev) => {
            const next = new Set(prev);
            next.delete(tableNo);
            return next;
          });
        }
        scheduleReload();
      },
    }),
    [scheduleReload]
  );
  useOrderHub(kitchenHubEvents, { joinKitchenGroup: true });

  // Update timer elapsed seconds every second for cooking items
  useEffect(() => {
    const interval = setInterval(() => {
      setKitchenItems((prevItems) =>
        prevItems.map((item) => {
          if (item.status === 'WAITING' || item.status === 'DOING') {
            return { ...item, elapsedSeconds: item.elapsedSeconds + 1 };
          }
          return item;
        })
      );
    }, 1000);
    return () => clearInterval(interval);
  }, []);

  // Format elapsed seconds into mm:ss
  const formatTime = (seconds: number) => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  };

  // Get timer color depending on duration
  const getTimerClass = (seconds: number, status: KitchenItemStatus) => {
    if (status === 'DONE') return 'normal';
    if (seconds >= 900) return 'danger'; // 15 mins
    if (seconds >= 480) return 'warning'; // 8 mins
    return 'normal';
  };

  // Transition toàn bộ món trong cùng 1 lượt order
  const handleOrderStatusChange = async (group: KitchenOrderGroup, newStatus: KitchenItemStatus) => {
    try {
      const itemIds = group.items.map((i) => i.id);
      await orderService.updateOrderItemsStatus(itemIds, newStatus);

      if (newStatus === 'SERVED') {
        setKitchenItems((prev) => prev.filter((item) => !itemIds.includes(item.id)));
        message.success(`Đã giao đơn #${group.orderId} (Bàn ${group.tableNumber})!`);
      } else {
        setKitchenItems((prev) =>
          prev.map((item) => (itemIds.includes(item.id) ? { ...item, status: newStatus } : item))
        );
        message.success(
          newStatus === 'DOING'
            ? `Đã nhận nấu đơn #${group.orderId} (${group.items.length} món)!`
            : `Đã nấu xong đơn #${group.orderId}! Chờ phục vụ giao.`
        );
      }
      scheduleReload();
    } catch (error) {
      message.error('Lỗi khi cập nhật trạng thái đơn.');
    }
  };

  // Kitchen Slip printing — 1 phiếu gồm tất cả món của đơn
  const handlePrintKitchenSlip = (group: KitchenOrderGroup) => {
    orderService.printKitchenOrderTicket({
      orderId: group.orderId,
      tableNumber: group.tableNumber,
      orderedAt: group.orderedAt,
      items: group.items.map((item) => ({
        name: item.name,
        quantity: item.quantity,
        notes: item.notes,
      })),
    });
    message.success(`Đã mở phiếu bếp đơn #${group.orderId} (${group.items.length} món).`);
  };

  // Table Checkout Bill printing
  const handlePrintCheckoutBill = (tableNo: number) => {
    orderService.printCheckoutBill(tableNo, activeOrders);
    message.success(`Đã in hóa đơn thanh toán cho Bàn ${tableNo}!`);
  };

  // Checkout table — xác nhận đã thu tiền → session CLOSED, bàn → MAINTENANCE (chờ dọn)
  const handleCheckoutTable = (tableNo: number) => {
    Modal.confirm({
      title: `Xác nhận thanh toán Bàn ${tableNo}`,
      content: `Xác nhận đã thu tiền cho Bàn ${tableNo}? Phiên ăn sẽ đóng và bàn chuyển sang trạng thái cần dọn (MAINTENANCE). Sau khi dọn xong, hãy đánh dấu Trống ở Quản lý bàn.`,
      okText: 'Xác nhận đã thu tiền',
      okType: 'primary',
      cancelText: 'Hủy',
      onOk: async () => {
        try {
          await orderService.completePaymentByTable(tableNo);
          setActiveOrders((prev) => prev.filter((o) => o.tableNumber !== tableNo));
          setKitchenItems((prev) => prev.filter((item) => item.tableNumber !== tableNo));
          setPendingCashTables((prev) => {
            const next = new Set(prev);
            next.delete(tableNo);
            return next;
          });
          setBillingModalVisible(false);
          setSelectedTable(null);
          message.success(`Thanh toán thành công! Bàn ${tableNo} đang chờ dọn (MAINTENANCE).`);
          loadData();
        } catch (err) {
          message.error('Không thể xác nhận thanh toán trên hệ thống.');
        }
      }
    });
  };

  const handleMarkTableCleaned = (table: RestaurantTable) => {
    Modal.confirm({
      title: `Đã dọn xong Bàn ${table.tableNumber}?`,
      content: 'Xác nhận bàn đã sạch và sẵn sàng nhận khách. Trạng thái sẽ chuyển thành Trống (AVAILABLE).',
      okText: 'Đã dọn — Trống',
      cancelText: 'Hủy',
      onOk: async () => {
        try {
          await tableService.updateTableStatus(table.id, 'AVAILABLE');
          setMaintenanceTables((prev) => prev.filter((t) => t.id !== table.id));
          message.success(`Bàn ${table.tableNumber} đã trống, sẵn sàng phục vụ.`);
        } catch {
          message.error('Không thể cập nhật trạng thái bàn.');
        }
      },
    });
  };

  // Group kitchen queue items by status, rồi gộp theo orderId
  const pendingGroups = useMemo(
    () => groupKitchenItemsByOrder(kitchenItems.filter((item) => item.status === 'WAITING')),
    [kitchenItems]
  );
  const preparingGroups = useMemo(
    () => groupKitchenItemsByOrder(kitchenItems.filter((item) => item.status === 'DOING')),
    [kitchenItems]
  );
  const readyGroups = useMemo(
    () => groupKitchenItemsByOrder(kitchenItems.filter((item) => item.status === 'DONE')),
    [kitchenItems]
  );

  // Group active tables from active orders + pending cash (fallback nếu order đã COMPLETED cũ)
  const activeTablesList = useMemo(() => {
    const tableMap: {
      [key: number]: {
        tableNumber: number;
        totalAmount: number;
        itemsCount: number;
        sessionStatus: string;
        awaitingCash: boolean;
        invoiceId?: string;
      };
    } = {};

    activeOrders.forEach((o) => {
      const tableNo = o.tableNumber;
      const amount = o.finalAmount;
      const count = o.items.reduce((sum, item) => sum + item.quantity, 0);
      const sessionStatus = o.sessionStatus || 'ACTIVE';
      const awaitingCash =
        sessionStatus === 'CHECKOUT' || pendingCashTables.has(tableNo);

      if (tableMap[tableNo]) {
        tableMap[tableNo].totalAmount += amount;
        tableMap[tableNo].itemsCount += count;
        if (sessionStatus === 'CHECKOUT') {
          tableMap[tableNo].sessionStatus = 'CHECKOUT';
          tableMap[tableNo].awaitingCash = true;
        }
      } else {
        tableMap[tableNo] = {
          tableNumber: tableNo,
          totalAmount: amount,
          itemsCount: count,
          sessionStatus,
          awaitingCash,
        };
      }
    });

    // Cộng VAT + phí DV theo settings cho tổng từ orders
    Object.values(tableMap).forEach((t) => {
      const net = t.totalAmount;
      const service = Math.round(net * serviceChargePercent / 100);
      const tax = Math.round(net * taxRatePercent / 100);
      t.totalAmount = net + service + tax;
    });

    // Đảm bảo bàn có pending cash vẫn hiện dù activeOrders trống
    pendingCashPayments.forEach((p) => {
      if (!tableMap[p.tableNumber]) {
        tableMap[p.tableNumber] = {
          tableNumber: p.tableNumber,
          totalAmount: p.amount, // pending cash amount đã gồm VAT từ BE
          itemsCount: 0,
          sessionStatus: 'CHECKOUT',
          awaitingCash: true,
          invoiceId: p.invoiceId,
        };
      } else {
        tableMap[p.tableNumber].awaitingCash = true;
        tableMap[p.tableNumber].sessionStatus = 'CHECKOUT';
        tableMap[p.tableNumber].invoiceId = p.invoiceId;
        // Ưu tiên số tiền từ payment PENDING (đã gồm VAT, khớp hóa đơn)
        tableMap[p.tableNumber].totalAmount = p.amount;
      }
    });

    return Object.values(tableMap)
      .filter((t) =>
        searchTableText === '' || t.tableNumber.toString().includes(searchTableText)
      )
      .sort((a, b) => Number(b.awaitingCash) - Number(a.awaitingCash) || a.tableNumber - b.tableNumber);
  }, [activeOrders, searchTableText, pendingCashTables, pendingCashPayments, taxRatePercent, serviceChargePercent]);

  const pendingCashCount = activeTablesList.filter((t) => t.awaitingCash).length;

  // Selected table billing details calculation
  const selectedTableBillDetails = useMemo(() => {
    if (selectedTable === null) return null;
    const tableOrders = activeOrders.filter((o) => o.tableNumber === selectedTable);
    
    const itemMap: { [key: string]: { name: string; unitPrice: number; quantity: number; total: number; notes: string[] } } = {};
    let subtotal = 0;
    let discount = 0;

    tableOrders.forEach((o) => {
      discount += o.discountAmount;
      o.items.forEach((item) => {
        const key = item.name + '_' + item.unitPrice;
        if (itemMap[key]) {
          itemMap[key].quantity += item.quantity;
          itemMap[key].total += item.unitPrice * item.quantity;
          if (item.notes) itemMap[key].notes.push(item.notes);
        } else {
          itemMap[key] = {
            name: item.name,
            unitPrice: item.unitPrice,
            quantity: item.quantity,
            total: item.unitPrice * item.quantity,
            notes: item.notes ? [item.notes] : []
          };
        }
        subtotal += item.unitPrice * item.quantity;
      });
    });

    return {
      tableNumber: selectedTable,
      items: Object.values(itemMap),
      subtotal,
      discount,
      serviceFee: Math.round((subtotal - discount) * serviceChargePercent / 100),
      vat: Math.round((subtotal - discount) * taxRatePercent / 100),
      taxRatePercent,
      serviceChargePercent,
      totalAmount: Math.round(
        (subtotal - discount) *
          (1 + taxRatePercent / 100 + serviceChargePercent / 100)
      ),
    };
  }, [selectedTable, activeOrders, taxRatePercent, serviceChargePercent]);

  return (
    <div className="staff-dashboard-container">
      {/* Header section */}
      <div className="staff-header-row">
        <div>
          <h2 className="staff-header-title">
            <FireOutlined className="staff-icon-fire" /> Staff Operations Dashboard
          </h2>
          <p className="staff-header-subtitle">
            Nhân viên điều hành: Quản lý chế biến món ăn và In hóa đơn thanh toán cho khách hàng.
          </p>
        </div>
        <Button 
          type="default" 
          icon={<PlayCircleOutlined />} 
          onClick={() => loadData(true)}
          className="staff-btn-simulate"
        >
          Tải lại dữ liệu
        </Button>
      </div>

      {isManagerViewOnly && (
        <div className="staff-manager-readonly-banner">
          Chế độ chỉ xem — Quản lý. Các thao tác nhận nấu/hoàn thành/thanh toán chỉ dành cho nhân viên phụ trách ca.
        </div>
      )}

      {/* Statistics Row */}
      <div className="staff-stats-row">
        <div className="staff-stat-card">
          <div className="staff-stat-icon pending"><ClockCircleOutlined /></div>
          <div className="staff-stat-info">
            <h4>Chờ chế biến</h4>
            <div>{pendingGroups.length} đơn</div>
          </div>
        </div>

        <div className="staff-stat-card">
          <div className="staff-stat-icon preparing"><FireOutlined /></div>
          <div className="staff-stat-info">
            <h4>Đang nấu</h4>
            <div>{preparingGroups.length} đơn</div>
          </div>
        </div>

        <div className="staff-stat-card">
          <div className="staff-stat-icon ready"><CoffeeOutlined /></div>
          <div className="staff-stat-info">
            <h4>Chờ giao khách</h4>
            <div>{readyGroups.length} đơn</div>
          </div>
        </div>

        <div className="staff-stat-card">
          <div className="staff-stat-icon billing"><DollarCircleOutlined /></div>
          <div className="staff-stat-info">
            <h4>Bàn đang dùng</h4>
            <div>{activeTablesList.length} bàn</div>
          </div>
        </div>

        <div className="staff-stat-card">
          <div className="staff-stat-icon pending"><TableOutlined /></div>
          <div className="staff-stat-info">
            <h4>Cần dọn bàn</h4>
            <div>{maintenanceTables.length} bàn</div>
          </div>
        </div>
      </div>

      {/* Main Tabs Container */}
      <div className="staff-main-tabs-card">
        <Tabs activeKey={activeTab} onChange={setActiveTab} className="staff-custom-tabs">
          {/* TAB 1: KITCHEN QUEUE */}
          <TabPane 
            tab={
              <span>
                <FireOutlined /> HÀNG CHỜ CHẾ BIẾN ({pendingGroups.length + preparingGroups.length + readyGroups.length})
              </span>
            } 
            key="queue"
          >
            <Spin spinning={loading}>
              <div className="staff-kanban-board">
                {/* WAITING COLUMN */}
                <div className="staff-kanban-column">
                  <div className="staff-column-header pending">
                    <span className="staff-column-title">
                      <span className="bullet-pink">●</span> CHỜ NHẬN (WAITING)
                    </span>
                    <span className="staff-column-count">{pendingGroups.length}</span>
                  </div>

                  <div className="staff-card-list">
                    {pendingGroups.length === 0 ? (
                      <div className="staff-column-empty">
                        <ClockCircleOutlined />
                        <p>Hàng chờ trống.<br />Chưa có món mới cần chế biến.</p>
                      </div>
                    ) : (
                      pendingGroups.map((group) => (
                        <div key={group.orderId} className="staff-item-card pending">
                          <div className="staff-card-header">
                            <span className="staff-table-badge">BÀN {group.tableNumber} · Đơn #{group.orderId}</span>
                            <span className={`staff-timer-badge ${getTimerClass(group.elapsedSeconds, group.status)}`}>
                              <ClockCircleOutlined /> {formatTime(group.elapsedSeconds)}
                            </span>
                          </div>

                          <div className="staff-card-body">
                            {group.items.map((item) => (
                              <div key={item.id} className="staff-order-line">
                                <div className="staff-item-name">{item.name}</div>
                                <div className="staff-item-qty">x{item.quantity}{item.notes ? ` · ${item.notes}` : ''}</div>
                              </div>
                            ))}
                          </div>

                          <div className="staff-card-footer">
                            <Button
                              type="default"
                              icon={<PrinterOutlined />}
                              onClick={() => handlePrintKitchenSlip(group)}
                              className="staff-btn-print-ticket"
                            >
                              In phiếu bếp
                            </Button>
                            {!isManagerViewOnly && (
                              <Button
                                type="primary"
                                icon={<PlayCircleOutlined />}
                                onClick={() => handleOrderStatusChange(group, 'DOING')}
                                className="staff-btn-action-pending"
                              >
                                Nhận nấu ({group.items.length})
                              </Button>
                            )}
                          </div>
                        </div>
                      ))
                    )}
                  </div>
                </div>

                {/* DOING COLUMN */}
                <div className="staff-kanban-column">
                  <div className="staff-column-header preparing">
                    <span className="staff-column-title">
                      <span className="bullet-blue">●</span> ĐANG CHẾ BIẾN (DOING)
                    </span>
                    <span className="staff-column-count">{preparingGroups.length}</span>
                  </div>

                  <div className="staff-card-list">
                    {preparingGroups.length === 0 ? (
                      <div className="staff-column-empty">
                        <FireOutlined />
                        <p>Bếp đang rảnh.<br />Hãy nhận món ở cột chờ.</p>
                      </div>
                    ) : (
                      preparingGroups.map((group) => (
                        <div key={group.orderId} className="staff-item-card preparing">
                          <div className="staff-card-header">
                            <span className="staff-table-badge">BÀN {group.tableNumber} · Đơn #{group.orderId}</span>
                            <span className={`staff-timer-badge ${getTimerClass(group.elapsedSeconds, group.status)}`}>
                              <ClockCircleOutlined /> {formatTime(group.elapsedSeconds)}
                            </span>
                          </div>

                          <div className="staff-card-body">
                            {group.items.map((item) => (
                              <div key={item.id} className="staff-order-line">
                                <div className="staff-item-name">{item.name}</div>
                                <div className="staff-item-qty">x{item.quantity}{item.notes ? ` · ${item.notes}` : ''}</div>
                              </div>
                            ))}
                          </div>

                          <div className="staff-card-footer">
                            <Button
                              type="default"
                              icon={<PrinterOutlined />}
                              onClick={() => handlePrintKitchenSlip(group)}
                              className="staff-btn-print-ticket"
                            >
                              In phiếu bếp
                            </Button>
                            {!isManagerViewOnly && (
                              <Button
                                type="primary"
                                icon={<CheckCircleOutlined />}
                                onClick={() => handleOrderStatusChange(group, 'DONE')}
                                className="staff-btn-action-preparing"
                              >
                                Hoàn thành ({group.items.length})
                              </Button>
                            )}
                          </div>
                        </div>
                      ))
                    )}
                  </div>
                </div>

                {/* DONE COLUMN */}
                <div className="staff-kanban-column">
                  <div className="staff-column-header ready">
                    <span className="staff-column-title">
                      <span className="bullet-green">●</span> CHỜ GIAO (DONE)
                    </span>
                    <span className="staff-column-count">{readyGroups.length}</span>
                  </div>

                  <div className="staff-card-list">
                    {readyGroups.length === 0 ? (
                      <div className="staff-column-empty">
                        <CoffeeOutlined />
                        <p>Không có món chờ giao.<br />Đang nấu hoàn thiện các món khác.</p>
                      </div>
                    ) : (
                      readyGroups.map((group) => (
                        <div key={group.orderId} className="staff-item-card ready">
                          <div className="staff-card-header">
                            <span className="staff-table-badge">BÀN {group.tableNumber} · Đơn #{group.orderId}</span>
                            <span className="staff-timer-badge normal">
                              <CheckOutlined /> Đã xong
                            </span>
                          </div>

                          <div className="staff-card-body">
                            {group.items.map((item) => (
                              <div key={item.id} className="staff-order-line">
                                <div className="staff-item-name">{item.name}</div>
                                <div className="staff-item-qty">x{item.quantity}{item.notes ? ` · ${item.notes}` : ''}</div>
                              </div>
                            ))}
                          </div>

                          <div className="staff-card-footer">
                            <Button
                              type="default"
                              icon={<PrinterOutlined />}
                              onClick={() => handlePrintKitchenSlip(group)}
                              className="staff-btn-print-ticket"
                            >
                              In lại phiếu
                            </Button>
                            {!isManagerViewOnly && (
                              <Button
                                type="primary"
                                icon={<DoubleRightOutlined />}
                                onClick={() => handleOrderStatusChange(group, 'SERVED')}
                                className="staff-btn-action-ready"
                              >
                                Đã giao bàn ({group.items.length})
                              </Button>
                            )}
                          </div>
                        </div>
                      ))
                    )}
                  </div>
                </div>
              </div>
            </Spin>
          </TabPane>

          {/* TAB 2: BILLING & CHECKOUT */}
          <TabPane 
            tab={
              <span>
                <DollarCircleOutlined /> THANH TOÁN & HÓA ĐƠN ({activeTablesList.length}
                {pendingCashCount > 0 ? ` · ${pendingCashCount} chờ thu` : ''})
              </span>
            } 
            key="billing"
          >
            <div className="staff-billing-tab-container">
              {/* Search Toolbar */}
              <div className="staff-billing-search-bar">
                <Input
                  placeholder="Tìm kiếm theo Số Bàn..."
                  prefix={<SearchOutlined style={{ color: '#bfbfbf' }} />}
                  value={searchTableText}
                  onChange={(e) => setSearchTableText(e.target.value)}
                  style={{ maxWidth: 300, borderRadius: 6, height: 38 }}
                  allowClear
                />
                <span className="staff-billing-total-count">
                  Danh sách gồm <strong>{activeTablesList.length}</strong> bàn đang dùng bữa
                  {pendingCashCount > 0 && (
                    <> · <strong style={{ color: '#d46b08' }}>{pendingCashCount}</strong> chờ thu tiền mặt</>
                  )}
                </span>
              </div>

              {/* Tables Cards Grid */}
              <Spin spinning={loading}>
                {activeTablesList.length === 0 ? (
                  <Empty 
                    image={Empty.PRESENTED_IMAGE_SIMPLE} 
                    description={
                      <span>Không tìm thấy bàn nào đang có hóa đơn hoạt động. <br />Đặt món giả lập để test.</span>
                    } 
                    style={{ padding: '48px 0' }}
                  />
                ) : (
                  <Row gutter={[20, 20]} style={{ marginTop: 12 }}>
                    {activeTablesList.map((table) => (
                      <Col xs={24} sm={12} md={8} lg={6} key={table.tableNumber}>
                        <Card 
                          className={`staff-table-billing-card${table.awaitingCash ? ' awaiting-cash' : ''}`}
                          bordered={false}
                          bodyStyle={{ padding: '20px' }}
                        >
                          <div className="staff-table-billing-card-header">
                            <div className="table-avatar">
                              <TableOutlined />
                            </div>
                            <div className="table-title">
                              <h3>BÀN {table.tableNumber}</h3>
                              <span>Số lượng món: {table.itemsCount}</span>
                              {table.awaitingCash && (
                                <span className="staff-cash-pending-badge">Chờ thu tiền mặt</span>
                              )}
                              {table.invoiceId && (
                                <span style={{ display: 'block', fontSize: 11, color: '#ad4e00', marginTop: 2 }}>
                                  {table.invoiceId}
                                </span>
                              )}
                            </div>
                          </div>

                          <div className="staff-table-billing-card-body">
                            <div className="amount-label">Tổng hóa đơn hiện tại</div>
                            <div className="amount-value">{table.totalAmount.toLocaleString('vi-VN')}đ</div>
                          </div>

                          <div className="staff-table-billing-card-actions">
                            <Button 
                              type="default" 
                              onClick={() => {
                                setSelectedTable(table.tableNumber);
                                setBillingModalVisible(true);
                              }}
                              className="btn-details"
                            >
                              Xem chi tiết
                            </Button>
                            {table.awaitingCash && !isManagerViewOnly ? (
                              <Button
                                type="primary"
                                icon={<CheckOutlined />}
                                onClick={() => handleCheckoutTable(table.tableNumber)}
                                className="btn-confirm-cash"
                                danger
                              >
                                Xác nhận thu tiền
                              </Button>
                            ) : (
                              <Button 
                                type="primary" 
                                icon={<PrinterOutlined />} 
                                onClick={() => handlePrintCheckoutBill(table.tableNumber)}
                                className="btn-print"
                              >
                                In hóa đơn
                              </Button>
                            )}
                          </div>
                        </Card>
                      </Col>
                    ))}
                  </Row>
                )}
              </Spin>

              {/* Bàn cần dọn sau thanh toán — STAFF chuyển MAINTENANCE → AVAILABLE */}
              <div className="staff-maintenance-section">
                <div className="staff-maintenance-header">
                  <h3>
                    <TableOutlined /> Bàn cần dọn ({maintenanceTables.length})
                  </h3>
                  <span>Sau khi dọn xong, đánh dấu Trống để nhận khách mới.</span>
                </div>
                {maintenanceTables.length === 0 ? (
                  <Empty
                    image={Empty.PRESENTED_IMAGE_SIMPLE}
                    description="Không có bàn đang chờ dọn."
                    style={{ padding: '24px 0' }}
                  />
                ) : (
                  <Row gutter={[16, 16]}>
                    {maintenanceTables.map((table) => (
                      <Col xs={24} sm={12} md={8} lg={6} key={table.id}>
                        <Card className="staff-maintenance-card" bordered={false} bodyStyle={{ padding: 16 }}>
                          <div className="staff-maintenance-card-title">BÀN {table.tableNumber}</div>
                          <div className="staff-maintenance-card-sub">
                            {table.locationName || 'Chưa gán khu vực'} · {table.capacity} chỗ
                          </div>
                          {!isManagerViewOnly && (
                            <Button
                              type="primary"
                              icon={<CheckCircleOutlined />}
                              block
                              onClick={() => handleMarkTableCleaned(table)}
                              className="staff-btn-mark-cleaned"
                            >
                              Đã dọn xong
                            </Button>
                          )}
                        </Card>
                      </Col>
                    ))}
                  </Row>
                )}
              </div>
            </div>
          </TabPane>
        </Tabs>
      </div>

      {/* DETAILED BILL DETAILS MODAL */}
      <Modal
        title={
          <div className="staff-modal-bill-title">
            <TableOutlined /> Chi tiết hóa đơn thanh toán - Bàn {selectedTable}
          </div>
        }
        open={billingModalVisible}
        onCancel={() => {
          setBillingModalVisible(false);
          setSelectedTable(null);
        }}
        width={500}
        footer={[
          <Button key="close" onClick={() => {
            setBillingModalVisible(false);
            setSelectedTable(null);
          }}>
            Đóng
          </Button>,
          <Button
            key="print"
            icon={<PrinterOutlined />}
            onClick={() => selectedTable && handlePrintCheckoutBill(selectedTable)}
          >
            In hóa đơn
          </Button>,
          ...(isManagerViewOnly ? [] : [
            <Button
              key="checkout"
              type="primary"
              icon={<CheckOutlined />}
              onClick={() => selectedTable && handleCheckoutTable(selectedTable)}
              danger
            >
              Xác nhận đã thu tiền
            </Button>
          ])
        ]}
      >
        {selectedTableBillDetails ? (
          <div className="staff-modal-bill-content">
            {(pendingCashTables.has(selectedTable!) ||
              activeOrders.some(
                (o) => o.tableNumber === selectedTable && o.sessionStatus === 'CHECKOUT'
              )) && (
              <div className="staff-modal-cash-banner">
                Khách đã chọn thanh toán tiền mặt — phiên đang khóa. Xác nhận sau khi thu đủ tiền tại quầy.
              </div>
            )}
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 12 }}>
              <span className="label-gray">Thời gian in phiếu:</span>
              <span className="val-dark">{new Date().toLocaleString('vi-VN')}</span>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
              <span className="label-gray">Nhân viên phụ trách:</span>
              <span className="val-dark">{user?.fullName || 'Sarah Williams'}</span>
            </div>

            <h4 className="bill-section-title">Danh sách món ăn đã đặt</h4>
            
            <List
              dataSource={selectedTableBillDetails.items}
              renderItem={(item) => (
                <List.Item className="bill-item-row" key={item.name}>
                  <div className="bill-item-info">
                    <div className="item-name">{item.name}</div>
                    {item.notes.length > 0 && (
                      <div className="item-notes">📝 Chú thích: {item.notes.join(', ')}</div>
                    )}
                  </div>
                  <div className="bill-item-qty-price">
                    <span>{item.quantity} x {item.unitPrice.toLocaleString('vi-VN')}đ</span>
                    <span className="item-total">{item.total.toLocaleString('vi-VN')}đ</span>
                  </div>
                </List.Item>
              )}
              style={{ maxHeight: 250, overflowY: 'auto' }}
            />

            <div className="bill-summary-section">
              <div className="summary-row">
                <span>Tạm tính:</span>
                <span>{selectedTableBillDetails.subtotal.toLocaleString('vi-VN')}đ</span>
              </div>
              {selectedTableBillDetails.discount > 0 && (
                <div className="summary-row discount">
                  <span>Giảm giá:</span>
                  <span>-{selectedTableBillDetails.discount.toLocaleString('vi-VN')}đ</span>
                </div>
              )}
              {selectedTableBillDetails.serviceFee > 0 && (
                <div className="summary-row">
                  <span>Phí dịch vụ ({selectedTableBillDetails.serviceChargePercent}%):</span>
                  <span>{selectedTableBillDetails.serviceFee.toLocaleString('vi-VN')}đ</span>
                </div>
              )}
              <div className="summary-row">
                <span>Thuế (VAT {selectedTableBillDetails.taxRatePercent}%):</span>
                <span>{selectedTableBillDetails.vat.toLocaleString('vi-VN')}đ</span>
              </div>
              <div className="summary-row total bold">
                <span>CẦN THANH TOÁN:</span>
                <span>{selectedTableBillDetails.totalAmount.toLocaleString('vi-VN')}đ</span>
              </div>
            </div>
          </div>
        ) : (
          <Empty description="Không có thông tin chi tiết hóa đơn." />
        )}
      </Modal>
    </div>
  );
};

export default StaffDashboardPage;
