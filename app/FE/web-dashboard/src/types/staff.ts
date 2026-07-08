export interface StaffMember {
  id: string; // e.g. S-101
  fullName: string;
  email: string;
  phone: string;
  role: 'Admin' | 'Chef' | 'Staff';
  isActive: boolean;
}
