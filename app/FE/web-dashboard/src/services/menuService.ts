import { apiClient } from './api/client';
import { MenuItemResponse, MenuItemPatchResponse, MenuItemCreatedResponse, CreateMenuItemRequest, UpdateMenuItemRequest } from '@/types/menu';

export const menuService = {
  // Lấy toàn bộ món ăn (mọi trang) — BE mặc định phân trang limit=10/request, admin cần
  // thấy hết cả menu nên gọi lặp cho tới khi hết totalPages, gộp lại thành 1 mảng.
  // includeUnavailable=true để admin thấy cả món đã tắt "còn hàng" (BE chỉ tôn trọng cờ
  // này với role STAFF/CHEF/MANAGER, khách hàng gửi lên cũng bị bỏ qua).
  getAllMenuItems: async (search?: string): Promise<MenuItemResponse[]> => {
    const items: MenuItemResponse[] = [];
    let page = 1;
    let totalPages = 1;

    do {
      const response = await apiClient.get<any>('/menu-items', {
        params: { search, page, limit: 50, includeUnavailable: true }
      });
      items.push(...(response.data.data || []));
      totalPages = response.data.pagination?.totalPages || 1;
      page += 1;
    } while (page <= totalPages);

    return items;
  },

  // Tạo món mới — BE chỉ trả về { id, name, createdAt }, KHÔNG phải MenuItemResponse đầy đủ.
  createMenuItem: async (request: CreateMenuItemRequest): Promise<MenuItemCreatedResponse> => {
    const response = await apiClient.post<any>('/menu-items', request);
    return response.data.data || response.data;
  },

  // Cập nhật món — BE chỉ có PATCH /menu-items/{id} (partial update), không có PUT.
  updateMenuItem: async (id: number, request: UpdateMenuItemRequest): Promise<MenuItemPatchResponse> => {
    const response = await apiClient.patch<any>(`/menu-items/${id}`, request);
    return response.data.data || response.data;
  },

  // Xóa món
  deleteMenuItem: async (id: number): Promise<void> => {
    await apiClient.delete(`/menu-items/${id}`);
  },

  // Thay đổi trạng thái có hàng / hết hàng — BE không có endpoint /availability riêng,
  // dùng chung PATCH /menu-items/{id} với body { isAvailable }.
  toggleAvailability: async (id: number, isAvailable: boolean): Promise<MenuItemPatchResponse> => {
    const response = await apiClient.patch<any>(`/menu-items/${id}`, { isAvailable });
    return response.data.data || response.data;
  },

  // Upload ảnh món ăn — trả về URL để gán vào field imageUrl, thay cho việc dán link tay.
  // Không tự set Content-Type: để axios/trình duyệt tự sinh multipart boundary, set tay
  // "multipart/form-data" thiếu boundary sẽ khiến BE parse form thất bại.
  uploadImage: async (file: File): Promise<string> => {
    const formData = new FormData();
    formData.append('file', file);
    const response = await apiClient.post<any>('/menu-items/upload-image', formData);
    const data = response.data.data || response.data;
    return data.imageUrl;
  }
};
