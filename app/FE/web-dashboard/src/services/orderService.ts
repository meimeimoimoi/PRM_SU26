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

export interface ChartPoint {
  label: string;
  value: number;
}

export type ChartPeriod = 'day' | 'week' | 'month';

export const orderService = {
  // Doanh số theo đơn hàng (không lọc thanh toán) — cho chart "Doanh số đơn hàng" ở Dashboard.
  getOrderChart: async (period: ChartPeriod): Promise<ChartPoint[]> => {
    const response = await apiClient.get<any>('/orders/chart', { params: { period } });
    return response.data.data || response.data || [];
  },

  // Doanh thu thực nhận (chỉ payment SUCCESS) — cho chart "Doanh thu thực nhận" ở Dashboard.
  getRevenueChart: async (period: ChartPeriod): Promise<ChartPoint[]> => {
    const response = await apiClient.get<any>('/payments/chart', { params: { period } });
    return response.data.data || response.data || [];
  },

  getActiveOrders: async (): Promise<OrderResponse[]> => {
    const response = await apiClient.get<any>('/orders/active');
    const data = response.data.data || response.data;
    return Array.isArray(data) ? data : [];
  },

  getTodayOrders: async (): Promise<OrderResponse[]> => {
    const response = await apiClient.get<any>('/orders/today');
    const data = response.data.data || response.data;
    return Array.isArray(data) ? data : [];
  },

  getKitchenItems: async (): Promise<KitchenItem[]> => {
    const orders = await orderService.getActiveOrders();
    const items: KitchenItem[] = [];

    orders.forEach((order) => {
      order.items.forEach((item) => {
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

    return items.sort((a, b) => new Date(a.orderedAt).getTime() - new Date(b.orderedAt).getTime());
  },

  updateItemStatus: async (itemId: number, newStatus: KitchenItemStatus): Promise<boolean> => {
    await apiClient.patch(`/orders/items/${itemId}/status`, { status: newStatus });
    return true;
  },

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
  },

  completePaymentByTable: async (tableNumber: number): Promise<boolean> => {
    await apiClient.post(`/payments/complete-by-table/${tableNumber}`);
    return true;
  }
};
