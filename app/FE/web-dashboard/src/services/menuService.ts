import { apiClient } from './api/client';
import { MenuItemResponse, MenuItemPatchResponse, CreateMenuItemRequest, UpdateMenuItemRequest } from '@/types/menu';

export const menuService = {
  // Lấy toàn bộ món ăn
  getAllMenuItems: async (search?: string): Promise<MenuItemResponse[]> => {
    const response = await apiClient.get<any>('/menu-items', {
      params: { search }
    });
    return response.data.data || response.data;
  },

  // Tạo món mới
  createMenuItem: async (request: CreateMenuItemRequest): Promise<MenuItemResponse> => {
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
  }
};
