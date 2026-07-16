import { apiClient } from './api/client';
import { Table, TableStatus } from '@/types/table';

export const tableService = {
  // Lấy danh sách tất cả bàn
  getAllTables: async (): Promise<Table[]> => {
    const response = await apiClient.get<any>('/tables');
    return (response.data.data || response.data || []) as Table[];
  },

  // Thêm bàn mới
  createTable: async (tableNumber: number, capacity: number, locationId?: number): Promise<Table> => {
    const response = await apiClient.post<any>('/tables', { tableNumber, capacity, locationId });
    return (response.data.data || response.data) as Table;
  },

  // Cập nhật trạng thái bàn (AVAILABLE / OCCUPIED)
  updateTableStatus: async (id: number, status: TableStatus): Promise<Table> => {
    const response = await apiClient.patch<any>(`/tables/${id}/status`, { status });
    return (response.data.data || response.data) as Table;
  },

  // Cập nhật thông tin cơ bản của bàn — chỉ Capacity + Location, không đổi được TableNumber
  // (QR đã in mã hóa theo số bàn, đổi số sẽ làm QR sai — xem TablesController PATCH /tables/{id}).
  updateTable: async (id: number, data: { capacity?: number; locationId?: number }): Promise<Table> => {
    const response = await apiClient.patch<any>(`/tables/${id}`, data);
    return (response.data.data || response.data) as Table;
  },

  // Xóa (soft-delete) bàn — BE từ chối nếu bàn đang OCCUPIED.
  deleteTable: async (id: number): Promise<void> => {
    await apiClient.delete(`/tables/${id}`);
  }
};
