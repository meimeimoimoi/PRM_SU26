import { Card } from 'antd';
import {
  Armchair,
  BatteryCharging,
  CircleDot,
  DoorOpen,
  MapPin,
  Minus,
  Navigation,
  Square,
  Utensils,
} from 'lucide-react';
import type { LucideIcon } from 'lucide-react';
import type { ObjectType } from '@/types/map';

interface ToolboxItem {
  type: ObjectType;
  label: string;
  Icon: LucideIcon;
}

const toolboxItems: ToolboxItem[] = [
  { type: 'table', label: 'Table', Icon: Square },
  { type: 'chair', label: 'Chair', Icon: Armchair },
  { type: 'delivery', label: 'Delivery Point', Icon: MapPin },
  { type: 'kitchen', label: 'Kitchen', Icon: Utensils },
  { type: 'charging', label: 'Charging Station', Icon: BatteryCharging },
  { type: 'wall', label: 'Wall', Icon: Minus },
  { type: 'restricted', label: 'Restricted Area', Icon: CircleDot },
  { type: 'door', label: 'Door', Icon: DoorOpen },
  { type: 'robot', label: 'Robot Start Position', Icon: Navigation },
];

export function Toolbox() {
  return (
    <Card title="Object Toolbox" className="shadow-sm" styles={{ body: { padding: 12 } }}>
      <div className="grid grid-cols-1 gap-2">
        {toolboxItems.map(({ type, label, Icon }) => (
          <button
            key={type}
            draggable
            type="button"
            className="flex h-11 items-center gap-3 rounded-lg border border-slate-200 bg-white px-3 text-left text-sm font-medium text-slate-700 transition hover:border-[#1677ff] hover:bg-blue-50 hover:text-[#1677ff]"
          >
            <Icon size={18} />
            <span>{label}</span>
          </button>
        ))}
      </div>
    </Card>
  );
}
