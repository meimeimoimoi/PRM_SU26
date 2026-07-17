import { apiClient } from './api/client';
import { StaffMember, CreateStaffRequest, UpdateStaffRequest } from '@/types/staff';

export const staffService = {
  // GET /api/v1/staff?role=&isActive=&page=&pageSize=
  getAll: async (): Promise<StaffMember[]> => {
    const response = await apiClient.get<any>('/staff', { params: { pageSize: 100 } });
    const data = response.data.data || response.data;
    return Array.isArray(data) ? data : (data.items || []);
  },

  // POST /api/v1/staff — Manager đặt mật khẩu trực tiếp cho nhân viên mới.
  create: async (request: CreateStaffRequest): Promise<StaffMember> => {
    const response = await apiClient.post<any>('/staff', request);
    return response.data.data || response.data;
  },

  // PATCH /api/v1/staff/{id} — partial update, không đổi được mật khẩu qua đây.
  update: async (id: number, request: UpdateStaffRequest): Promise<StaffMember> => {
    const response = await apiClient.patch<any>(`/staff/${id}`, request);
    return response.data.data || response.data;
  },

  // DELETE /api/v1/staff/{id} — vô hiệu hóa (soft), không xóa vĩnh viễn. Manager không thể tự vô hiệu hóa chính mình.
  deactivate: async (id: number): Promise<void> => {
    await apiClient.delete(`/staff/${id}`);
  }
};
