import React, { useEffect, useState, useMemo, useCallback } from 'react';
import { Search, Plus, X, Edit3, Trash2, QrCode, CheckCircle, AlertCircle, Wrench } from 'lucide-react';
import { Table as TableType, TableStatus } from '@/types/table';
import { Location } from '@/types/location';
import { tableService } from '@/services/tableService';
import { locationService } from '@/services/locationService';
import { getErrorMessage } from '@/utils/apiError';

const statusStyles: Record<TableStatus, { label: string; color: string; bg: string }> = {
  AVAILABLE: { label: 'Empty', color: 'text-emerald-600', bg: 'bg-emerald-50' },
  OCCUPIED: { label: 'Occupied', color: 'text-red-500', bg: 'bg-red-50' },
  MAINTENANCE: { label: 'Needs Cleaning', color: 'text-amber-600', bg: 'bg-amber-50' },
  RESERVED: { label: 'Reserved', color: 'text-purple-600', bg: 'bg-purple-50' },
};

function AdminTable() {
  const [tables, setTables] = useState<TableType[]>([]);
  const [locations, setLocations] = useState<Location[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<TableStatus | 'ALL'>('ALL');

  const [showAdd, setShowAdd] = useState(false);
  const [showAddLocation, setShowAddLocation] = useState(false);
  const [editTable, setEditTable] = useState<TableType | null>(null);
  const [qrTable, setQrTable] = useState<TableType | null>(null);
  const [saving, setSaving] = useState(false);
  const [copied, setCopied] = useState(false);

  const [tableNumber, setTableNumber] = useState(1);
  const [capacity, setCapacity] = useState(4);
  const [locationId, setLocationId] = useState<number | undefined>();
  const [locationName, setLocationName] = useState('');

  const fetchData = useCallback(async () => {
    setLoading(true);
    try {
      const [t, l] = await Promise.all([tableService.getAllTables(), locationService.getAll()]);
      setTables((t || []).sort((a, b) => a.tableNumber - b.tableNumber));
      setLocations(l || []);
    } catch { setTables([]); setLocations([]); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { fetchData(); }, [fetchData]);

  const resetForm = () => { setTableNumber(1); setCapacity(4); setLocationId(undefined); };

  const handleAdd = async () => {
    if (tables.some((t) => t.tableNumber === tableNumber)) { alert(`Table ${tableNumber} already exists!`); return; }
    setSaving(true);
    try {
      const created = await tableService.createTable(tableNumber, capacity, locationId);
      setTables((prev) => [...prev, created].sort((a, b) => a.tableNumber - b.tableNumber));
      setShowAdd(false); resetForm();
    } catch (err) { alert(getErrorMessage(err, 'Add table failed')); }
    finally { setSaving(false); }
  };

  const handleEdit = async () => {
    if (!editTable) return;
    setSaving(true);
    try {
      const updated = await tableService.updateTable(editTable.id, { capacity, locationId });
      setTables((prev) => prev.map((t) => (t.id === editTable.id ? updated : t)));
      setEditTable(null);
    } catch (err) { alert(getErrorMessage(err, 'Edit failed')); }
    finally { setSaving(false); }
  };

  const handleStatusChange = async (id: number, status: TableStatus) => {
    try {
      const updated = await tableService.updateTableStatus(id, status);
      setTables((prev) => prev.map((t) => (t.id === id ? updated : t)));
    } catch (err) { alert(getErrorMessage(err, 'Status change failed')); }
  };

  const handleDelete = async (t: TableType) => {
    if (!confirm(`Delete T-${t.tableNumber.toString().padStart(2, '0')}? QR code will be invalid.`)) return;
    try {
      await tableService.deleteTable(t.id);
      setTables((prev) => prev.filter((x) => x.id !== t.id));
    } catch (err) { alert(getErrorMessage(err, 'Delete failed')); }
  };

  const openEdit = (t: TableType) => {
    setEditTable(t); setCapacity(t.capacity); setLocationId(t.locationId);
  };

  const handleCopyQr = async () => {
    if (qrTable?.qrCode) {
      await navigator.clipboard.writeText(qrTable.qrCode);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  const handleCreateLocation = async () => {
    if (!locationName.trim()) return;
    try {
      const loc = await locationService.create(locationName);
      setLocations((prev) => [...prev, loc]);
      setLocationId(loc.id);
      setShowAddLocation(false); setLocationName('');
    } catch (err) { alert(getErrorMessage(err, 'Create location failed')); }
  };

  const filtered = useMemo(() => tables.filter((t) => {
    const idStr = `T-${t.tableNumber.toString().padStart(2, '0')}`.toLowerCase();
    const locStr = (t.locationName || '').toLowerCase();
    const q = search.toLowerCase();
    const matchSearch = !search || idStr.includes(q) || locStr.includes(q);
    const matchStatus = statusFilter === 'ALL' || t.status === statusFilter;
    return matchSearch && matchStatus;
  }), [tables, search, statusFilter]);

  return (
    <div className="space-y-5">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <p className="text-sm text-slate-500 font-body">Manage restaurant tables and their statuses.</p>
        <button onClick={() => { resetForm(); setShowAdd(true); }}
          className="flex items-center gap-2 px-4 py-2 rounded-xl bg-accent text-white text-sm font-medium hover:bg-accent/80 transition-all cursor-pointer font-body shadow-sm">
          <Plus size={16} /> Add Table
        </button>
      </div>

      <div className="flex flex-col sm:flex-row gap-3">
        <div className="flex items-center gap-2 px-3 py-2 rounded-lg bg-white border border-slate-200 flex-1 max-w-sm shadow-sm">
          <Search size={14} className="text-slate-400" />
          <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Search by ID or location..."
            className="bg-transparent border-none outline-none text-sm text-slate-700 placeholder:text-slate-400 w-full font-body" />
          {search && <X size={14} className="text-slate-400 cursor-pointer hover:text-slate-600" onClick={() => setSearch('')} />}
        </div>
        <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value as TableStatus | 'ALL')}
          className="px-3 py-2 rounded-lg bg-white border border-slate-200 text-sm text-slate-700 outline-none font-body cursor-pointer shadow-sm">
          <option value="ALL">All Status</option>
          <option value="AVAILABLE">Empty</option>
          <option value="OCCUPIED">Occupied</option>
          <option value="MAINTENANCE">Needs Cleaning</option>
          <option value="RESERVED">Reserved</option>
        </select>
      </div>

      <div className="bg-white rounded-2xl border border-slate-200 overflow-hidden shadow-sm">
        {loading ? (
          <div className="flex items-center justify-center h-40">
            <div className="w-6 h-6 border-2 border-accent/30 border-t-accent rounded-full animate-spin" />
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm font-body">
              <thead>
                <tr className="border-b border-slate-200 bg-slate-50">
                  <th className="text-left py-3.5 px-5 text-[11px] font-semibold text-slate-500 uppercase tracking-wider">Table</th>
                  <th className="text-center py-3.5 px-4 text-[11px] font-semibold text-slate-500 uppercase tracking-wider">Capacity</th>
                  <th className="text-left py-3.5 px-4 text-[11px] font-semibold text-slate-500 uppercase tracking-wider hidden sm:table-cell">Location</th>
                  <th className="text-center py-3.5 px-4 text-[11px] font-semibold text-slate-500 uppercase tracking-wider">Status</th>
                  <th className="text-right py-3.5 px-5 text-[11px] font-semibold text-slate-500 uppercase tracking-wider">Actions</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map((t) => {
                  const s = statusStyles[t.status] || statusStyles.OCCUPIED;
                  return (
                    <tr key={t.id} className="border-b border-slate-100 hover:bg-slate-50 transition-colors">
                      <td className="py-3.5 px-5">
                        <span className="text-slate-800 font-medium font-heading text-sm">T-{t.tableNumber.toString().padStart(2, '0')}</span>
                      </td>
                      <td className="py-3.5 px-4 text-center text-slate-500 text-xs">{t.capacity} pax</td>
                      <td className="py-3.5 px-4 text-slate-500 text-xs hidden sm:table-cell">{t.locationName || '—'}</td>
                      <td className="py-3.5 px-4 text-center">
                        <span className={`inline-flex items-center gap-1.5 text-[11px] font-medium px-2.5 py-1 rounded-md ${s.bg} ${s.color}`}>
                          {t.status === 'AVAILABLE' ? <CheckCircle size={11} /> :
                           t.status === 'OCCUPIED' ? <AlertCircle size={11} /> :
                           t.status === 'MAINTENANCE' ? <Wrench size={11} /> : <AlertCircle size={11} />}
                          {s.label}
                        </span>
                      </td>
                      <td className="py-3.5 px-5 text-right">
                        <div className="flex items-center justify-end gap-1">
                          {t.status === 'MAINTENANCE' && (
                            <button onClick={() => handleStatusChange(t.id, 'AVAILABLE')}
                              className="px-2 py-1 rounded-md text-[10px] font-medium bg-emerald-50 text-emerald-600 hover:bg-emerald-100 transition-all cursor-pointer font-body">
                              Cleaned
                            </button>
                          )}
                          <button onClick={() => setQrTable(t)}
                            className="p-1.5 rounded-lg text-slate-400 hover:text-accent hover:bg-slate-100 transition-all cursor-pointer">
                            <QrCode size={14} />
                          </button>
                          <button onClick={() => openEdit(t)}
                            className="p-1.5 rounded-lg text-slate-400 hover:text-accent hover:bg-slate-100 transition-all cursor-pointer">
                            <Edit3 size={14} />
                          </button>
                          <div className="relative group">
                            <button className="p-1.5 rounded-lg text-slate-400 hover:text-slate-800 hover:bg-slate-100 transition-all cursor-pointer">
                              <AlertCircle size={14} />
                            </button>
                            <div className="absolute right-0 top-full mt-1 w-44 py-1 rounded-lg bg-white border border-slate-200 shadow-lg opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all z-10">
                              {(['AVAILABLE', 'OCCUPIED', 'MAINTENANCE'] as TableStatus[]).map((st) => (
                                <button key={st} onClick={() => handleStatusChange(t.id, st)}
                                  className="flex items-center gap-2 w-full px-3 py-1.5 text-xs text-slate-600 hover:text-slate-800 hover:bg-slate-50 transition-colors cursor-pointer font-body text-left">
                                  {st === 'AVAILABLE' ? <CheckCircle size={12} className="text-emerald-500" /> :
                                   st === 'OCCUPIED' ? <AlertCircle size={12} className="text-red-500" /> :
                                   <Wrench size={12} className="text-amber-500" />}
                                  {statusStyles[st].label}
                                </button>
                              ))}
                            </div>
                          </div>
                          <button onClick={() => handleDelete(t)}
                            className="p-1.5 rounded-lg text-slate-400 hover:text-red-500 hover:bg-slate-100 transition-all cursor-pointer">
                            <Trash2 size={14} />
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
                {filtered.length === 0 && (
                  <tr><td colSpan={5} className="text-center py-10 text-slate-400 text-sm font-body">No tables found</td></tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {showAdd && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/30" onClick={() => setShowAdd(false)}>
          <div className="bg-white rounded-2xl border border-slate-200 w-full max-w-md p-6 animate-slide-up shadow-xl" onClick={(e) => e.stopPropagation()}>
            <div className="flex items-center justify-between mb-5">
              <h3 className="text-base font-semibold text-slate-800 font-heading">Add Table</h3>
              <button onClick={() => setShowAdd(false)} className="p-1 text-slate-400 hover:text-slate-600 transition-colors cursor-pointer"><X size={18} /></button>
            </div>
            <div className="space-y-4">
              <div>
                <label className="text-[11px] text-slate-500 font-body block mb-1">Table Number *</label>
                <input type="number" value={tableNumber} onChange={(e) => setTableNumber(Number(e.target.value))} min={1}
                  className="w-full px-3 py-2 rounded-lg bg-slate-50 border border-slate-200 text-sm text-slate-800 outline-none font-body" />
              </div>
              <div>
                <label className="text-[11px] text-slate-500 font-body block mb-1">Capacity (PAX) *</label>
                <input type="number" value={capacity} onChange={(e) => setCapacity(Number(e.target.value))} min={1} max={20}
                  className="w-full px-3 py-2 rounded-lg bg-slate-50 border border-slate-200 text-sm text-slate-800 outline-none font-body" />
              </div>
              <div>
                <label className="text-[11px] text-slate-500 font-body block mb-1">Location</label>
                <select value={locationId ?? ''} onChange={(e) => {
                  const v = e.target.value;
                  if (v === '__create__') { setShowAddLocation(true); return; }
                  setLocationId(v ? Number(v) : undefined);
                }}
                  className="w-full px-3 py-2 rounded-lg bg-white border border-slate-200 text-sm text-slate-700 outline-none font-body cursor-pointer">
                  <option value="">No location</option>
                  {locations.map((l) => <option key={l.id} value={l.id}>{l.name}</option>)}
                  <option value="__create__" className="text-accent">+ Create new location...</option>
                </select>
              </div>
              <div className="flex justify-end gap-3 pt-2">
                <button onClick={() => setShowAdd(false)}
                  className="px-4 py-2 rounded-lg text-sm text-slate-600 hover:text-slate-800 hover:bg-slate-100 transition-all cursor-pointer font-body">Cancel</button>
                <button onClick={handleAdd} disabled={saving}
                  className="px-4 py-2 rounded-lg bg-accent text-white text-sm font-medium hover:bg-accent/80 transition-all cursor-pointer font-body disabled:opacity-50">
                  {saving ? 'Adding...' : 'Add Table'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {editTable && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/30" onClick={() => setEditTable(null)}>
          <div className="bg-white rounded-2xl border border-slate-200 w-full max-w-md p-6 animate-slide-up shadow-xl" onClick={(e) => e.stopPropagation()}>
            <div className="flex items-center justify-between mb-5">
              <h3 className="text-base font-semibold text-slate-800 font-heading">Edit T-{editTable.tableNumber.toString().padStart(2, '0')}</h3>
              <button onClick={() => setEditTable(null)} className="p-1 text-slate-400 hover:text-slate-600 transition-colors cursor-pointer"><X size={18} /></button>
            </div>
            <div className="space-y-4">
              <div>
                <label className="text-[11px] text-slate-500 font-body block mb-1">Table Number (locked)</label>
                <input value={`T-${editTable.tableNumber.toString().padStart(2, '0')}`} disabled
                  className="w-full px-3 py-2 rounded-lg bg-slate-100 border border-slate-200 text-sm text-slate-500 outline-none font-body cursor-not-allowed" />
              </div>
              <div>
                <label className="text-[11px] text-slate-500 font-body block mb-1">Capacity (PAX)</label>
                <input type="number" value={capacity} onChange={(e) => setCapacity(Number(e.target.value))} min={1} max={20}
                  className="w-full px-3 py-2 rounded-lg bg-slate-50 border border-slate-200 text-sm text-slate-800 outline-none font-body" />
              </div>
              <div>
                <label className="text-[11px] text-slate-500 font-body block mb-1">Location</label>
                <select value={locationId ?? ''} onChange={(e) => setLocationId(e.target.value ? Number(e.target.value) : undefined)}
                  className="w-full px-3 py-2 rounded-lg bg-white border border-slate-200 text-sm text-slate-700 outline-none font-body cursor-pointer">
                  <option value="">No location</option>
                  {locations.map((l) => <option key={l.id} value={l.id}>{l.name}</option>)}
                </select>
              </div>
              <div className="flex justify-end gap-3 pt-2">
                <button onClick={() => setEditTable(null)}
                  className="px-4 py-2 rounded-lg text-sm text-slate-600 hover:text-slate-800 hover:bg-slate-100 transition-all cursor-pointer font-body">Cancel</button>
                <button onClick={handleEdit} disabled={saving}
                  className="px-4 py-2 rounded-lg bg-accent text-white text-sm font-medium hover:bg-accent/80 transition-all cursor-pointer font-body disabled:opacity-50">
                  {saving ? 'Saving...' : 'Save'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {showAddLocation && (
        <div className="fixed inset-0 z-[60] flex items-center justify-center p-4 bg-black/30" onClick={() => setShowAddLocation(false)}>
          <div className="bg-white rounded-2xl border border-slate-200 w-full max-w-sm p-6 animate-slide-up shadow-xl" onClick={(e) => e.stopPropagation()}>
            <div className="flex items-center justify-between mb-5">
              <h3 className="text-base font-semibold text-slate-800 font-heading">New Location</h3>
              <button onClick={() => setShowAddLocation(false)} className="p-1 text-slate-400 hover:text-slate-600 transition-colors cursor-pointer"><X size={18} /></button>
            </div>
            <div className="space-y-4">
              <div>
                <label className="text-[11px] text-slate-500 font-body block mb-1">Location Name *</label>
                <input value={locationName} onChange={(e) => setLocationName(e.target.value)}
                  className="w-full px-3 py-2 rounded-lg bg-slate-50 border border-slate-200 text-sm text-slate-800 outline-none font-body" placeholder="e.g. Floor 2, VIP Room" />
              </div>
              <div className="flex justify-end gap-3 pt-2">
                <button onClick={() => setShowAddLocation(false)}
                  className="px-4 py-2 rounded-lg text-sm text-slate-600 hover:text-slate-800 hover:bg-slate-100 transition-all cursor-pointer font-body">Cancel</button>
                <button onClick={handleCreateLocation} disabled={!locationName.trim()}
                  className="px-4 py-2 rounded-lg bg-accent text-white text-sm font-medium hover:bg-accent/80 transition-all cursor-pointer font-body disabled:opacity-50">Create</button>
              </div>
            </div>
          </div>
        </div>
      )}

      {qrTable && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/30" onClick={() => setQrTable(null)}>
          <div className="bg-white rounded-2xl border border-slate-200 w-full max-w-sm p-6 animate-slide-up shadow-xl" onClick={(e) => e.stopPropagation()}>
            <div className="flex items-center justify-between mb-5">
              <h3 className="text-base font-semibold text-slate-800 font-heading">QR — T-{qrTable.tableNumber.toString().padStart(2, '0')}</h3>
              <button onClick={() => setQrTable(null)} className="p-1 text-slate-400 hover:text-slate-600 transition-colors cursor-pointer"><X size={18} /></button>
            </div>
            {qrTable.qrCode ? (
              <div className="flex flex-col items-center gap-4">
                <div className="bg-white p-4 rounded-xl border border-slate-200">
                  <img src={`https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=${encodeURIComponent(qrTable.qrCode)}`}
                    alt="QR Code" className="w-48 h-48" />
                </div>
                <div className="flex items-center gap-2 w-full">
                  <input value={qrTable.qrCode} readOnly
                    className="flex-1 px-3 py-2 rounded-lg bg-slate-50 border border-slate-200 text-xs text-slate-700 outline-none font-body" />
                  <button onClick={handleCopyQr}
                    className="px-3 py-2 rounded-lg bg-accentlight text-accent text-xs font-medium hover:bg-accent/10 transition-all cursor-pointer font-body whitespace-nowrap">
                    {copied ? 'Copied!' : 'Copy'}
                  </button>
                </div>
                <p className="text-[10px] text-slate-400 text-center font-body">Print & place on table for customers to scan.</p>
              </div>
            ) : (
              <p className="text-sm text-slate-500 text-center py-4 font-body">No QR code available for this table.</p>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

export default AdminTable;
