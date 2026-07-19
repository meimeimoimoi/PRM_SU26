import { apiClient } from './api/client';

/**
 * Gọi AI.API (qua gateway /api/v1/ai/query) — service phân loại ý định bằng Ollama rồi
 * ghép dữ liệu thật (bàn/doanh thu/món bán chạy) trước khi trả lời. Chỉ MANAGER được phép
 * gọi (xem [Authorize(Roles = Roles.Manager)] trên AiController.Query).
 */
export const aiService = {
  async ask(prompt: string): Promise<string> {
    const response = await apiClient.post<any>('/ai/query', { prompt });
    const data = response.data?.data ?? response.data ?? {};
    return data.answer || '';
  },
};
