import React, { useEffect, useState } from 'react';
import { Button, message, Spin } from 'antd';
import { 
  PlayCircleOutlined, 
  CheckCircleOutlined, 
  ClockCircleOutlined, 
  FireOutlined, 
  CoffeeOutlined, 
  DoubleRightOutlined,
  ThunderboltOutlined,
  CheckOutlined
} from '@ant-design/icons';
import { KitchenItem, KitchenItemStatus } from '@/types/chef';
import { chefService } from '@/services/chefService';
import '@/styles/ChefPage.css';

const ChefPage: React.FC = () => {
  const [items, setItems] = useState<KitchenItem[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [completedCount, setCompletedCount] = useState<number>(18); // Simulated completed stats

  // Load initial orders
  const loadOrders = async () => {
    setLoading(true);
    try {
      const data = await chefService.getKitchenItems();
      setItems(data);
    } catch (err) {
      message.error('Không thể tải danh sách chế biến.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadOrders();
  }, []);

  // Update timer seconds every second
  useEffect(() => {
    const interval = setInterval(() => {
      setItems((prevItems) =>
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

  // Transition handler
  const handleStatusChange = async (id: number, newStatus: KitchenItemStatus) => {
    try {
      // Try to update through service API
      await chefService.updateItemStatus(id, newStatus);
      
      // Update local state directly
      if (newStatus === 'SERVED') {
        setItems((prev) => prev.filter((item) => item.id !== id));
        setCompletedCount((prev) => prev + 1);
        message.success('Đã giao món thành công!');
      } else {
        setItems((prev) =>
          prev.map((item) => (item.id === id ? { ...item, status: newStatus } : item))
        );
        message.success(
          newStatus === 'DOING' 
            ? 'Đã nhận món chế biến!' 
            : 'Đã nấu xong! Chờ phục vụ giao.'
        );
      }
    } catch (error) {
      // Offline fallback behavior
      if (newStatus === 'SERVED') {
        setItems((prev) => prev.filter((item) => item.id !== id));
        setCompletedCount((prev) => prev + 1);
        message.success('Đã giao món thành công! (Chế độ offline)');
      } else {
        setItems((prev) =>
          prev.map((item) => (item.id === id ? { ...item, status: newStatus } : item))
        );
        message.success(
          newStatus === 'DOING' 
            ? 'Đã nhận chế biến! (Chế độ offline)' 
            : 'Đã hoàn thành món! (Chế độ offline)'
        );
      }
    }
  };

  // Order Simulator: Inject a random kitchen item
  const handleSimulateOrder = () => {
    const randomDishes = [
      { name: 'Bánh Mì Thịt Nướng', notes: 'Ít ớt, nhiều dưa chua' },
      { name: 'Cà Phê Sữa Đá', notes: 'Ít ngọt' },
      { name: 'Phở Bò Đặc Biệt', notes: 'Nước béo' },
      { name: 'Gỏi Cuốn Tôm Thịt', notes: '' },
      { name: 'Phở Gà Trứng Non', notes: 'Thêm hành trần' },
      { name: 'Trà Đào Đá Xay', notes: 'Thêm thạch đào' }
    ];

    const randomDish = randomDishes[Math.floor(Math.random() * randomDishes.length)];
    const newId = Math.floor(10000 + Math.random() * 90000);
    const newOrder: KitchenItem = {
      id: newId,
      orderId: Math.floor(1000 + Math.random() * 9000),
      tableNumber: Math.floor(1 + Math.random() * 15),
      name: randomDish.name,
      quantity: Math.floor(1 + Math.random() * 3),
      notes: randomDish.notes || undefined,
      status: 'WAITING',
      orderedAt: new Date().toISOString(),
      elapsedSeconds: 0
    };

    setItems((prev) => [newOrder, ...prev]);
    message.info(`🔔 Món mới đã gửi xuống bếp: ${newOrder.quantity}x ${newOrder.name}`);
  };

  // Group items by status
  const pendingItems = items.filter((item) => item.status === 'WAITING');
  const preparingItems = items.filter((item) => item.status === 'DOING');
  const readyItems = items.filter((item) => item.status === 'DONE');

  return (
    <div className="chef-kitchen-container">
      {/* Header section with simulator */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 16 }}>
        <div>
          <h2 style={{ margin: 0, fontSize: '28px', fontWeight: 700, color: '#1a202c' }}>
            <FireOutlined style={{ color: '#ff4d4f', marginRight: 8 }} /> Kitchen Queue Dashboard
          </h2>
          <p style={{ margin: 0, color: '#718096', fontSize: '14px' }}>
            Điều hành món ăn, theo dõi thời gian và quản lý chế biến thời gian thực.
          </p>
        </div>
        <Button 
          type="primary" 
          danger
          icon={<ThunderboltOutlined />} 
          onClick={handleSimulateOrder}
          style={{ height: 38, borderRadius: 6, fontWeight: 600 }}
        >
          Giả lập order mới xuống bếp
        </Button>
      </div>

      {/* Simulator banner alert */}
      <div className="simulation-banner">
        <span>💡 <strong>Mô phỏng:</strong> Bạn có thể bấm nút đỏ góc trên để tạo ngẫu nhiên món ăn mới chuyển đến hàng chờ!</span>
        <Button size="small" type="link" onClick={loadOrders}>Tải lại gốc</Button>
      </div>

      {/* Counter Statistics Row */}
      <div className="chef-stats-row">
        <div className="chef-stat-card">
          <div className="chef-stat-icon pending">
            <ClockCircleOutlined />
          </div>
          <div className="chef-stat-info">
            <h4>Chờ nhận món</h4>
            <div>{pendingItems.length} món</div>
          </div>
        </div>

        <div className="chef-stat-card">
          <div className="chef-stat-icon preparing">
            <FireOutlined />
          </div>
          <div className="chef-stat-info">
            <h4>Đang nấu</h4>
            <div>{preparingItems.length} món</div>
          </div>
        </div>

        <div className="chef-stat-card">
          <div className="chef-stat-icon ready">
            <CoffeeOutlined />
          </div>
          <div className="chef-stat-info">
            <h4>Chờ phục vụ</h4>
            <div>{readyItems.length} món</div>
          </div>
        </div>

        <div className="chef-stat-card">
          <div className="chef-stat-icon completed">
            <CheckCircleOutlined />
          </div>
          <div className="chef-stat-info">
            <h4>Đã hoàn thành hôm nay</h4>
            <div>{completedCount} món</div>
          </div>
        </div>
      </div>

      {/* Kanban Board columns */}
      <Spin spinning={loading}>
        <div className="chef-kanban-board">
        {/* COLUMN 1: PENDING */}
        <div className="chef-kanban-column">
          <div className="chef-column-header pending">
            <span className="chef-column-title">
              <span style={{ color: '#eb2f96' }}>●</span> CHỜ NHẬN (WAITING)
            </span>
            <span className="chef-column-count">{pendingItems.length}</span>
          </div>

          <div className="chef-card-list">
            {pendingItems.length === 0 ? (
              <div className="chef-column-empty">
                <ClockCircleOutlined />
                <p>Hàng chờ trống.<br />Không có món ăn mới cần nhận.</p>
              </div>
            ) : (
              pendingItems.map((item) => (
                <div key={item.id} className="chef-item-card pending">
                  <div className="chef-card-header">
                    <span className="chef-table-badge">BÀN {item.tableNumber}</span>
                    <span className={`chef-timer-badge ${getTimerClass(item.elapsedSeconds, item.status)}`}>
                      <ClockCircleOutlined /> {formatTime(item.elapsedSeconds)}
                    </span>
                  </div>

                  <div className="chef-card-body">
                    <div className="chef-item-name">{item.name}</div>
                    <div className="chef-item-qty">Số lượng: {item.quantity}</div>
                    {item.notes && <div className="chef-item-notes">📝 Chú thích: {item.notes}</div>}
                  </div>

                  <div className="chef-card-footer">
                    <Button 
                      type="primary" 
                      icon={<PlayCircleOutlined />} 
                      onClick={() => handleStatusChange(item.id, 'DOING')}
                      style={{ 
                        backgroundColor: '#eb2f96', 
                        borderColor: '#eb2f96', 
                        borderRadius: 6,
                        fontWeight: 500
                      }}
                    >
                      Nhận nấu
                    </Button>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>

        {/* COLUMN 2: PREPARING */}
        <div className="chef-kanban-column">
          <div className="chef-column-header preparing">
            <span className="chef-column-title">
              <span style={{ color: '#1890ff' }}>●</span> ĐANG CHẾ BIẾN (DOING)
            </span>
            <span className="chef-column-count">{preparingItems.length}</span>
          </div>

          <div className="chef-card-list">
            {preparingItems.length === 0 ? (
              <div className="chef-column-empty">
                <FireOutlined />
                <p>Bếp đang rảnh.<br />Nhận món ở cột chờ để nấu.</p>
              </div>
            ) : (
              preparingItems.map((item) => (
                <div key={item.id} className="chef-item-card preparing">
                  <div className="chef-card-header">
                    <span className="chef-table-badge">BÀN {item.tableNumber}</span>
                    <span className={`chef-timer-badge ${getTimerClass(item.elapsedSeconds, item.status)}`}>
                      <ClockCircleOutlined /> {formatTime(item.elapsedSeconds)}
                    </span>
                  </div>

                  <div className="chef-card-body">
                    <div className="chef-item-name">{item.name}</div>
                    <div className="chef-item-qty">Số lượng: {item.quantity}</div>
                    {item.notes && <div className="chef-item-notes">📝 Chú thích: {item.notes}</div>}
                  </div>

                  <div className="chef-card-footer">
                    <Button 
                      type="primary" 
                      icon={<CheckCircleOutlined />} 
                      onClick={() => handleStatusChange(item.id, 'DONE')}
                      style={{ 
                        backgroundColor: '#1890ff', 
                        borderColor: '#1890ff', 
                        borderRadius: 6,
                        fontWeight: 500
                      }}
                    >
                      Hoàn thành
                    </Button>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>

        {/* COLUMN 3: READY */}
        <div className="chef-kanban-column">
          <div className="chef-column-header ready">
            <span className="chef-column-title">
              <span style={{ color: '#52c41a' }}>●</span> CHỜ GIAO (DONE)
            </span>
            <span className="chef-column-count">{readyItems.length}</span>
          </div>

          <div className="chef-card-list">
            {readyItems.length === 0 ? (
              <div className="chef-column-empty">
                <CoffeeOutlined />
                <p>Không có món chờ giao.<br />Hoàn thành món ăn để phục vụ lấy.</p>
              </div>
            ) : (
              readyItems.map((item) => (
                <div key={item.id} className="chef-item-card ready">
                  <div className="chef-card-header">
                    <span className="chef-table-badge">BÀN {item.tableNumber}</span>
                    <span className="chef-timer-badge normal">
                      <CheckOutlined /> Xong
                    </span>
                  </div>

                  <div className="chef-card-body">
                    <div className="chef-item-name">{item.name}</div>
                    <div className="chef-item-qty">Số lượng: {item.quantity}</div>
                    {item.notes && <div className="chef-item-notes">📝 Chú thích: {item.notes}</div>}
                  </div>

                  <div className="chef-card-footer">
                    <Button 
                      type="primary" 
                      icon={<DoubleRightOutlined />} 
                      onClick={() => handleStatusChange(item.id, 'SERVED')}
                      style={{ 
                        backgroundColor: '#52c41a', 
                        borderColor: '#52c41a', 
                        borderRadius: 6,
                        fontWeight: 500
                      }}
                    >
                      Xác nhận giao món
                    </Button>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      </div>
      </Spin>
    </div>
  );
};

export default ChefPage;
