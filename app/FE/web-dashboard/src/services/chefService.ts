import { apiClient } from './api/client';
import { KitchenItem, KitchenItemStatus } from '@/types/chef';

export const chefService = {
  // Lấy danh sách các món ăn cần chế biến (OrderDetail)
  getKitchenItems: async (): Promise<KitchenItem[]> => {
    try {
      const response = await apiClient.get<any>('/kitchen/items');
      return response.data.data || response.data;
    } catch (error) {
      console.warn('API /kitchen/items chưa sẵn sàng, sử dụng dữ liệu mẫu.');
      // Trả về dữ liệu mẫu khớp với Entity trong Backend
      return [
        {
          id: 1001,
          orderId: 8921,
          tableNumber: 5,
          name: 'Phở Bò Đặc Biệt',
          quantity: 2,
          notes: 'Không hành, nhiều bánh phở',
          status: 'WAITING',
          orderedAt: new Date(Date.now() - 8 * 60 * 1000).toISOString(),
          elapsedSeconds: 480
        },
        {
          id: 1002,
          orderId: 8921,
          tableNumber: 5,
          name: 'Gỏi Cuốn Tôm Thịt',
          quantity: 1,
          status: 'WAITING',
          orderedAt: new Date(Date.now() - 8 * 60 * 1000).toISOString(),
          elapsedSeconds: 480
        },
        {
          id: 1003,
          orderId: 8922,
          tableNumber: 12,
          name: 'Bánh Mì Thịt Nướng',
          quantity: 3,
          notes: 'Cắt đôi bánh mì',
          status: 'DOING',
          orderedAt: new Date(Date.now() - 15 * 60 * 1000).toISOString(),
          elapsedSeconds: 900
        },
        {
          id: 1004,
          orderId: 8923,
          tableNumber: 3,
          name: 'Cà Phê Sữa Đá',
          quantity: 1,
          status: 'DONE',
          orderedAt: new Date(Date.now() - 18 * 60 * 1000).toISOString(),
          elapsedSeconds: 1080
        }
      ];
    }
  },

  // Cập nhật trạng thái món ăn (ví dụ: chuyển từ WAITING -> DOING -> DONE -> SERVED)
  updateItemStatus: async (id: number, status: KitchenItemStatus): Promise<KitchenItem> => {
    try {
      const response = await apiClient.patch<any>(`/kitchen/items/${id}/status`, { status });
      return response.data.data || response.data;
    } catch (error) {
      console.warn(`API cập nhật trạng thái lỗi, thực hiện cập nhật offline cho ${id} -> ${status}`);
      throw error;
    }
  }
};
