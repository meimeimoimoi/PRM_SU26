// Khớp với BE: SmartDine.Application.DTOs.Staff.StaffDtos (StaffController chỉ MANAGER truy cập).
export type StaffRole = 'STAFF' | 'CHEF' | 'MANAGER';

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
