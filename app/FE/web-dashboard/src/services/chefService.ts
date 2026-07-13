import { apiClient } from './api/client';
import { KitchenItem, KitchenItemStatus } from '@/types/chef';

export const chefService = {
  getKitchenItems: async (): Promise<KitchenItem[]> => {
    const response = await apiClient.get<any>('/kitchen/items');
    return response.data.data || response.data;
  },

  updateItemStatus: async (id: number, status: KitchenItemStatus): Promise<KitchenItem> => {
    const response = await apiClient.patch<any>(`/kitchen/items/${id}/status`, { status });
    return response.data.data || response.data;
  }
};
