import { apiClient } from './api/client';
import { RestaurantSettings, UpdateSettingsRequest } from '@/types/settings';

export const settingsService = {
  getSettings: async (): Promise<RestaurantSettings> => {
    const response = await apiClient.get<any>('/settings');
    return response.data.data || response.data;
  },

  updateSettings: async (request: UpdateSettingsRequest): Promise<RestaurantSettings> => {
    const response = await apiClient.patch<any>('/settings', request);
    return response.data.data || response.data;
  }
};
