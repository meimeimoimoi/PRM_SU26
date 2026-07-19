import { apiClient } from './api/client';
import { Location } from '@/types/location';

export const locationService = {
  // GET /api/v1/locations (Manager)
  getAll: async (): Promise<Location[]> => {
    const response = await apiClient.get<any>('/locations');
    return response.data.data || response.data || [];
  },

  // POST /api/v1/locations (Manager)
  create: async (name: string): Promise<Location> => {
    const response = await apiClient.post<any>('/locations', { name });
    return response.data.data || response.data;
  }
};
