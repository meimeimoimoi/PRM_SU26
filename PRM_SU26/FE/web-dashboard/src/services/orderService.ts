import { apiClient } from './api/client';
import { KitchenItem, KitchenItemStatus } from '@/types/chef';

export interface OrderDetailItem {
  id: number;
  menuItemId: number;
  name: string;
  unitPrice: number;
  quantity: number;
  notes?: string;
  status: string;
}

export interface OrderResponse {
  id: number;
  customerId?: number;
  customerName?: string;
  tableNumber: number;
  totalAmount: number;
  discountAmount: number;
  finalAmount: number;
  status: string;
  createdAt: string;
  items: OrderDetailItem[];
}

// Global variable to hold local mock state for simulator and offline operations
let mockActiveOrders: OrderResponse[] = [
  {
    id: 8921,
    tableNumber: 5,
    customerName: 'Anh Tuấn',
    totalAmount: 180000,
    discountAmount: 0,
    finalAmount: 180000,
    status: 'PENDING',
    createdAt: new Date(Date.now() - 12 * 60 * 1000).toISOString(),
    items: [
      {
        id: 1001,
        menuItemId: 1,
        name: 'Phở Bò Đặc Biệt',
        unitPrice: 65000,
        quantity: 2,
        notes: 'Không hành, nhiều bánh phở',
        status: 'WAITING'
      },
      {
        id: 1002,
        menuItemId: 2,
        name: 'Gỏi Cuốn Tôm Thịt',
        unitPrice: 15000,
        quantity: 3,
        notes: 'Nước chấm tương đen',
        status: 'WAITING'
      }
    ]
  },
  {
    id: 8922,
    tableNumber: 12,
    customerName: 'Chị Vy',
    totalAmount: 135000,
    discountAmount: 15000,
    finalAmount: 120000,
    status: 'PREPARING',
    createdAt: new Date(Date.now() - 20 * 60 * 1000).toISOString(),
    items: [
      {
        id: 1003,
        menuItemId: 3,
        name: 'Bánh Mì Thịt Nướng',
        unitPrice: 35000,
        quantity: 3,
        notes: 'Cắt đôi bánh mì',
        status: 'DOING'
      },
      {
        id: 1004,
        menuItemId: 4,
        name: 'Cà Phê Sữa Đá',
        unitPrice: 30000,
        quantity: 1,
        status: 'DONE'
      }
    ]
  },
  {
    id: 8923,
    tableNumber: 3,
    customerName: 'Minh Hoàng',
    totalAmount: 45000,
    discountAmount: 0,
    finalAmount: 45000,
    status: 'PENDING',
    createdAt: new Date(Date.now() - 5 * 60 * 1000).toISOString(),
    items: [
      {
        id: 1005,
        menuItemId: 5,
        name: 'Trà Đào Đá Xay',
        unitPrice: 45000,
        quantity: 1,
        notes: 'Nhiều đào',
        status: 'WAITING'
      }
    ]
  }
];

export const orderService = {
  // Lấy toàn bộ đơn hàng đang hoạt động từ backend
  getActiveOrders: async (): Promise<OrderResponse[]> => {
    try {
      const response = await apiClient.get<any>('/orders/active');
      const data = response.data.data || response.data;
      if (data && Array.isArray(data) && data.length > 0) {
        return data;
      }
      return mockActiveOrders;
    } catch (error) {
      console.warn('API /orders/active lỗi hoặc chưa khởi chạy. Sử dụng dữ liệu offline.');
      return mockActiveOrders;
    }
  },

  // Lấy các món ăn cần chế biến cho bếp bằng cách làm phẳng (flatten) các Order
  getKitchenItems: async (): Promise<KitchenItem[]> => {
    const orders = await orderService.getActiveOrders();
    const items: KitchenItem[] = [];

    orders.forEach((order) => {
      order.items.forEach((item) => {
        // Chỉ hiện thị các món chưa giao (SERVED) hoặc đã hủy (CANCELLED)
        if (item.status !== 'SERVED' && item.status !== 'CANCELLED') {
          const orderedTime = new Date(order.createdAt).getTime();
          const elapsed = Math.floor((Date.now() - orderedTime) / 1000);
          items.push({
            id: item.id,
            orderId: order.id,
            tableNumber: order.tableNumber,
            name: item.name,
            quantity: item.quantity,
            notes: item.notes,
            status: item.status as KitchenItemStatus,
            orderedAt: order.createdAt,
            elapsedSeconds: elapsed > 0 ? elapsed : 0
          });
        }
      });
    });

    // Sắp xếp thời gian cũ hơn lên đầu (FIFO)
    return items.sort((a, b) => new Date(a.orderedAt).getTime() - new Date(b.orderedAt).getTime());
  },

  // Cập nhật trạng thái của món ăn
  updateItemStatus: async (itemId: number, newStatus: KitchenItemStatus): Promise<boolean> => {
    try {
      // Gọi API cập nhật trạng thái món ăn nếu backend hỗ trợ
      await apiClient.patch(`/kitchen/items/${itemId}/status`, { status: newStatus });
    } catch (e) {
      // Fallback: Cập nhật trong mockActiveOrders local để simulator chạy đồng bộ
      mockActiveOrders = mockActiveOrders.map((order) => {
        const updatedItems = order.items.map((item) => {
          if (item.id === itemId) {
            return { ...item, status: newStatus };
          }
          return item;
        });
        return { ...order, items: updatedItems };
      });
    }
    return true;
  },

  // Giả lập khách hàng đặt món mới
  simulateNewOrder: (tableNo?: number): KitchenItem[] => {
    const randomDishes = [
      { name: 'Phở Bò Đặc Biệt', price: 65000, notes: 'Nước béo, nhiều hành' },
      { name: 'Gỏi Cuốn Tôm Thịt', price: 15000, notes: 'Không hành hẹ' },
      { name: 'Bánh Mì Thịt Nướng', price: 35000, notes: 'Không ớt' },
      { name: 'Cà Phê Sữa Đá', price: 30000, notes: 'Ít sữa nhiều đá' },
      { name: 'Trà Đào Đá Xay', price: 45000, notes: 'Thêm thạch đào' },
      { name: 'Phở Gà Trứng Non', price: 60000, notes: 'Không da gà' }
    ];

    const randomTable = tableNo || Math.floor(1 + Math.random() * 15);
    const orderId = Math.floor(1000 + Math.random() * 9000);
    
    // Chọn ngẫu nhiên 1-3 món
    const itemsCount = Math.floor(1 + Math.random() * 3);
    const items: OrderDetailItem[] = [];
    const kitchenItems: KitchenItem[] = [];

    let total = 0;
    for (let i = 0; i < itemsCount; i++) {
      const dish = randomDishes[Math.floor(Math.random() * randomDishes.length)];
      const itemId = Math.floor(10000 + Math.random() * 90000);
      const qty = Math.floor(1 + Math.random() * 2);
      
      items.push({
        id: itemId,
        menuItemId: i + 1,
        name: dish.name,
        unitPrice: dish.price,
        quantity: qty,
        notes: Math.random() > 0.4 ? dish.notes : undefined,
        status: 'WAITING'
      });

      kitchenItems.push({
        id: itemId,
        orderId: orderId,
        tableNumber: randomTable,
        name: dish.name,
        quantity: qty,
        notes: Math.random() > 0.4 ? dish.notes : undefined,
        status: 'WAITING',
        orderedAt: new Date().toISOString(),
        elapsedSeconds: 0
      });

      total += dish.price * qty;
    }

    const newOrder: OrderResponse = {
      id: orderId,
      tableNumber: randomTable,
      customerName: `Khách Bàn ${randomTable}`,
      totalAmount: total,
      discountAmount: 0,
      finalAmount: total,
      status: 'PENDING',
      createdAt: new Date().toISOString(),
      items: items
    };

    mockActiveOrders = [newOrder, ...mockActiveOrders];
    return kitchenItems;
  },

  // In hóa đơn món ăn cho bếp chế biến
  printKitchenTicket: (item: { orderId: number; tableNumber: number; name: string; quantity: number; notes?: string; orderedAt: string }) => {
    const printWindow = window.open('', '_blank');
    if (!printWindow) return;

    printWindow.document.write(`
      <html>
        <head>
          <title>Phiếu Chế Biến - Bàn ${item.tableNumber}</title>
          <style>
            @page { size: 80mm auto; margin: 0; }
            body {
              font-family: 'Courier New', Courier, monospace;
              width: 72mm;
              margin: 0 auto;
              padding: 5mm 0;
              font-size: 13px;
              color: #000;
              line-height: 1.3;
            }
            .center { text-align: center; }
            .bold { font-weight: bold; }
            .header { border-bottom: 1px dashed #000; padding-bottom: 4mm; margin-bottom: 4mm; }
            .title { font-size: 16px; text-transform: uppercase; }
            .info-row { display: flex; justify-content: space-between; margin-bottom: 1mm; }
            .dish-card {
              border: 1px solid #000;
              padding: 3mm;
              margin: 3mm 0;
              border-radius: 4px;
            }
            .dish-name { font-size: 18px; text-transform: uppercase; }
            .notes {
              background: #f2f2f2;
              border-left: 3px solid #000;
              padding: 1.5mm 3mm;
              margin-top: 3mm;
              font-size: 12px;
            }
            .footer { border-top: 1px dashed #000; padding-top: 3mm; margin-top: 5mm; font-size: 10px; }
          </style>
        </head>
        <body>
          <div class="header center">
            <div class="title bold">PHIẾU CHẾ BIẾN BẾP</div>
            <div style="font-size: 11px; margin-top: 1mm;">Mã Order: #${item.orderId}</div>
          </div>
          
          <div class="info-row">
            <span class="bold" style="font-size: 16px;">BÀN: ${item.tableNumber}</span>
            <span>Giờ: ${new Date(item.orderedAt).toLocaleTimeString('vi-VN')}</span>
          </div>
          <div class="info-row" style="font-size: 11px; color: #555; margin-bottom: 3mm;">
            <span>Ngày: ${new Date(item.orderedAt).toLocaleDateString('vi-VN')}</span>
          </div>

          <div class="dish-card">
            <div class="info-row">
              <span class="dish-name bold">${item.name}</span>
              <span class="bold" style="font-size: 20px;">SL: ${item.quantity}</span>
            </div>
            ${item.notes ? `<div class="notes bold">📝 Chú thích: ${item.notes}</div>` : ''}
          </div>

          <div class="footer center">
            <div>SmartDine - In lúc: ${new Date().toLocaleTimeString('vi-VN')}</div>
            <div class="bold" style="margin-top: 1mm;">VUI LÒNG LÀM THEO THỨ TỰ HÀNG CHỜ!</div>
          </div>

          <script>
            window.onload = function() {
              window.print();
              window.close();
            }
          </script>
        </body>
      </html>
    `);
    printWindow.document.close();
  },

  // In hóa đơn thanh toán cho bàn
  printCheckoutBill: (tableNumber: number, orders: OrderResponse[]) => {
    const tableOrders = orders.filter((o) => o.tableNumber === tableNumber);
    if (tableOrders.length === 0) return;

    // Gom tất cả món ăn từ các order của bàn này
    const itemMap: { [key: string]: { name: string; unitPrice: number; quantity: number; total: number } } = {};
    let subtotal = 0;
    let totalDiscount = 0;

    tableOrders.forEach((o) => {
      totalDiscount += o.discountAmount;
      o.items.forEach((item) => {
        const key = item.name + '_' + item.unitPrice;
        if (itemMap[key]) {
          itemMap[key].quantity += item.quantity;
          itemMap[key].total += item.unitPrice * item.quantity;
        } else {
          itemMap[key] = {
            name: item.name,
            unitPrice: item.unitPrice,
            quantity: item.quantity,
            total: item.unitPrice * item.quantity
          };
        }
        subtotal += item.unitPrice * item.quantity;
      });
    });

    const itemsList = Object.values(itemMap);
    const finalAmount = subtotal - totalDiscount;
    const printWindow = window.open('', '_blank');
    if (!printWindow) return;

    printWindow.document.write(`
      <html>
        <head>
          <title>Hóa Đơn Thanh Toán - Bàn ${tableNumber}</title>
          <style>
            @page { size: 80mm auto; margin: 0; }
            body {
              font-family: 'Courier New', Courier, monospace;
              width: 72mm;
              margin: 0 auto;
              padding: 8mm 0;
              font-size: 12px;
              color: #000;
              line-height: 1.4;
            }
            .center { text-align: center; }
            .right { text-align: right; }
            .bold { font-weight: bold; }
            .header { border-bottom: 1px dashed #000; padding-bottom: 4mm; margin-bottom: 4mm; }
            .logo { font-size: 20px; font-weight: bold; letter-spacing: 1px; }
            .subtitle { font-size: 11px; margin-top: 1mm; color: #555; }
            .info-block { margin-bottom: 4mm; border-bottom: 1px solid #eee; padding-bottom: 2mm; }
            .info-row { display: flex; justify-content: space-between; margin-bottom: 1mm; }
            .item-table { width: 100%; border-collapse: collapse; margin-top: 2mm; margin-bottom: 4mm; }
            .item-table th { border-bottom: 1px solid #000; text-align: left; padding: 1mm 0; font-size: 11px; }
            .item-table td { padding: 1.5mm 0; font-size: 11px; vertical-align: top; }
            .summary-block { border-top: 1px dashed #000; padding-top: 3mm; margin-top: 2mm; }
            .total-row { display: flex; justify-content: space-between; font-size: 14px; margin-top: 1mm; }
            .footer { border-top: 1px dashed #000; padding-top: 4mm; margin-top: 6mm; font-size: 10px; }
          </style>
        </head>
        <body>
          <div class="header center">
            <div class="logo">SMARTDINE</div>
            <div class="subtitle">Hệ Thống Nhà Hàng Thông Minh</div>
            <div class="subtitle">Hotline: 1900 6868 - SmartDine.vn</div>
          </div>

          <div class="info-block">
            <div class="info-row">
              <span class="bold" style="font-size: 15px;">HÓA ĐƠN THANH TOÁN</span>
              <span class="bold" style="font-size: 15px;">BÀN: ${tableNumber}</span>
            </div>
            <div class="info-row">
              <span>Thu ngân: Staff Sarah</span>
              <span>Giờ: ${new Date().toLocaleTimeString('vi-VN')}</span>
            </div>
            <div class="info-row">
              <span>Ngày: ${new Date().toLocaleDateString('vi-VN')}</span>
              <span>Số phiếu: #BILL-${tableNumber}-${Math.floor(100 + Math.random() * 900)}</span>
            </div>
          </div>

          <table class="item-table">
            <thead>
              <tr>
                <th width="50%">TÊN MÓN</th>
                <th width="15%" class="right">SL</th>
                <th width="35%" class="right">Đ.GIÁ</th>
              </tr>
            </thead>
            <tbody>
              ${itemsList.map((item) => `
                <tr>
                  <td>
                    <div class="bold">${item.name}</div>
                  </td>
                  <td class="right">${item.quantity}</td>
                  <td class="right">${item.unitPrice.toLocaleString('vi-VN')}đ</td>
                </tr>
              `).join('')}
            </tbody>
          </table>

          <div class="summary-block">
            <div class="info-row">
              <span>Tạm tính:</span>
              <span class="bold">${subtotal.toLocaleString('vi-VN')}đ</span>
            </div>
            ${totalDiscount > 0 ? `
              <div class="info-row" style="color: #ff4d4f;">
                <span>Giảm giá:</span>
                <span class="bold">-${totalDiscount.toLocaleString('vi-VN')}đ</span>
              </div>
            ` : ''}
            <div class="total-row bold">
              <span>TỔNG CỘNG:</span>
              <span style="font-size: 16px;">${finalAmount.toLocaleString('vi-VN')}đ</span>
            </div>
          </div>

          <div class="footer center">
            <div class="bold">CẢM ƠN QUÝ KHÁCH & HẸN GẶP LẠI!</div>
            <div style="margin-top: 1mm; color: #555;">Wifi: SmartDine_Free / Pass: smartdine123</div>
            <div style="margin-top: 1.5mm; font-size: 8px; color: #aaa;">Powered by SmartDine REST-API Dashboard</div>
          </div>

          <script>
            window.onload = function() {
              window.print();
              window.close();
            }
          </script>
        </body>
      </html>
    `);
    printWindow.document.close();
  }
};
