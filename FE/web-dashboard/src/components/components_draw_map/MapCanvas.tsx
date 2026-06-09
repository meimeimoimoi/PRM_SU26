import { useMemo, useState } from 'react';
import { Badge, Typography } from 'antd';
import { BatteryCharging, Bot, ShieldAlert, Utensils } from 'lucide-react';
import type { MapObject, ObjectType } from '@/types/map';
import { useMapStore } from '@/store/mapStore';
import { astarService } from '@/utils/astar';

const objectStyles: Record<ObjectType, string> = {
  table: 'border-blue-500 bg-blue-50 text-blue-700',
  chair: 'border-cyan-500 bg-cyan-50 text-cyan-700',
  wall: 'border-slate-700 bg-slate-200 text-slate-800',
  kitchen: 'border-amber-500 bg-amber-50 text-amber-700',
  delivery: 'border-emerald-500 bg-emerald-50 text-emerald-700',
  charging: 'border-violet-500 bg-violet-50 text-violet-700',
  restricted: 'border-rose-500 bg-rose-50 text-rose-700',
  door: 'border-teal-500 bg-teal-50 text-teal-700',
  robot: 'border-indigo-500 bg-indigo-50 text-indigo-700',
};

function objectIcon(type: ObjectType) {
  if (type === 'kitchen') return <Utensils size={18} />;
  if (type === 'charging') return <BatteryCharging size={18} />;
  if (type === 'restricted') return <ShieldAlert size={18} />;
  if (type === 'robot') return <Bot size={18} />;
  return null;
}

function MapObjectView({ object, selected }: { object: MapObject; selected: boolean }) {
  const selectObject = useMapStore((state) => state.selectObject);

  return (
    <button
      type="button"
      onClick={() => selectObject(object.id)}
      className={`absolute flex items-center justify-center rounded-lg border-2 p-2 text-xs font-semibold shadow-sm transition hover:shadow-md ${objectStyles[object.type]} ${
        selected ? 'ring-2 ring-[#1677ff] ring-offset-2' : ''
      }`}
      style={{
        left: object.x,
        top: object.y,
        width: object.width,
        height: object.height,
        transform: `rotate(${object.rotation}deg)`,
      }}
    >
      <span className="flex max-w-full flex-col items-center gap-1 overflow-hidden text-center leading-tight">
        {objectIcon(object.type)}
        <span className="w-full truncate">{object.name}</span>
      </span>
    </button>
  );
}

export function MapCanvas() {
  const [zoom, setZoom] = useState(1);
  const [pan, setPan] = useState({ x: 0, y: 0 });
  const [coordinates, setCoordinates] = useState({ x: 0, y: 0 });
  const objects = useMapStore((state) => state.objects);
  const selectedObject = useMapStore((state) => state.selectedObject);

  const robotPath = useMemo(
    () => astarService.findPath({ x: 347, y: 452 }, { x: 426, y: 166 }),
    [],
  );

  return (
    <div className="relative h-full min-h-[560px] overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm">
      <div className="absolute left-4 top-4 z-20 flex items-center gap-2 rounded-lg border border-slate-200 bg-white px-3 py-2 shadow-sm">
        <Badge status="processing" />
        <Typography.Text className="!text-xs !text-slate-600">
          X {coordinates.x.toFixed(0)} / Y {coordinates.y.toFixed(0)}
        </Typography.Text>
      </div>

      <div className="absolute right-4 top-4 z-20 flex items-center overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm">
        <button
          type="button"
          className="h-9 w-10 text-sm font-semibold text-slate-700"
          onClick={() => setZoom((value) => Math.max(0.5, value - 0.1))}
        >
          -
        </button>
        <div className="w-16 border-x border-slate-200 text-center text-xs font-semibold text-slate-600">
          {Math.round(zoom * 100)}%
        </div>
        <button
          type="button"
          className="h-9 w-10 text-sm font-semibold text-slate-700"
          onClick={() => setZoom((value) => Math.min(1.8, value + 0.1))}
        >
          +
        </button>
      </div>

      <div
        className="h-full w-full cursor-grab active:cursor-grabbing"
        onMouseMove={(event) => {
          const bounds = event.currentTarget.getBoundingClientRect();
          setCoordinates({
            x: (event.clientX - bounds.left - pan.x) / zoom,
            y: (event.clientY - bounds.top - pan.y) / zoom,
          });
        }}
        onWheel={(event) => {
          setZoom((value) => Math.max(0.5, Math.min(1.8, value - event.deltaY * 0.001)));
        }}
        onDoubleClick={() => setPan({ x: pan.x + 20, y: pan.y + 20 })}
        style={{
          backgroundColor: '#ffffff',
          backgroundImage:
            'linear-gradient(#e5e7eb 1px, transparent 1px), linear-gradient(90deg, #e5e7eb 1px, transparent 1px)',
          backgroundSize: `${20 * zoom}px ${20 * zoom}px`,
          backgroundPosition: `${pan.x}px ${pan.y}px`,
        }}
      >
        <div
          className="relative h-[900px] w-[1200px] origin-top-left"
          style={{ transform: `translate(${pan.x}px, ${pan.y}px) scale(${zoom})` }}
        >
          <svg className="pointer-events-none absolute inset-0 h-full w-full overflow-visible">
            <polyline
              fill="none"
              points={robotPath.map((point) => `${point.x},${point.y}`).join(' ')}
              stroke="#1677ff"
              strokeDasharray="8 8"
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth="4"
            />
          </svg>

          {objects.map((object) => (
            <MapObjectView key={object.id} object={object} selected={selectedObject?.id === object.id} />
          ))}
        </div>
      </div>
    </div>
  );
}
