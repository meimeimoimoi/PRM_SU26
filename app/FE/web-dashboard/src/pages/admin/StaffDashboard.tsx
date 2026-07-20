import React, { useState, useCallback } from 'react';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import { useDispatch, useSelector } from 'react-redux';
import { logout, selectCurrentUser } from '@/store/slices/authSlice';
import {
  ClipboardList,
  ChefHat,
  LogOut,
  Bell,
  Menu,
  X,
  Clock,
  LayoutDashboard,
  Bot,
} from 'lucide-react';

const navigation = [
  { name: 'Kitchen & Billing', icon: ChefHat, path: '/staffboard/kitchen' },
  { name: 'Today Orders', icon: ClipboardList, path: '/staffboard/orders' },
  { name: 'Robot Controller', icon: Bot, path: '/staffboard/draw-map' },
];

function Sidebar({ open, onClose }: {
  open: boolean; onClose: () => void;
}) {
  const navigate = useNavigate();
  const location = useLocation();
  const dispatch = useDispatch();
  const user = useSelector(selectCurrentUser);

  return (
    <>
      {open && (
        <div className="fixed inset-0 bg-black/20 z-40 lg:hidden" onClick={onClose} />
      )}

      <aside className={`
        fixed lg:static inset-y-0 left-0 z-50 w-[260px] flex flex-col
        glass-sidebar transition-transform duration-300 ease-out
        ${open ? 'translate-x-0' : '-translate-x-full lg:translate-x-0'}
      `}>
        <div className="flex items-center justify-between px-5 py-5 border-b border-slate-200">
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-xl bg-accent flex items-center justify-center">
              <ChefHat size={18} className="text-white" />
            </div>
            <div>
              <h2 className="text-sm font-bold text-slate-800 font-heading">SmartDine</h2>
              <p className="text-[10px] text-slate-400 font-body">Staff Dashboard</p>
            </div>
          </div>
          <button onClick={onClose} className="lg:hidden p-1 text-slate-400 hover:text-slate-600 transition-colors cursor-pointer">
            <X size={18} />
          </button>
        </div>

        <nav className="flex-1 py-4 px-3 space-y-1 overflow-y-auto scrollbar-thin">
          {navigation.map((item) => {
            const active = location.pathname === item.path;
            return (
              <button
                key={item.name}
                onClick={() => { navigate(item.path); onClose(); }}
                className={`
                  flex items-center gap-3 w-full px-3 py-2.5 rounded-xl text-sm font-medium
                  transition-all duration-200 cursor-pointer text-left
                  ${active
                    ? 'bg-accentlight text-accent shadow-sm'
                    : 'text-slate-500 hover:text-slate-800 hover:bg-slate-100'
                  }
                `}
              >
                <item.icon size={18} />
                <span className="font-body">{item.name}</span>
              </button>
            );
          })}

          {user?.role === 'MANAGER' && (
            <button
              onClick={() => { navigate('/admin-v2'); onClose(); }}
              className="flex items-center gap-3 w-full px-3 py-2.5 rounded-xl text-sm font-medium text-slate-500 hover:text-slate-800 hover:bg-slate-100 transition-all duration-200 cursor-pointer text-left mt-10"
            >
              <LayoutDashboard size={18} />
              <span className="font-body">Back to Admin</span>
            </button>
          )}
        </nav>

        <div className="px-3 py-3 border-t border-slate-200 space-y-2">
          <div className="flex items-center gap-3 px-3 py-2.5 rounded-xl bg-slate-50">
            <div className="w-8 h-8 rounded-full bg-accentlight flex items-center justify-center text-accent text-xs font-bold font-heading">
              {user?.fullName ? user.fullName.split(' ').map((n: string) => n[0]).join('').slice(0, 2).toUpperCase() : '?'}
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium text-slate-700 truncate font-body">{user?.fullName || 'User'}</p>
              <p className="text-[10px] text-slate-400 font-body">{user?.role === 'MANAGER' ? 'Manager' : 'Staff'}</p>
            </div>
            <button onClick={() => { dispatch(logout()); navigate('/login'); }} className="p-1 text-slate-400 hover:text-red-500 transition-colors cursor-pointer">
              <LogOut size={15} />
            </button>
          </div>
        </div>
      </aside>
    </>
  );
}

function StaffDashboard() {
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const location = useLocation();
  const dispatch = useDispatch();
  const navigate = useNavigate();
  const user = useSelector(selectCurrentUser);

  const pageTitles: Record<string, string> = {
    '/staffboard': 'Staff Operations',
    '/staffboard/kitchen': 'Kitchen & Billing',
    '/staffboard/orders': 'Today Orders',
    '/draw-map': 'Robot Controller',
    '/staffboard/draw-map': 'Robot Controller',
  };
  const pageTitle = pageTitles[location.pathname] || 'Staffboard';

  return (
    <div className="min-h-screen bg-background text-foreground font-body">
      <div className="relative flex min-h-screen">
        <Sidebar open={sidebarOpen} onClose={() => setSidebarOpen(false)} />

        <main className="flex-1 flex flex-col min-w-0">
          <header className="sticky top-0 z-30 bg-white/90 backdrop-blur-md border-b border-slate-200 px-4 md:px-6 h-16 flex items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <button
                onClick={() => setSidebarOpen(true)}
                className="lg:hidden p-2 text-slate-500 hover:text-slate-800 transition-colors cursor-pointer"
              >
                <Menu size={20} />
              </button>
              <div className="hidden sm:flex items-center gap-2 px-3 py-2 rounded-lg bg-slate-100 border border-slate-200 min-w-[200px] md:min-w-[280px]">
                <svg className="w-3.5 h-3.5 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
                <input
                  type="text"
                  placeholder="Search orders, tables..."
                  className="bg-transparent border-none outline-none text-sm text-slate-600 placeholder:text-slate-400 w-full font-body"
                />
              </div>
            </div>

            <div className="flex items-center gap-2 md:gap-3">
              <button className="relative p-2 rounded-lg text-slate-500 hover:text-slate-800 hover:bg-slate-100 transition-all cursor-pointer">
                <Bell size={18} />
                <span className="absolute -top-0.5 -right-0.5 w-4 h-4 rounded-full bg-accent text-[9px] font-bold text-white flex items-center justify-center font-heading">
                  3
                </span>
              </button>
              <div className="flex items-center gap-2 pl-2 md:pl-3 border-l border-slate-200">
                <button onClick={() => { dispatch(logout()); navigate('/login'); }} className="flex items-center gap-2 cursor-pointer">
                  <div className="w-8 h-8 rounded-full bg-accentlight flex items-center justify-center text-accent text-xs font-bold font-heading">
                    {user?.fullName ? user.fullName.split(' ').map((n: string) => n[0]).join('').slice(0, 2).toUpperCase() : '?'}
                  </div>
                  <div className="hidden md:block text-left">
                    <p className="text-sm font-medium text-slate-700 font-body leading-tight">{user?.fullName || 'User'}</p>
                    <p className="text-[10px] text-slate-400 font-body">{user?.role === 'MANAGER' ? 'Manager' : 'Staff'}</p>
                  </div>
                </button>
              </div>
            </div>
          </header>

          <div className="flex-1 p-4 md:p-6 lg:p-8 space-y-6 overflow-y-auto scrollbar-thin">
            <div className="flex items-center justify-between">
              <h1 className="text-xl md:text-2xl font-bold text-slate-800 font-heading">{pageTitle}</h1>
              <div className="flex items-center gap-2 text-xs text-slate-400 font-body">
                <Clock size={12} />
                <span>{new Date().toLocaleTimeString()}</span>
              </div>
            </div>

            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}

export default StaffDashboard;
