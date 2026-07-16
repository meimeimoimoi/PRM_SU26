import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useSelector } from 'react-redux';
import { Badge, Dropdown, List, Typography, Empty, Space } from 'antd';
import { BellOutlined, DollarCircleOutlined, WarningOutlined } from '@ant-design/icons';
import { selectCurrentUser } from '@/store/slices/authSlice';
import { useOrderHub } from '@/hooks/useOrderHub';
import { menuService } from '@/services/menuService';

interface NotificationItem {
  id: string;
  type: 'payment' | 'alert';
  message: string;
  time: string;
}

const formatVnd = (n: number) => `${new Intl.NumberFormat('vi-VN').format(n)}đ`;

/**
 * Chuông thông báo — không có API/bảng notification riêng ở BE, nên nội dung được
 * ghép từ 2 nguồn có sẵn:
 *   1. "Thanh toán thành công" — realtime qua SignalR event ReceivePaymentSuccess
 *      (OrderHub broadcast thêm vào KitchenGroup, xem OrderNotificationService.cs).
 *   2. "Cảnh báo hệ thống" — số món đang tắt "còn hàng", tính từ danh sách menu đã tải
 *      (chỉ MANAGER mới thấy, vì chỉ Manager mới quản lý menu).
 * Danh sách thanh toán chỉ tồn tại trong phiên làm việc (không lưu DB) — reload trang sẽ mất,
 * chấp nhận được vì mục đích là nhắc realtime, không phải lịch sử giao dịch (đã có ở Transactions).
 */
const NotificationBell: React.FC = () => {
  const user = useSelector(selectCurrentUser);
  const isKitchenStaff = user?.role === 'MANAGER' || user?.role === 'STAFF';
  const isManager = user?.role === 'MANAGER';

  const [paymentNotifications, setPaymentNotifications] = useState<NotificationItem[]>([]);
  const [outOfStockCount, setOutOfStockCount] = useState(0);

  const kitchenHubEvents = useMemo(
    () => ({
      ReceivePaymentSuccess: (data: any) => {
        setPaymentNotifications((prev) => [
          {
            id: `${data.invoiceId || data.InvoiceId}-${Date.now()}`,
            type: 'payment' as const,
            message: `Bàn T-${(data.tableNumber ?? data.tableId ?? data.TableId ?? '').toString().padStart(2, '0')} đã thanh toán ${formatVnd(data.amount ?? data.Amount ?? 0)}`,
            time: new Date(data.timestamp || data.Timestamp || Date.now()).toLocaleTimeString('vi-VN')
          },
          ...prev
        ].slice(0, 20));
      }
    }),
    []
  );
  useOrderHub(kitchenHubEvents, { joinKitchenGroup: isKitchenStaff });

  const checkOutOfStock = useCallback(async () => {
    if (!isManager) return;
    try {
      const items = await menuService.getAllMenuItems();
      setOutOfStockCount(items.filter((i) => !i.isAvailable).length);
    } catch {
      // Chuông thông báo là tiện ích phụ — lỗi tải menu không nên làm hỏng cả trang.
    }
  }, [isManager]);

  useEffect(() => {
    checkOutOfStock();
  }, [checkOutOfStock]);

  const alertItems: NotificationItem[] = useMemo(
    () =>
      outOfStockCount > 0
        ? [{ id: 'out-of-stock', type: 'alert' as const, message: `${outOfStockCount} món đang hết hàng`, time: '' }]
        : [],
    [outOfStockCount]
  );

  const allItems = [...alertItems, ...paymentNotifications];

  return (
    <Dropdown
      trigger={['click']}
      placement="bottomRight"
      onOpenChange={(open) => {
        if (open) checkOutOfStock();
      }}
      dropdownRender={() => (
        <div
          style={{
            width: 320,
            maxHeight: 400,
            overflowY: 'auto',
            background: '#fff',
            borderRadius: 8,
            boxShadow: '0 2px 8px rgba(0,0,0,0.15)'
          }}
        >
          <div style={{ padding: '10px 16px', fontWeight: 600, borderBottom: '1px solid #f0f0f0' }}>
            Thông báo
          </div>
          {allItems.length === 0 ? (
            <Empty description="Không có thông báo mới" style={{ padding: 24 }} />
          ) : (
            <List
              dataSource={allItems}
              renderItem={(item) => (
                <List.Item style={{ padding: '10px 16px' }}>
                  <Space align="start">
                    {item.type === 'payment' ? (
                      <DollarCircleOutlined style={{ color: '#52c41a', marginTop: 3 }} />
                    ) : (
                      <WarningOutlined style={{ color: '#faad14', marginTop: 3 }} />
                    )}
                    <div>
                      <div style={{ fontSize: 13 }}>{item.message}</div>
                      {item.time && (
                        <Typography.Text type="secondary" style={{ fontSize: 11 }}>
                          {item.time}
                        </Typography.Text>
                      )}
                    </div>
                  </Space>
                </List.Item>
              )}
            />
          )}
        </div>
      )}
    >
      <Badge count={allItems.length} size="small" offset={[-2, 2]}>
        <BellOutlined style={{ fontSize: 18, color: '#4a5568', cursor: 'pointer' }} />
      </Badge>
    </Dropdown>
  );
};

export default NotificationBell;
