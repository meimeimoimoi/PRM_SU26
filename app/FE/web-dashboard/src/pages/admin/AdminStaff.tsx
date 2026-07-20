import React, { useEffect, useState, useMemo, useCallback } from 'react';
import { 
  Search, 
  X, 
  Edit3, 
  UserX, 
  Shield, 
  ShieldCheck, 
  Mail, 
  Calendar,
  Filter,
  UserPlus,
} from 'lucide-react';
import { StaffMember, StaffRole } from '@/types/staff';
import { staffService } from '@/services/staffService';
import { getErrorMessage } from '@/utils/apiError';

const roleLabel: Record<StaffRole, string> = { 
  MANAGER: 'Manager', 
  STAFF: 'Staff' 
};

function AdminStaff() {
  const [staff, setStaff] = useState<StaffMember[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [roleFilter, setRoleFilter] = useState<StaffRole | 'ALL'>('ALL');
  const [showAdd, setShowAdd] = useState(false);
  const [editing, setEditing] = useState<StaffMember | null>(null);
  const [saving, setSaving] = useState(false);

  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [role, setRole] = useState<StaffRole>('STAFF');

  const fetchAll = useCallback(async () => {
    setLoading(true);
    try { 
      const d = await staffService.getAll(); 
      setStaff(Array.isArray(d) ? d : []); 
    } catch { 
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
    } catch (err) { 
      alert(getErrorMessage(err, 'Add failed')); 
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
    } catch (err) { 
      alert(getErrorMessage(err, 'Update failed')); 
    } finally { 
      setSaving(false); 
    }
  };

  const handleDeactivate = async (id: number, name: string) => {
    if (!confirm(`Deactivate ${name}? They won't be able to log in.`)) return;
    try {
      await staffService.deactivate(id);
      setStaff((prev) => prev.map((s) => s.id === id ? { ...s, isActive: false } : s));
    } catch (err) { 
      alert(getErrorMessage(err, 'Deactivate failed')); 
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
    label: isActive ? 'Active' : 'Inactive',
    color: isActive ? 'text-emerald-600' : 'text-slate-400',
    bg: isActive ? 'bg-emerald-50' : 'bg-slate-100',
  });

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold text-slate-800 font-heading tracking-tight">Staff Management</h2>
          <p className="text-sm text-slate-500 font-body">Manage employee access, roles, and details.</p>
        </div>
        <button 
          onClick={() => { resetForm(); setShowAdd(true); }}
          className="flex items-center justify-center gap-2 px-5 py-2.5 rounded-xl bg-accent text-white text-sm font-semibold hover:bg-accent/80 transition-all cursor-pointer shadow-sm"
        >
          <UserPlus size={16} /> Add Staff Member
        </button>
      </div>

      <div className="flex flex-col sm:flex-row gap-3">
        <div className="flex items-center gap-3 px-4 py-2.5 rounded-xl bg-white border border-slate-200 flex-1 shadow-sm">
          <Search size={16} className="text-slate-400" />
          <input 
            value={search} 
            onChange={(e) => setSearch(e.target.value)} 
            placeholder="Search by name or email..."
            className="bg-transparent border-none outline-none text-sm text-slate-700 placeholder:text-slate-400 w-full font-body" 
          />
          {search && <X size={14} className="text-slate-400 cursor-pointer hover:text-slate-600" onClick={() => setSearch('')} />}
        </div>
        <div className="relative">
          <select 
            value={roleFilter} 
            onChange={(e) => setRoleFilter(e.target.value as StaffRole | 'ALL')}
            className="appearance-none pl-10 pr-10 py-2.5 rounded-xl bg-white border border-slate-200 text-sm text-slate-700 outline-none font-body cursor-pointer hover:bg-slate-50 transition-all min-w-[140px] shadow-sm"
          >
            <option value="ALL">All Roles</option>
            <option value="MANAGER">Manager</option>
            <option value="STAFF">Staff</option>
          </select>
          <Filter size={14} className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-400" />
        </div>
      </div>

      <div className="bg-white rounded-2xl border border-slate-200 overflow-hidden shadow-sm">
        {loading ? (
          <div className="flex flex-col items-center justify-center h-64 gap-3">
            <div className="w-8 h-8 border-2 border-accent/20 border-t-accent rounded-full animate-spin" />
            <span className="text-xs text-slate-400 font-medium">Fetching personnel...</span>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left">
              <thead>
                <tr className="border-b border-slate-200 bg-slate-50">
                  <th className="py-4 px-6 text-[10px] font-bold text-slate-500 uppercase tracking-[2px]">Personnel</th>
                  <th className="py-4 px-6 text-[10px] font-bold text-slate-500 uppercase tracking-[2px] hidden lg:table-cell">Contact</th>
                  <th className="py-4 px-6 text-[10px] font-bold text-slate-500 uppercase tracking-[2px] text-center">Role</th>
                  <th className="py-4 px-6 text-[10px] font-bold text-slate-500 uppercase tracking-[2px] text-center">Status</th>
                  <th className="py-4 px-6 text-[10px] font-bold text-slate-500 uppercase tracking-[2px] text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {filtered.map((s) => {
                  const st = statusInfo(s.isActive);
                  return (
                    <tr key={s.id} className="group hover:bg-slate-50 transition-colors">
                      <td className="py-4 px-6">
                        <div className="flex items-center gap-4">
                          <div className="w-10 h-10 rounded-xl bg-accentlight border border-accent/20 flex items-center justify-center text-accent text-xs font-bold">
                            {initials(s.fullName)}
                          </div>
                          <div>
                            <div className="text-sm font-semibold text-slate-800 group-hover:text-slate-900 transition-colors">{s.fullName}</div>
                            <div className="text-[10px] text-slate-400 font-mono tracking-wider">#{s.id}</div>
                          </div>
                        </div>
                      </td>
                      <td className="py-4 px-6 hidden lg:table-cell">
                        <div className="flex flex-col gap-0.5">
                          <div className="text-xs text-slate-500 flex items-center gap-1.5">
                            <Mail size={12} className="text-slate-400" /> {s.email}
                          </div>
                          <div className="text-[10px] text-slate-400 flex items-center gap-1.5">
                            <Calendar size={12} className="text-slate-400" /> 
                            Joined {new Date(s.createdAt).toLocaleDateString()}
                          </div>
                        </div>
                      </td>
                      <td className="py-4 px-6 text-center">
                        <span className={`inline-flex items-center gap-1.5 text-[10px] font-bold px-2.5 py-1 rounded-lg ${
                          s.role === 'MANAGER' ? 'text-purple-600 bg-purple-50 border border-purple-200' : 'text-blue-600 bg-blue-50 border border-blue-200'
                        }`}>
                          {s.role === 'MANAGER' ? <ShieldCheck size={12} /> : <Shield size={12} />}
                          {roleLabel[s.role]}
                        </span>
                      </td>
                      <td className="py-4 px-6 text-center">
                        <span className={`inline-flex items-center gap-2 text-[10px] font-bold ${st.color}`}>
                          <span className={`w-1.5 h-1.5 rounded-full ${s.isActive ? 'bg-emerald-500' : 'bg-slate-300'}`} />
                          {st.label}
                        </span>
                      </td>
                      <td className="py-4 px-6 text-right">
                        <div className="flex items-center justify-end gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                          <button 
                            onClick={() => openEdit(s)}
                            className="p-2 rounded-lg text-slate-400 hover:text-accent hover:bg-slate-100 transition-all cursor-pointer"
                          >
                            <Edit3 size={14} />
                          </button>
                          {s.isActive && (
                            <button 
                              onClick={() => handleDeactivate(s.id, s.fullName)}
                              className="p-2 rounded-lg text-slate-400 hover:text-red-500 hover:bg-slate-100 transition-all cursor-pointer"
                            >
                              <UserX size={14} />
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  );
                })}
                {filtered.length === 0 && (
                  <tr>
                    <td colSpan={5} className="text-center py-20">
                      <div className="flex flex-col items-center gap-2 text-slate-300">
                        <Search size={32} />
                        <span className="text-sm font-medium">No personnel found</span>
                      </div>
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {(showAdd || editing) && (
        <div 
          className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/30 backdrop-blur-sm" 
          onClick={() => { setShowAdd(false); setEditing(null); }}
        >
          <div 
            className="bg-white rounded-2xl border border-slate-200 w-full max-w-md p-8 shadow-xl" 
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between mb-8">
              <div>
                <h3 className="text-xl font-bold text-slate-800 font-heading">{editing ? 'Edit Profile' : 'New Personnel'}</h3>
                <p className="text-xs text-slate-400 font-body mt-1">Fill in the details below to proceed.</p>
              </div>
              <button 
                onClick={() => { setShowAdd(false); setEditing(null); }} 
                className="p-2 rounded-xl text-slate-400 hover:text-slate-600 hover:bg-slate-100 transition-all"
              >
                <X size={20} />
              </button>
            </div>

            <form onSubmit={editing ? handleEdit : handleAdd} className="space-y-5">
              <div className="space-y-2">
                <label className="text-[10px] font-bold text-slate-500 uppercase tracking-widest ml-1">Full Name</label>
                <input 
                  autoFocus
                  required
                  value={fullName} 
                  onChange={(e) => setFullName(e.target.value)}
                  className="w-full px-4 py-3 rounded-xl bg-slate-50 border border-slate-200 text-sm text-slate-800 placeholder:text-slate-400 outline-none focus:bg-white focus:border-accent/50 transition-all" 
                  placeholder="e.g. John Doe" 
                />
              </div>

              <div className="space-y-2">
                <label className="text-[10px] font-bold text-slate-500 uppercase tracking-widest ml-1">Email Address</label>
                <input 
                  required
                  type="email" 
                  value={email} 
                  onChange={(e) => setEmail(e.target.value)}
                  className="w-full px-4 py-3 rounded-xl bg-slate-50 border border-slate-200 text-sm text-slate-800 placeholder:text-slate-400 outline-none focus:bg-white focus:border-accent/50 transition-all" 
                  placeholder="john@smartdine.com" 
                />
              </div>

              {!editing && (
                <div className="space-y-2">
                  <label className="text-[10px] font-bold text-slate-500 uppercase tracking-widest ml-1">Initial Password</label>
                  <input 
                    required
                    type="password" 
                    value={password} 
                    onChange={(e) => setPassword(e.target.value)}
                    className="w-full px-4 py-3 rounded-xl bg-slate-50 border border-slate-200 text-sm text-slate-800 placeholder:text-slate-400 outline-none focus:bg-white focus:border-accent/50 transition-all" 
                    placeholder="Min 6 characters" 
                  />
                </div>
              )}

              <div className="space-y-2">
                <label className="text-[10px] font-bold text-slate-500 uppercase tracking-widest ml-1">Assigned Role</label>
                <div className="grid grid-cols-2 gap-3">
                  <button
                    type="button"
                    onClick={() => setRole('STAFF')}
                    className={`p-4 rounded-xl border-2 transition-all flex flex-col items-center gap-2 ${
                      role === 'STAFF' ? 'border-blue-500/50 bg-blue-50 text-blue-600' : 'border-slate-200 bg-white text-slate-400'
                    }`}
                  >
                    <Shield size={24} />
                    <span className="text-[10px] font-bold uppercase tracking-widest">Staff</span>
                  </button>
                  <button
                    type="button"
                    onClick={() => setRole('MANAGER')}
                    className={`p-4 rounded-xl border-2 transition-all flex flex-col items-center gap-2 ${
                      role === 'MANAGER' ? 'border-purple-500/50 bg-purple-50 text-purple-600' : 'border-slate-200 bg-white text-slate-400'
                    }`}
                  >
                    <ShieldCheck size={24} />
                    <span className="text-[10px] font-bold uppercase tracking-widest">Manager</span>
                  </button>
                </div>
              </div>

              <div className="flex gap-4 pt-6">
                <button 
                  type="button"
                  onClick={() => { setShowAdd(false); setEditing(null); }}
                  className="flex-1 py-3 text-sm font-bold text-slate-500 hover:text-slate-800 transition-colors"
                >
                  Cancel
                </button>
                <button 
                  type="submit" 
                  disabled={saving}
                  className="flex-[2] py-3 rounded-xl bg-accent text-white text-sm font-bold hover:bg-accent/80 shadow-sm transition-all disabled:opacity-50"
                >
                  {saving ? 'Processing...' : (editing ? 'Update Profile' : 'Create Account')}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

export default AdminStaff;
