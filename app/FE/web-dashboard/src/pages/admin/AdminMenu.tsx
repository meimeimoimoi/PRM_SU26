import React, { useEffect, useState, useMemo, useCallback } from 'react';
import { Plus, Search, X, Edit3, Trash2, Upload, Check, AlertCircle } from 'lucide-react';
import { MenuItemResponse, MenuCategoryResponse } from '@/types/menu';
import { menuService } from '@/services/menuService';
import { categoryService } from '@/services/categoryService';
import { getErrorMessage } from '@/utils/apiError';

function AdminMenu() {
  const [items, setItems] = useState<MenuItemResponse[]>([]);
  const [categories, setCategories] = useState<MenuCategoryResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [catFilter, setCatFilter] = useState<number | 'ALL'>('ALL');
  const [modal, setModal] = useState<null | 'add' | 'edit'>(null);
  const [editId, setEditId] = useState<number | null>(null);

  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [price, setPrice] = useState(10000);
  const [categoryId, setCategoryId] = useState<number>(0);
  const [imageUrl, setImageUrl] = useState('');
  const [available, setAvailable] = useState(true);
  const [saving, setSaving] = useState(false);

  const fetchItems = useCallback(async () => {
    setLoading(true);
    try {
      const data = await menuService.getAllMenuItems();
      setItems(data || []);
    } catch { setItems([]); }
    finally { setLoading(false); }
  }, []);

  const fetchCategories = useCallback(async () => {
    try {
      const data = await categoryService.getAll();
      setCategories(data || []);
    } catch {}
  }, []);

  useEffect(() => { fetchItems(); fetchCategories(); }, [fetchItems, fetchCategories]);

  const resetForm = () => {
    setName(''); setDescription(''); setPrice(10000);
    setCategoryId(categories[0]?.id ?? 0);
    setImageUrl(''); setAvailable(true); setEditId(null);
  };

  const openEdit = (item: MenuItemResponse) => {
    setEditId(item.id); setName(item.name); setDescription(item.description || '');
    setPrice(item.price); setCategoryId(item.categoryId);
    setImageUrl(item.imageUrl || ''); setAvailable(item.isAvailable);
    setModal('edit');
  };

  const handleImageUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]; if (!file) return;
    try {
      const url = await menuService.uploadImage(file);
      setImageUrl(url);
    } catch (err) { alert(getErrorMessage(err, 'Upload failed')); }
    e.target.value = '';
  };

  const handleSave = async () => {
    if (!name.trim()) return;
    setSaving(true);
    try {
      const catId = categoryId ?? 0;
      if (modal === 'add') {
        const req = { name, description, price, imageUrl: imageUrl || undefined, categoryId: catId };
        const created = await menuService.createMenuItem(req);
        const newItem: MenuItemResponse = {
          id: created.id, name: req.name, description: req.description, price: req.price,
          imageUrl: req.imageUrl, categoryId: req.categoryId,
          categoryName: categories.find((c) => c.id === req.categoryId)?.name || '',
          isAvailable: true, averageRating: 0,
        };
        setItems((prev) => [...prev, newItem]);
      } else if (editId) {
        await menuService.updateMenuItem(editId, { name, description, price, imageUrl: imageUrl || undefined, categoryId: catId, isAvailable: available });
        setItems((prev) => prev.map((i) => i.id === editId ? {
          ...i, name, description, price, imageUrl: imageUrl || undefined, categoryId: catId,
          categoryName: categories.find((c) => c.id === catId)?.name || '',
          isAvailable: available,
        } as MenuItemResponse : i));
      }
      setModal(null); resetForm();
    } catch (err) { alert(getErrorMessage(err, 'Save failed')); }
    finally { setSaving(false); }
  };

  const toggleAvailable = async (id: number, next: boolean) => {
    try {
      await menuService.toggleAvailability(id, next);
      setItems((prev) => prev.map((i) => i.id === id ? { ...i, isAvailable: next } : i));
    } catch (err) { alert(getErrorMessage(err, 'Toggle failed')); }
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Delete this item?')) return;
    try {
      await menuService.deleteMenuItem(id);
      setItems((prev) => prev.filter((i) => i.id !== id));
    } catch (err) { alert(getErrorMessage(err, 'Delete failed')); }
  };

  useEffect(() => { if (modal === 'add') resetForm(); }, [modal]);

  const filtered = useMemo(() => items.filter((i) => {
    const matchSearch = !search || i.name.toLowerCase().includes(search.toLowerCase()) || (i.description?.toLowerCase() || '').includes(search.toLowerCase());
    const matchCat = catFilter === 'ALL' || i.categoryId === catFilter;
    return matchSearch && matchCat;
  }), [items, search, catFilter]);

  const formatPrice = (p: number) => p.toLocaleString('vi-VN') + 'đ';

  return (
    <div className="space-y-5">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <p className="text-sm text-slate-500 font-body">Manage your restaurant menu.</p>
        <button onClick={() => setModal('add')}
          className="flex items-center gap-2 px-4 py-2 rounded-xl bg-accent text-white text-sm font-medium hover:bg-accent/80 transition-all cursor-pointer font-body shadow-sm">
          <Plus size={16} /> Add Item
        </button>
      </div>

      <div className="flex flex-col sm:flex-row gap-3">
        <div className="flex items-center gap-2 px-3 py-2 rounded-lg bg-white border border-slate-200 flex-1 max-w-sm shadow-sm">
          <Search size={14} className="text-slate-400" />
          <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Search items..."
            className="bg-transparent border-none outline-none text-sm text-slate-700 placeholder:text-slate-400 w-full font-body" />
          {search && <X size={14} className="text-slate-400 cursor-pointer hover:text-slate-600" onClick={() => setSearch('')} />}
        </div>
        <select value={catFilter === 'ALL' ? 'ALL' : catFilter} onChange={(e) => setCatFilter(e.target.value === 'ALL' ? 'ALL' : Number(e.target.value))}
          className="px-3 py-2 rounded-lg bg-white border border-slate-200 text-sm text-slate-700 outline-none font-body cursor-pointer shadow-sm">
          <option value="ALL">All Categories</option>
          {categories.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
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
                  <th className="text-left py-3.5 px-5 text-[11px] font-semibold text-slate-500 uppercase tracking-wider">Item</th>
                  <th className="text-left py-3.5 px-4 text-[11px] font-semibold text-slate-500 uppercase tracking-wider hidden sm:table-cell">Category</th>
                  <th className="text-right py-3.5 px-4 text-[11px] font-semibold text-slate-500 uppercase tracking-wider">Price</th>
                  <th className="text-center py-3.5 px-4 text-[11px] font-semibold text-slate-500 uppercase tracking-wider hidden md:table-cell">Status</th>
                  <th className="text-right py-3.5 px-5 text-[11px] font-semibold text-slate-500 uppercase tracking-wider">Actions</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map((item) => (
                  <tr key={item.id} className="border-b border-slate-100 hover:bg-slate-50 transition-colors">
                    <td className="py-3.5 px-5">
                      <div className="flex items-center gap-3">
                        <img src={item.imageUrl || 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=80'} alt={item.name}
                          className="w-9 h-9 rounded-lg object-cover shrink-0" />
                        <div>
                          <div className="text-sm font-medium text-slate-800">{item.name}</div>
                          {item.description && <div className="text-[11px] text-slate-400 truncate max-w-[180px]">{item.description}</div>}
                        </div>
                      </div>
                    </td>
                    <td className="py-3.5 px-4 text-slate-500 text-xs hidden sm:table-cell">{item.categoryName || '—'}</td>
                    <td className="py-3.5 px-4 text-right text-slate-800 font-heading text-xs font-semibold">{formatPrice(item.price)}</td>
                    <td className="py-3.5 px-4 text-center hidden md:table-cell">
                      <span className={`inline-flex items-center gap-1 text-[11px] font-medium ${item.isAvailable ? 'text-emerald-600' : 'text-red-500'}`}>
                        {item.isAvailable ? <Check size={11} /> : <AlertCircle size={11} />}
                        {item.isAvailable ? 'Available' : 'Out of Stock'}
                      </span>
                    </td>
                    <td className="py-3.5 px-5 text-right">
                      <div className="flex items-center justify-end gap-1">
                        <button onClick={() => openEdit(item)}
                          className="p-1.5 rounded-lg text-slate-400 hover:text-accent hover:bg-slate-100 transition-all cursor-pointer">
                          <Edit3 size={14} />
                        </button>
                        <button onClick={() => toggleAvailable(item.id, !item.isAvailable)}
                          className={`p-1.5 rounded-lg transition-all cursor-pointer ${
                            item.isAvailable ? 'text-amber-500 hover:text-amber-600 hover:bg-slate-100' : 'text-emerald-500 hover:text-emerald-600 hover:bg-slate-100'
                          }`}>
                          {item.isAvailable ? <AlertCircle size={14} /> : <Check size={14} />}
                        </button>
                        <button onClick={() => handleDelete(item.id)}
                          className="p-1.5 rounded-lg text-slate-400 hover:text-red-500 hover:bg-slate-100 transition-all cursor-pointer">
                          <Trash2 size={14} />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
                {filtered.length === 0 && (
                  <tr><td colSpan={5} className="text-center py-10 text-slate-400 text-sm font-body">No items found</td></tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {modal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/30" onClick={() => setModal(null)}>
          <div className="bg-white rounded-2xl border border-slate-200 w-full max-w-lg p-6 animate-slide-up shadow-xl" onClick={(e) => e.stopPropagation()}>
            <div className="flex items-center justify-between mb-5">
              <h3 className="text-base font-semibold text-slate-800 font-heading">{modal === 'add' ? 'Add Item' : 'Edit Item'}</h3>
              <button onClick={() => setModal(null)} className="p-1 text-slate-400 hover:text-slate-600 transition-colors cursor-pointer">
                <X size={18} />
              </button>
            </div>
            <div className="space-y-4">
              <div>
                <label className="text-[11px] text-slate-500 font-body block mb-1">Name *</label>
                <input value={name} onChange={(e) => setName(e.target.value)}
                  className="w-full px-3 py-2 rounded-lg bg-slate-50 border border-slate-200 text-sm text-slate-800 outline-none font-body placeholder:text-slate-400" placeholder="Item name" />
              </div>
              <div>
                <label className="text-[11px] text-slate-500 font-body block mb-1">Description</label>
                <input value={description} onChange={(e) => setDescription(e.target.value)}
                  className="w-full px-3 py-2 rounded-lg bg-slate-50 border border-slate-200 text-sm text-slate-800 outline-none font-body placeholder:text-slate-400" placeholder="Brief description" />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-[11px] text-slate-500 font-body block mb-1">Price (VND) *</label>
                  <input type="number" value={price} onChange={(e) => setPrice(Number(e.target.value))} min={1000} step={5000}
                    className="w-full px-3 py-2 rounded-lg bg-slate-50 border border-slate-200 text-sm text-slate-800 outline-none font-body" />
                </div>
                <div>
                  <label className="text-[11px] text-slate-500 font-body block mb-1">Category *</label>
                  <select value={categoryId} onChange={(e) => setCategoryId(Number(e.target.value))}
                    className="w-full px-3 py-2 rounded-lg bg-white border border-slate-200 text-sm text-slate-700 outline-none font-body cursor-pointer">
                    {categories.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
                  </select>
                </div>
              </div>
              <div>
                <label className="text-[11px] text-slate-500 font-body block mb-1">Image</label>
                <div className="flex items-center gap-3">
                  <input value={imageUrl} onChange={(e) => setImageUrl(e.target.value)}
                    className="flex-1 px-3 py-2 rounded-lg bg-slate-50 border border-slate-200 text-sm text-slate-800 outline-none font-body placeholder:text-slate-400" placeholder="Image URL" />
                  <label className="p-2 rounded-lg bg-slate-50 border border-slate-200 text-slate-500 hover:text-accent cursor-pointer hover:bg-slate-100 transition-all">
                    <Upload size={16} />
                    <input type="file" accept="image/*" onChange={handleImageUpload} className="hidden" />
                  </label>
                </div>
                {imageUrl && <img src={imageUrl} alt="preview" className="mt-2 w-16 h-16 rounded-lg object-cover" />}
              </div>
              {modal === 'edit' && (
                <div className="flex items-center gap-2">
                  <input type="checkbox" id="avail" checked={available} onChange={(e) => setAvailable(e.target.checked)}
                    className="w-4 h-4 rounded accent-accent cursor-pointer" />
                  <label htmlFor="avail" className="text-sm text-slate-600 font-body cursor-pointer">Available</label>
                </div>
              )}
              <div className="flex justify-end gap-3 pt-2">
                <button onClick={() => setModal(null)}
                  className="px-4 py-2 rounded-lg text-sm text-slate-600 hover:text-slate-800 hover:bg-slate-100 transition-all cursor-pointer font-body">Cancel</button>
                <button onClick={handleSave} disabled={saving || !name.trim()}
                  className="px-4 py-2 rounded-lg bg-accent text-white text-sm font-medium hover:bg-accent/80 transition-all cursor-pointer font-body disabled:opacity-50">
                  {saving ? 'Saving...' : 'Save'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default AdminMenu;
