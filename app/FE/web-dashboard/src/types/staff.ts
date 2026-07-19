// Khớp với BE: SmartDine.Application.DTOs.Staff.StaffDtos (StaffController chỉ MANAGER truy cập).
// Dashboard chỉ tạo/gán được 2 role: STAFF (order + bếp + thanh toán) và MANAGER.
export type StaffRole = 'STAFF' | 'MANAGER';

export interface StaffMember {
  id: number;
  fullName: string;
  email: string;
  role: StaffRole;
  isActive: boolean;
  createdAt: string;
}

export interface CreateStaffRequest {
  fullName: string;
  email: string;
  password: string;
  role: StaffRole;
}

// PATCH /api/v1/staff/{id} là partial update — mọi field optional, BE không hỗ trợ đổi mật khẩu qua đây.
export interface UpdateStaffRequest {
  fullName?: string;
  email?: string;
  role?: StaffRole;
  isActive?: boolean;
}
