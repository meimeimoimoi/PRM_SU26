import { apiClient } from './api/client';
import { MenuCategoryResponse } from '@/types/menu';

export const categoryService = {
  // GET /api/v1/menu-categories — public, không cần role.
  getAll: async (): Promise<MenuCategoryResponse[]> => {
    const response = await apiClient.get<any>('/menu-categories');
    return response.data.data || response.data || [];
  }
};
