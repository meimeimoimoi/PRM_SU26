// Khớp với BE: SmartDine.Application.DTOs.Settings (SettingsController — chỉ MANAGER truy cập).
// Bản ghi cấu hình duy nhất (singleton) trong hệ thống.
export interface RestaurantSettings {
  id: number;
  restaurantName: string;
  address?: string;
  phone?: string;
  openingTime: string; // "HH:mm"
  closingTime: string; // "HH:mm"
  taxRate: number; // 0-100 (%)
  serviceChargeRate: number; // 0-100 (%)
  updatedAt?: string;
}

// PATCH /api/v1/settings là partial update — mọi field optional.
export interface UpdateSettingsRequest {
  restaurantName?: string;
  address?: string;
  phone?: string;
  openingTime?: string;
  closingTime?: string;
  taxRate?: number;
  serviceChargeRate?: number;
}
