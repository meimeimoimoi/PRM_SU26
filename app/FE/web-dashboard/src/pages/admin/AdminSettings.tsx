import React, { useEffect, useState } from 'react';
import { Building2, Phone, MapPin, Clock, Percent, Save } from 'lucide-react';
import { settingsService } from '@/services/settingsService';
import { RestaurantSettings } from '@/types/settings';
import { getErrorMessage } from '@/utils/apiError';

function AdminSettings() {
  const [settings, setSettings] = useState<RestaurantSettings | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  const [restaurantName, setRestaurantName] = useState('');
  const [address, setAddress] = useState('');
  const [phone, setPhone] = useState('');
  const [openingTime, setOpeningTime] = useState('08:00');
  const [closingTime, setClosingTime] = useState('22:00');
  const [taxRate, setTaxRate] = useState(0);
  const [serviceChargeRate, setServiceChargeRate] = useState(0);

  useEffect(() => {
    const fetch = async () => {
      setLoading(true);
      try {
        const data = await settingsService.getSettings();
        setSettings(data);
        setRestaurantName(data.restaurantName || '');
        setAddress(data.address || '');
        setPhone(data.phone || '');
        setOpeningTime(data.openingTime || '08:00');
        setClosingTime(data.closingTime || '22:00');
        setTaxRate(data.taxRate || 0);
        setServiceChargeRate(data.serviceChargeRate || 0);
      } catch { }
      finally { setLoading(false); }
    };
    fetch();
  }, []);

  const handleSave = async () => {
    setSaving(true);
    try {
      const updated = await settingsService.updateSettings({
        restaurantName, address, phone, openingTime, closingTime, taxRate, serviceChargeRate,
      });
      setSettings(updated);
      alert('Settings updated successfully!');
    } catch (err) { alert(getErrorMessage(err, 'Update failed')); }
    finally { setSaving(false); }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-40">
        <div className="w-6 h-6 border-2 border-accent/30 border-t-accent rounded-full animate-spin" />
      </div>
    );
  }

  return (
    <div className="space-y-5 max-w-2xl">
      <p className="text-sm text-slate-500 font-body">Configure your restaurant information and preferences.</p>

      <div className="bg-white rounded-2xl border border-slate-200 p-6 space-y-5 shadow-sm">
        <h3 className="text-sm font-semibold text-slate-800 font-heading">General Information</h3>

        <div>
          <label className="text-[11px] text-slate-500 font-body flex items-center gap-1.5 mb-1.5">
            <Building2 size={12} /> Restaurant Name
          </label>
          <input value={restaurantName} onChange={(e) => setRestaurantName(e.target.value)}
            className="w-full px-3 py-2 rounded-lg bg-slate-50 border border-slate-200 text-sm text-slate-800 outline-none font-body focus:bg-white focus:border-accent/50 transition-all" />
        </div>

        <div>
          <label className="text-[11px] text-slate-500 font-body flex items-center gap-1.5 mb-1.5">
            <MapPin size={12} /> Address
          </label>
          <input value={address} onChange={(e) => setAddress(e.target.value)}
            className="w-full px-3 py-2 rounded-lg bg-slate-50 border border-slate-200 text-sm text-slate-800 outline-none font-body focus:bg-white focus:border-accent/50 transition-all" />
        </div>

        <div>
          <label className="text-[11px] text-slate-500 font-body flex items-center gap-1.5 mb-1.5">
            <Phone size={12} /> Phone
          </label>
          <input value={phone} onChange={(e) => setPhone(e.target.value)}
            className="w-full px-3 py-2 rounded-lg bg-slate-50 border border-slate-200 text-sm text-slate-800 outline-none font-body focus:bg-white focus:border-accent/50 transition-all" />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="text-[11px] text-slate-500 font-body flex items-center gap-1.5 mb-1.5">
              <Clock size={12} /> Opening Time
            </label>
            <input type="time" value={openingTime} onChange={(e) => setOpeningTime(e.target.value)}
              className="w-full px-3 py-2 rounded-lg bg-slate-50 border border-slate-200 text-sm text-slate-800 outline-none font-body focus:bg-white focus:border-accent/50 transition-all" />
          </div>
          <div>
            <label className="text-[11px] text-slate-500 font-body flex items-center gap-1.5 mb-1.5">
              <Clock size={12} /> Closing Time
            </label>
            <input type="time" value={closingTime} onChange={(e) => setClosingTime(e.target.value)}
              className="w-full px-3 py-2 rounded-lg bg-slate-50 border border-slate-200 text-sm text-slate-800 outline-none font-body focus:bg-white focus:border-accent/50 transition-all" />
          </div>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="text-[11px] text-slate-500 font-body flex items-center gap-1.5 mb-1.5">
              <Percent size={12} /> Tax Rate (%)
            </label>
            <input type="number" value={taxRate} onChange={(e) => setTaxRate(Number(e.target.value))} min={0} max={100}
              className="w-full px-3 py-2 rounded-lg bg-slate-50 border border-slate-200 text-sm text-slate-800 outline-none font-body focus:bg-white focus:border-accent/50 transition-all" />
          </div>
          <div>
            <label className="text-[11px] text-slate-500 font-body flex items-center gap-1.5 mb-1.5">
              <Percent size={12} /> Service Charge (%)
            </label>
            <input type="number" value={serviceChargeRate} onChange={(e) => setServiceChargeRate(Number(e.target.value))} min={0} max={100}
              className="w-full px-3 py-2 rounded-lg bg-slate-50 border border-slate-200 text-sm text-slate-800 outline-none font-body focus:bg-white focus:border-accent/50 transition-all" />
          </div>
        </div>

        <div className="flex justify-end pt-2">
          <button onClick={handleSave} disabled={saving}
            className="flex items-center gap-2 px-5 py-2 rounded-xl bg-accent text-white text-sm font-medium hover:bg-accent/80 transition-all cursor-pointer font-body disabled:opacity-50 shadow-sm">
            <Save size={16} /> {saving ? 'Saving...' : 'Save Settings'}
          </button>
        </div>
      </div>
    </div>
  );
}

export default AdminSettings;
