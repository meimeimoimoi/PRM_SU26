import React, { useEffect, useState, useMemo } from 'react';
import { useSelector } from 'react-redux';
import { selectCurrentUser } from '@/store/slices/authSlice';
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
import { KitchenItem, KitchenItemStatus } from '@/types/chef';
import { orderService, OrderResponse } from '@/services/orderService';
import '@/styles/StaffDashboardPage.css';

const { TabPane } = Tabs;

const StaffDashboardPage: React.FC = () => {
  const user = useSelector(selectCurrentUser);
  // Kitchen Queue State
  const [kitchenItems, setKitchenItems] = useState<KitchenItem[]>([]);
  const [loading, setLoading] = useState<boolean>(false);


  // Billing State
  const [activeOrders, setActiveOrders] = useState<OrderResponse[]>([]);
  const [selectedTable, setSelectedTable] = useState<number | null>(null);
  const [billingModalVisible, setBillingModalVisible] = useState<boolean>(false);
  const [searchTableText, setSearchTableText] = useState<string>('');

  // Load active orders and kitchen items
  const loadData = async () => {
    setLoading(true);
    try {
      const orders = await orderService.getActiveOrders();
      setActiveOrders(orders);
      
      const items = await orderService.getKitchenItems();
      setKitchenItems(items);
    } catch (err) {
      message.error('Không thể tải danh sách dữ liệu.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

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

  // Transition dish status
  const handleStatusChange = async (id: number, newStatus: KitchenItemStatus) => {
    try {
      await orderService.updateItemStatus(id, newStatus);
      
      if (newStatus === 'SERVED') {
        setKitchenItems((prev) => prev.filter((item) => item.id !== id));
        message.success('Đã giao món thành công!');
      } else {
        setKitchenItems((prev) =>
          prev.map((item) => (item.id === id ? { ...item, status: newStatus } : item))
        );
        message.success(
          newStatus === 'DOING' 
            ? 'Đã nhận nấu món!' 
            : 'Đã nấu xong! Chờ phục vụ giao.'
        );
      }
      // Reload active orders from service in background to keep billing aligned
      const orders = await orderService.getActiveOrders();
      setActiveOrders(orders);
    } catch (error) {
      message.error('Lỗi khi cập nhật trạng thái món.');
    }
  };

  // Kitchen Slip printing
  const handlePrintKitchenSlip = (item: KitchenItem) => {
    orderService.printKitchenTicket(item);
    message.success(`Đã xuất lệnh in phiếu bếp cho món ${item.name}!`);
  };

  // Table Checkout Bill printing
  const handlePrintCheckoutBill = (tableNo: number) => {
    orderService.printCheckoutBill(tableNo, activeOrders);
    message.success(`Đã in hóa đơn thanh toán cho Bàn ${tableNo}!`);
  };

  // Checkout table (Archive/Close table session locally)
  const handleCheckoutTable = (tableNo: number) => {
    Modal.confirm({
      title: `Xác nhận thanh toán Bàn ${tableNo}`,
      content: `Bạn có chắc chắn muốn xác nhận thanh toán và đóng phiên phục vụ của Bàn ${tableNo}? Thao tác này sẽ xóa bàn khỏi danh sách hóa đơn hiện tại.`,
      okText: 'Thanh toán & Đóng bàn',
      okType: 'primary',
      cancelText: 'Hủy',
      onOk: async () => {
        try {
          await orderService.completePaymentByTable(tableNo);
          setActiveOrders((prev) => prev.filter((o) => o.tableNumber !== tableNo));
          setKitchenItems((prev) => prev.filter((item) => item.tableNumber !== tableNo));
          setBillingModalVisible(false);
          setSelectedTable(null);
          message.success(`Thanh toán thành công! Bàn ${tableNo} hiện đã trống.`);
        } catch (err) {
          message.error('Không thể xác nhận thanh toán trên hệ thống.');
        }
      }
    });
  };

  // Group kitchen queue items by status
  const pendingItems = kitchenItems.filter((item) => item.status === 'WAITING');
  const preparingItems = kitchenItems.filter((item) => item.status === 'DOING');
  const readyItems = kitchenItems.filter((item) => item.status === 'DONE');

  // Group active tables from active orders
  const activeTablesList = useMemo(() => {
    const tableMap: { [key: number]: { tableNumber: number; totalAmount: number; itemsCount: number } } = {};
    activeOrders.forEach((o) => {
      const tableNo = o.tableNumber;
      const amount = o.finalAmount;
      const count = o.items.reduce((sum, item) => sum + item.quantity, 0);

      if (tableMap[tableNo]) {
        tableMap[tableNo].totalAmount += amount;
        tableMap[tableNo].itemsCount += count;
      } else {
        tableMap[tableNo] = {
          tableNumber: tableNo,
          totalAmount: amount,
          itemsCount: count
        };
      }
    });

    return Object.values(tableMap).filter((t) => 
      searchTableText === '' || t.tableNumber.toString().includes(searchTableText)
    );
  }, [activeOrders, searchTableText]);

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
      totalAmount: subtotal - discount
    };
  }, [selectedTable, activeOrders]);

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
          onClick={loadData}
          className="staff-btn-simulate"
        >
          Tải lại dữ liệu
        </Button>
      </div>

      {/* Statistics Row */}
      <div className="staff-stats-row">
        <div className="staff-stat-card">
          <div className="staff-stat-icon pending"><ClockCircleOutlined /></div>
          <div className="staff-stat-info">
            <h4>Chờ chế biến</h4>
            <div>{pendingItems.length} món</div>
          </div>
        </div>

        <div className="staff-stat-card">
          <div className="staff-stat-icon preparing"><FireOutlined /></div>
          <div className="staff-stat-info">
            <h4>Đang nấu</h4>
            <div>{preparingItems.length} món</div>
          </div>
        </div>

        <div className="staff-stat-card">
          <div className="staff-stat-icon ready"><CoffeeOutlined /></div>
          <div className="staff-stat-info">
            <h4>Chờ giao khách</h4>
            <div>{readyItems.length} món</div>
          </div>
        </div>

        <div className="staff-stat-card">
          <div className="staff-stat-icon billing"><DollarCircleOutlined /></div>
          <div className="staff-stat-info">
            <h4>Bàn đang dùng</h4>
            <div>{activeTablesList.length} bàn</div>
          </div>
        </div>
      </div>

      {/* Main Tabs Container */}
      <div className="staff-main-tabs-card">
        <Tabs defaultActiveKey="queue" className="staff-custom-tabs">
          {/* TAB 1: KITCHEN QUEUE */}
          <TabPane 
            tab={
              <span>
                <FireOutlined /> HÀNG CHỜ CHẾ BIẾN ({kitchenItems.length})
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
                    <span className="staff-column-count">{pendingItems.length}</span>
                  </div>

                  <div className="staff-card-list">
                    {pendingItems.length === 0 ? (
                      <div className="staff-column-empty">
                        <ClockCircleOutlined />
                        <p>Hàng chờ trống.<br />Chưa có món mới cần chế biến.</p>
                      </div>
                    ) : (
                      pendingItems.map((item) => (
                        <div key={item.id} className="staff-item-card pending">
                          <div className="staff-card-header">
                            <span className="staff-table-badge">BÀN {item.tableNumber}</span>
                            <span className={`staff-timer-badge ${getTimerClass(item.elapsedSeconds, item.status)}`}>
                              <ClockCircleOutlined /> {formatTime(item.elapsedSeconds)}
                            </span>
                          </div>

                          <div className="staff-card-body">
                            <div className="staff-item-name">{item.name}</div>
                            <div className="staff-item-qty">Số lượng: {item.quantity}</div>
                            {item.notes && <div className="staff-item-notes">📝 Chú thích: {item.notes}</div>}
                          </div>

                          <div className="staff-card-footer">
                            <Button 
                              type="default" 
                              icon={<PrinterOutlined />} 
                              onClick={() => handlePrintKitchenSlip(item)}
                              className="staff-btn-print-ticket"
                            >
                              In phiếu bếp
                            </Button>
                            <Button 
                              type="primary" 
                              icon={<PlayCircleOutlined />} 
                              onClick={() => handleStatusChange(item.id, 'DOING')}
                              className="staff-btn-action-pending"
                            >
                              Nhận nấu
                            </Button>
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
                    <span className="staff-column-count">{preparingItems.length}</span>
                  </div>

                  <div className="staff-card-list">
                    {preparingItems.length === 0 ? (
                      <div className="staff-column-empty">
                        <FireOutlined />
                        <p>Bếp đang rảnh.<br />Hãy nhận món ở cột chờ.</p>
                      </div>
                    ) : (
                      preparingItems.map((item) => (
                        <div key={item.id} className="staff-item-card preparing">
                          <div className="staff-card-header">
                            <span className="staff-table-badge">BÀN {item.tableNumber}</span>
                            <span className={`staff-timer-badge ${getTimerClass(item.elapsedSeconds, item.status)}`}>
                              <ClockCircleOutlined /> {formatTime(item.elapsedSeconds)}
                            </span>
                          </div>

                          <div className="staff-card-body">
                            <div className="staff-item-name">{item.name}</div>
                            <div className="staff-item-qty">Số lượng: {item.quantity}</div>
                            {item.notes && <div className="staff-item-notes">📝 Chú thích: {item.notes}</div>}
                          </div>

                          <div className="staff-card-footer">
                            <Button 
                              type="default" 
                              icon={<PrinterOutlined />} 
                              onClick={() => handlePrintKitchenSlip(item)}
                              className="staff-btn-print-ticket"
                            >
                              In phiếu bếp
                            </Button>
                            <Button 
                              type="primary" 
                              icon={<CheckCircleOutlined />} 
                              onClick={() => handleStatusChange(item.id, 'DONE')}
                              className="staff-btn-action-preparing"
                            >
                              Hoàn thành
                            </Button>
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
                    <span className="staff-column-count">{readyItems.length}</span>
                  </div>

                  <div className="staff-card-list">
                    {readyItems.length === 0 ? (
                      <div className="staff-column-empty">
                        <CoffeeOutlined />
                        <p>Không có món chờ giao.<br />Đang nấu hoàn thiện các món khác.</p>
                      </div>
                    ) : (
                      readyItems.map((item) => (
                        <div key={item.id} className="staff-item-card ready">
                          <div className="staff-card-header">
                            <span className="staff-table-badge">BÀN {item.tableNumber}</span>
                            <span className="staff-timer-badge normal">
                              <CheckOutlined /> Đã xong
                            </span>
                          </div>

                          <div className="staff-card-body">
                            <div className="staff-item-name">{item.name}</div>
                            <div className="staff-item-qty">Số lượng: {item.quantity}</div>
                            {item.notes && <div className="staff-item-notes">📝 Chú thích: {item.notes}</div>}
                          </div>

                          <div className="staff-card-footer">
                            <Button 
                              type="default" 
                              icon={<PrinterOutlined />} 
                              onClick={() => handlePrintKitchenSlip(item)}
                              className="staff-btn-print-ticket"
                            >
                              In lại phiếu
                            </Button>
                            <Button 
                              type="primary" 
                              icon={<DoubleRightOutlined />} 
                              onClick={() => handleStatusChange(item.id, 'SERVED')}
                              className="staff-btn-action-ready"
                            >
                              Đã giao bàn
                            </Button>
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
                <DollarCircleOutlined /> THÀNH TOÁN & HÓA ĐƠN ({activeTablesList.length})
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
                          className="staff-table-billing-card"
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
                            <Button 
                              type="primary" 
                              icon={<PrinterOutlined />} 
                              onClick={() => handlePrintCheckoutBill(table.tableNumber)}
                              className="btn-print"
                            >
                              In hóa đơn
                            </Button>
                          </div>
                        </Card>
                      </Col>
                    ))}
                  </Row>
                )}
              </Spin>
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
          <Button 
            key="checkout" 
            type="primary" 
            icon={<CheckOutlined />} 
            onClick={() => selectedTable && handleCheckoutTable(selectedTable)}
            danger
          >
            Thành toán & Trả bàn
          </Button>
        ]}
      >
        {selectedTableBillDetails ? (
          <div className="staff-modal-bill-content">
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
