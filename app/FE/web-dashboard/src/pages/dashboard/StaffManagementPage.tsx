import React, { useEffect, useState, useMemo, useCallback } from 'react';
import { 
  Search, 
  Plus, 
  X, 
  Edit3, 
  UserX, 
  Shield, 
  ShieldCheck, 
  Mail, 
  Calendar,
  Filter,
  MoreVertical,
  ChevronRight,
  UserPlus
} from 'lucide-react';
import { StaffMember, StaffRole } from '@/types/staff';
import { staffService } from '@/services/staffService';
import { getErrorMessage } from '@/utils/apiError';
import { toast } from 'react-hot-toast'; // Assuming toast is available or I'll use a simple alert if not sure

const roleLabel: Record<StaffRole, string> = { 
  MANAGER: 'Quản lý', 
  STAFF: 'Nhân viên' 
};

const StaffManagementPage: React.FC = () => {
  const [staff, setStaff] = useState<StaffMember[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [roleFilter, setRoleFilter] = useState<StaffRole | 'ALL'>('ALL');
  const [showAdd, setShowAdd] = useState(false);
  const [editing, setEditing] = useState<StaffMember | null>(null);
  const [saving, setSaving] = useState(false);

  // Form states
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [role, setRole] = useState<StaffRole>('STAFF');

  const fetchAll = useCallback(async () => {
    setLoading(true);
    try {
      const data = await staffService.getAll();
      setStaff(Array.isArray(data) ? data : []);
    } catch (err) {
      console.error(err);
      setStaff([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchAll();
  }, [fetchAll]);

  const resetForm = () => {
    setFullName('');
    setEmail('');
    setPassword('');
    setRole('STAFF');
  };

  const handleAdd = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!fullName.trim() || !email.trim() || !password) return;
    setSaving(true);
    try {
      const created = await staffService.create({ fullName, email, password, role });
      setStaff((prev) => [...prev, created]);
      setShowAdd(false);
      resetForm();
      // Using alert as a fallback if toast isn't globally configured
      alert('Thêm nhân viên thành công!');
    } catch (err) {
      alert(getErrorMessage(err, 'Thêm nhân viên thất bại'));
    } finally {
      setSaving(false);
    }
  };

  const handleEdit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editing) return;
    setSaving(true);
    try {
      const updated = await staffService.update(editing.id, { 
        fullName, 
        email, 
        role, 
        isActive: editing.isActive 
      });
      setStaff((prev) => prev.map((s) => (s.id === editing.id ? updated : s)));
      setEditing(null);
      alert('Cập nhật thông tin thành công!');
    } catch (err) {
      alert(getErrorMessage(err, 'Cập nhật thất bại'));
    } finally {
      setSaving(false);
    }
  };

  const handleDeactivate = async (id: number, name: string) => {
    if (!confirm(`Bạn có chắc chắn muốn vô hiệu hóa nhân viên ${name}?`)) return;
    try {
      await staffService.deactivate(id);
      setStaff((prev) => prev.map((s) => s.id === id ? { ...s, isActive: false } : s));
      alert('Đã vô hiệu hóa nhân viên');
    } catch (err) {
      alert(getErrorMessage(err, 'Vô hiệu hóa thất bại'));
    }
  };

  const openEdit = (s: StaffMember) => {
    setEditing(s);
    setFullName(s.fullName);
    setEmail(s.email);
    setRole(s.role);
  };

  const initials = (name: string) =>
    name.split(' ').map(n => n[0]).join('').slice(0, 2).toUpperCase();

  const filtered = useMemo(() => staff.filter((s) => {
    const q = search.toLowerCase();
    const ms = !search || s.fullName.toLowerCase().includes(q) || s.email.toLowerCase().includes(q);
    const mr = roleFilter === 'ALL' || s.role === roleFilter;
    return ms && mr;
  }), [staff, search, roleFilter]);

  const statusInfo = (isActive: boolean) => ({
    label: isActive ? 'Đang hoạt động' : 'Đã khóa',
    color: isActive ? 'text-green-500' : 'text-slate-400',
    bg: isActive ? 'bg-green-500/10' : 'bg-slate-500/10',
    dot: isActive ? 'bg-green-500' : 'bg-slate-400'
  });

  return (
    <div className="p-4 md:p-8 max-w-7xl mx-auto space-y-8 animate-in fade-in duration-500">
      {/* Header Section */}
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-6">
        <div>
          <h1 className="text-3xl font-bold text-slate-900 tracking-tight flex items-center gap-3">
            <ShieldCheck className="text-blue-600 w-8 h-8" />
            Quản lý Nhân viên
          </h1>
          <p className="mt-2 text-slate-500 font-medium">
            Thiết lập quyền truy cập, vai trò và thông tin đội ngũ nhân sự.
          </p>
        </div>
        
        <button 
          onClick={() => { resetForm(); setShowAdd(true); }}
          className="flex items-center justify-center gap-2 px-6 py-3 rounded-2xl bg-blue-600 text-white font-semibold shadow-lg shadow-blue-600/20 hover:bg-blue-700 hover:-translate-y-0.5 transition-all active:scale-95 group"
        >
          <UserPlus size={20} className="group-hover:rotate-12 transition-transform" />
          <span>Thêm nhân viên</span>
        </button>
      </div>

      {/* Stats Quick View (Pro UI touch) */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="glass-card p-4 rounded-3xl border border-white/40 bg-white/50 backdrop-blur-md shadow-sm">
          <div className="text-xs font-bold text-slate-400 uppercase tracking-wider mb-1">Tổng nhân sự</div>
          <div className="text-2xl font-bold text-slate-800">{staff.length}</div>
        </div>
        <div className="glass-card p-4 rounded-3xl border border-white/40 bg-white/50 backdrop-blur-md shadow-sm">
          <div className="text-xs font-bold text-slate-400 uppercase tracking-wider mb-1">Đang hoạt động</div>
          <div className="text-2xl font-bold text-green-600">{staff.filter(s => s.isActive).length}</div>
        </div>
        <div className="glass-card p-4 rounded-3xl border border-white/40 bg-white/50 backdrop-blur-md shadow-sm">
          <div className="text-xs font-bold text-slate-400 uppercase tracking-wider mb-1">Quản lý</div>
          <div className="text-2xl font-bold text-purple-600">{staff.filter(s => s.role === 'MANAGER').length}</div>
        </div>
      </div>

      {/* Filters & Search */}
      <div className="flex flex-col md:flex-row gap-4 items-center bg-white/80 p-3 rounded-2xl border border-slate-100 shadow-sm">
        <div className="relative flex-1 w-full">
          <Search className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-400" size={18} />
          <input 
            value={search} 
            onChange={(e) => setSearch(e.target.value)} 
            placeholder="Tìm theo tên hoặc email..."
            className="w-full pl-12 pr-4 py-3 bg-slate-50 border-none rounded-xl text-slate-700 placeholder:text-slate-400 focus:ring-2 focus:ring-blue-500/20 transition-all outline-none"
          />
        </div>
        
        <div className="flex items-center gap-2 w-full md:w-auto">
          <div className="p-3 bg-slate-50 rounded-xl text-slate-400">
            <Filter size={18} />
          </div>
          <select 
            value={roleFilter} 
            onChange={(e) => setRoleFilter(e.target.value as StaffRole | 'ALL')}
            className="flex-1 md:w-48 p-3 bg-slate-50 border-none rounded-xl text-slate-600 font-medium focus:ring-2 focus:ring-blue-500/20 outline-none cursor-pointer appearance-none"
          >
            <option value="ALL">Tất cả vai trò</option>
            <option value="MANAGER">Quản lý</option>
            <option value="STAFF">Nhân viên</option>
          </select>
        </div>
      </div>

      {/* Main Content: Staff Table */}
      <div className="bg-white rounded-[2rem] border border-slate-100 shadow-xl shadow-slate-200/50 overflow-hidden">
        {loading ? (
          <div className="flex flex-col items-center justify-center py-20 space-y-4">
            <div className="w-10 h-10 border-4 border-blue-600/20 border-t-blue-600 rounded-full animate-spin" />
            <p className="text-slate-400 font-medium">Đang tải dữ liệu...</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="bg-slate-50/50 border-b border-slate-100">
                  <th className="py-5 px-6 text-xs font-bold text-slate-400 uppercase tracking-widest">Thành viên</th>
                  <th className="py-5 px-6 text-xs font-bold text-slate-400 uppercase tracking-widest hidden lg:table-cell">Liên hệ</th>
                  <th className="py-5 px-6 text-xs font-bold text-slate-400 uppercase tracking-widest text-center">Vai trò</th>
                  <th className="py-5 px-6 text-xs font-bold text-slate-400 uppercase tracking-widest text-center">Trạng thái</th>
                  <th className="py-5 px-6 text-xs font-bold text-slate-400 uppercase tracking-widest text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-50">
                {filtered.map((s) => {
                  const status = statusInfo(s.isActive);
                  return (
                    <tr key={s.id} className="group hover:bg-slate-50/80 transition-colors">
                      <td className="py-5 px-6">
                        <div className="flex items-center gap-4">
                          <div className="w-12 h-12 rounded-2xl bg-gradient-to-br from-blue-50 to-blue-100 flex items-center justify-center text-blue-600 font-bold text-lg border border-blue-200/50 group-hover:scale-105 transition-transform shadow-sm">
                            {initials(s.fullName)}
                          </div>
                          <div>
                            <div className="font-bold text-slate-800">{s.fullName}</div>
                            <div className="text-[11px] text-slate-400 font-mono mt-0.5">ID: #{s.id}</div>
                          </div>
                        </div>
                      </td>
                      <td className="py-5 px-6 hidden lg:table-cell">
                        <div className="flex flex-col gap-1">
                          <div className="flex items-center gap-2 text-sm text-slate-600">
                            <Mail size={14} className="text-slate-300" />
                            {s.email}
                          </div>
                          <div className="flex items-center gap-2 text-[11px] text-slate-400">
                            <Calendar size={12} />
                            Gia nhập: {new Date(s.createdAt).toLocaleDateString('vi-VN')}
                          </div>
                        </div>
                      </td>
                      <td className="py-5 px-6 text-center">
                        <span className={`inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs font-bold ${
                          s.role === 'MANAGER' 
                            ? 'text-purple-600 bg-purple-50 border border-purple-100' 
                            : 'text-blue-600 bg-blue-50 border border-blue-100'
                        }`}>
                          {s.role === 'MANAGER' ? <ShieldCheck size={14} /> : <Shield size={14} />}
                          {roleLabel[s.role]}
                        </span>
                      </td>
                      <td className="py-5 px-6 text-center">
                        <div className={`inline-flex items-center gap-2 px-3 py-1.5 rounded-full text-[11px] font-bold ${status.bg} ${status.color}`}>
                          <span className={`w-1.5 h-1.5 rounded-full ${status.dot} animate-pulse`} />
                          {status.label}
                        </div>
                      </td>
                      <td className="py-5 px-6 text-right">
                        <div className="flex items-center justify-end gap-2 opacity-0 group-hover:opacity-100 transition-opacity">
                          <button 
                            onClick={() => openEdit(s)}
                            className="p-2.5 rounded-xl bg-white text-slate-600 hover:text-blue-600 hover:bg-blue-50 border border-slate-200 hover:border-blue-200 transition-all shadow-sm"
                          >
                            <Edit3 size={16} />
                          </button>
                          {s.isActive && (
                            <button 
                              onClick={() => handleDeactivate(s.id, s.fullName)}
                              className="p-2.5 rounded-xl bg-white text-slate-600 hover:text-red-600 hover:bg-red-50 border border-slate-200 hover:border-red-200 transition-all shadow-sm"
                            >
                              <UserX size={16} />
                            </button>
                          )}
                        </div>
                        <div className="group-hover:hidden text-slate-300">
                          <MoreVertical size={20} className="ml-auto" />
                        </div>
                      </td>
                    </tr>
                  );
                })}
                {filtered.length === 0 && (
                  <tr>
                    <td colSpan={5} className="py-20 text-center">
                      <div className="flex flex-col items-center gap-3">
                        <div className="w-16 h-16 bg-slate-50 rounded-full flex items-center justify-center text-slate-200">
                          <Search size={32} />
                        </div>
                        <p className="text-slate-400 font-medium">Không tìm thấy nhân viên nào</p>
                      </div>
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Add / Edit Modal Overlay */}
      {(showAdd || editing) && (
        <div className="fixed inset-0 z-[1000] flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm animate-in fade-in duration-200" onClick={() => { setShowAdd(false); setEditing(null); }}>
          <div className="bg-white w-full max-w-md rounded-[2.5rem] p-8 shadow-2xl animate-in zoom-in-95 slide-in-from-bottom-10 duration-300" onClick={(e) => e.stopPropagation()}>
            <div className="flex items-center justify-between mb-8">
              <div>
                <h3 className="text-2xl font-bold text-slate-900">{editing ? 'Sửa thông tin' : 'Thêm nhân sự'}</h3>
                <p className="text-sm text-slate-400 font-medium">Hoàn tất các thông tin bên dưới.</p>
              </div>
              <button 
                onClick={() => { setShowAdd(false); setEditing(null); }} 
                className="p-2 rounded-2xl bg-slate-50 text-slate-400 hover:text-slate-600 hover:bg-slate-100 transition-colors"
              >
                <X size={20} />
              </button>
            </div>

            <form onSubmit={editing ? handleEdit : handleAdd} className="space-y-5">
              <div className="space-y-1.5">
                <label className="text-xs font-bold text-slate-500 uppercase tracking-widest ml-1">Họ và tên</label>
                <input 
                  autoFocus
                  required
                  value={fullName} 
                  onChange={(e) => setFullName(e.target.value)}
                  className="w-full px-5 py-3.5 bg-slate-50 border-2 border-transparent rounded-2xl text-slate-700 placeholder:text-slate-300 focus:bg-white focus:border-blue-500/20 transition-all outline-none" 
                  placeholder="Ví dụ: Nguyễn Văn A" 
                />
              </div>

              <div className="space-y-1.5">
                <label className="text-xs font-bold text-slate-500 uppercase tracking-widest ml-1">Email đăng nhập</label>
                <div className="relative">
                  <Mail className="absolute left-5 top-1/2 -translate-y-1/2 text-slate-300" size={18} />
                  <input 
                    required
                    type="email" 
                    value={email} 
                    onChange={(e) => setEmail(e.target.value)}
                    className="w-full pl-14 pr-5 py-3.5 bg-slate-50 border-2 border-transparent rounded-2xl text-slate-700 placeholder:text-slate-300 focus:bg-white focus:border-blue-500/20 transition-all outline-none" 
                    placeholder="name@smartdine.com" 
                  />
                </div>
              </div>

              {!editing && (
                <div className="space-y-1.5">
                  <label className="text-xs font-bold text-slate-500 uppercase tracking-widest ml-1">Mật khẩu ban đầu</label>
                  <input 
                    required
                    type="password" 
                    value={password} 
                    onChange={(e) => setPassword(e.target.value)}
                    className="w-full px-5 py-3.5 bg-slate-50 border-2 border-transparent rounded-2xl text-slate-700 placeholder:text-slate-300 focus:bg-white focus:border-blue-500/20 transition-all outline-none" 
                    placeholder="Tối thiểu 6 ký tự" 
                  />
                </div>
              )}

              <div className="space-y-1.5">
                <label className="text-xs font-bold text-slate-500 uppercase tracking-widest ml-1">Vai trò</label>
                <div className="grid grid-cols-2 gap-3">
                  <button
                    type="button"
                    onClick={() => setRole('STAFF')}
                    className={`p-4 rounded-2xl border-2 transition-all flex flex-col items-center gap-2 ${
                      role === 'STAFF' ? 'border-blue-500 bg-blue-50 text-blue-600' : 'border-slate-100 bg-slate-50 text-slate-400 grayscale'
                    }`}
                  >
                    <Shield size={24} />
                    <span className="text-xs font-bold uppercase tracking-widest">Nhân viên</span>
                  </button>
                  <button
                    type="button"
                    onClick={() => setRole('MANAGER')}
                    className={`p-4 rounded-2xl border-2 transition-all flex flex-col items-center gap-2 ${
                      role === 'MANAGER' ? 'border-purple-500 bg-purple-50 text-purple-600' : 'border-slate-100 bg-slate-50 text-slate-400 grayscale'
                    }`}
                  >
                    <ShieldCheck size={24} />
                    <span className="text-xs font-bold uppercase tracking-widest">Quản lý</span>
                  </button>
                </div>
              </div>

              <div className="flex gap-4 pt-4">
                <button 
                  type="button"
                  onClick={() => { setShowAdd(false); setEditing(null); }}
                  className="flex-1 py-4 rounded-2xl text-slate-400 font-bold hover:bg-slate-50 transition-colors"
                >
                  Hủy
                </button>
                <button 
                  type="submit" 
                  disabled={saving}
                  className="flex-[2] py-4 rounded-2xl bg-slate-900 text-white font-bold hover:bg-slate-800 shadow-xl shadow-slate-900/20 transition-all active:scale-95 disabled:opacity-50"
                >
                  {saving ? 'Đang lưu...' : (editing ? 'Cập nhật' : 'Thêm ngay')}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default StaffManagementPage;
