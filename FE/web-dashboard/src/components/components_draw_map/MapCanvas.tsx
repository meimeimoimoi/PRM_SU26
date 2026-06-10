import React, { useCallback, useRef, useState } from 'react';
import { Tag, Typography } from 'antd';
import { BatteryCharging, Bot, ChefHat, ShieldAlert, Square } from 'lucide-react';
import type { MapObject, MapObjectType } from '@/types/map';
import { useMapStore } from '@/store/mapStore';
import { Toolbox } from './Toolbox';

const MIN_ZOOM = 0.25;
const MAX_ZOOM = 3;
const ZOOM_STEP = 0.1;

const toolLabels: Record<string, string> = {
  select: 'Select',
  pan: 'Pan',
  table: 'Table',
  chair: 'Chair',
  wall: 'Wall',
  kitchen: 'Kitchen',
  delivery: 'Delivery Point',
  charging: 'Charging Station',
  restricted: 'Restricted Area',
  door: 'Door',
  robotStart: 'Start Position',
};

function getObjectIcon(type: MapObjectType) {
  if (type === 'kitchen') return <ChefHat size={18} />;
  if (type === 'charging') return <BatteryCharging size={18} />;
  if (type === 'restricted') return <ShieldAlert size={18} />;
  if (type === 'robotStart') return <Bot size={18} />;
  return <Square size={18} />;
}

interface MapObjectShapeProps {
  object: MapObject;
  selected: boolean;
  onSelect: (id: string) => void;
}

function MapObjectShape({ object, selected, onSelect }: MapObjectShapeProps) {
  const isWall = object.type === 'wall';

  return (
    <button
      type="button"
      className={`map-object map-object-${object.type} ${selected ? 'map-object-selected' : ''}`}
      style={{
        left: object.x,
        top: object.y,
        width: object.width,
        height: object.height,
        transform: `rotate(${object.rotation}deg)`,
      }}
      onClick={(e) => {
        e.stopPropagation();
        onSelect(object.id);
      }}
    >
      {isWall ? null : getObjectIcon(object.type)}
      <span>{object.name}</span>
    </button>
  );
}

let objectCounter = 100;

export function MapCanvas() {
  const objects = useMapStore((s) => s.objects);
  const selectedObjectId = useMapStore((s) => s.selectedObjectId);
  const selectedTool = useMapStore((s) => s.selectedTool);
  const zoom = useMapStore((s) => s.zoom);
  const setZoom = useMapStore((s) => s.setZoom);
  const setSelectedObject = useMapStore((s) => s.setSelectedObject);
  const addObject = useMapStore((s) => s.addObject);
  const removeObject = useMapStore((s) => s.removeObject);

  const containerRef = useRef<HTMLDivElement>(null);
  const [pan, setPan] = useState({ x: 0, y: 0 });
  const isPanning = useRef(false);
  const panStart = useRef({ x: 0, y: 0, panX: 0, panY: 0 });
  const spaceHeld = useRef(false);

  // Zoom via mouse wheel
  const handleWheel = useCallback(
    (e: React.WheelEvent) => {
      e.preventDefault();
      const direction = e.deltaY < 0 ? 1 : -1;
      const next = Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, +(zoom + direction * ZOOM_STEP).toFixed(2)));
      setZoom(next);
    },
    [zoom, setZoom],
  );

  // Pan: middle-mouse or Space+drag
  const handlePointerDown = useCallback(
    (e: React.PointerEvent) => {
      if (e.button === 1 || (e.button === 0 && spaceHeld.current) || selectedTool === 'pan') {
        isPanning.current = true;
        panStart.current = { x: e.clientX, y: e.clientY, panX: pan.x, panY: pan.y };
        (e.target as HTMLElement).setPointerCapture(e.pointerId);
        e.preventDefault();
      }
    },
    [pan, selectedTool],
  );

  const handlePointerMove = useCallback((e: React.PointerEvent) => {
    if (!isPanning.current) return;
    const dx = e.clientX - panStart.current.x;
    const dy = e.clientY - panStart.current.y;
    setPan({ x: panStart.current.panX + dx, y: panStart.current.panY + dy });
  }, []);

  const handlePointerUp = useCallback(() => {
    isPanning.current = false;
  }, []);

  // Space key for pan mode
  React.useEffect(() => {
    const down = (e: KeyboardEvent) => {
      if (e.code === 'Space' && !e.repeat) {
        spaceHeld.current = true;
      }
      if (e.code === 'Delete' || e.code === 'Backspace') {
        const active = document.activeElement;
        if (active && (active.tagName === 'INPUT' || active.tagName === 'TEXTAREA')) return;
        const selId = useMapStore.getState().selectedObjectId;
        if (selId) removeObject(selId);
      }
    };
    const up = (e: KeyboardEvent) => {
      if (e.code === 'Space') spaceHeld.current = false;
    };
    window.addEventListener('keydown', down);
    window.addEventListener('keyup', up);
    return () => {
      window.removeEventListener('keydown', down);
      window.removeEventListener('keyup', up);
    };
  }, [removeObject]);

  // Click on canvas to add object
  const handleCanvasClick = useCallback(
    (e: React.MouseEvent) => {
      if (isPanning.current) return;

      // Deselect
      if (selectedTool === 'select' || selectedTool === 'pan') {
        setSelectedObject(null);
        return;
      }

      const rect = containerRef.current?.getBoundingClientRect();
      if (!rect) return;

      const x = (e.clientX - rect.left - pan.x) / zoom;
      const y = (e.clientY - rect.top - pan.y) / zoom;

      if (selectedTool === 'table') {
        objectCounter++;
        addObject({
          id: `table-${objectCounter}`,
          type: 'table',
          name: `Table ${objectCounter}`,
          x: Math.round(x - 60),
          y: Math.round(y - 40),
          width: 120,
          height: 80,
          rotation: 0,
        });
      } else if (selectedTool === 'wall') {
        objectCounter++;
        addObject({
          id: `wall-${objectCounter}`,
          type: 'wall',
          name: `Wall ${objectCounter}`,
          x: Math.round(x - 100),
          y: Math.round(y - 10),
          width: 200,
          height: 20,
          rotation: 0,
        });
      }
    },
    [selectedTool, zoom, pan, setSelectedObject, addObject],
  );

  const cursorClass =
    selectedTool === 'pan' || spaceHeld.current
      ? 'canvas-cursor-grab'
      : selectedTool === 'table' || selectedTool === 'wall'
        ? 'canvas-cursor-crosshair'
        : '';

  return (
    <div className="map-canvas-card">
      <Toolbox />

      <div className="canvas-status">
        <Typography.Text strong>Canvas</Typography.Text>
        <Tag color="blue">{toolLabels[selectedTool]}</Tag>
        <Tag>{Math.round(zoom * 100)}%</Tag>
      </div>

      <div
        ref={containerRef}
        className={`map-canvas-viewport ${cursorClass}`}
        onWheel={handleWheel}
        onPointerDown={handlePointerDown}
        onPointerMove={handlePointerMove}
        onPointerUp={handlePointerUp}
        onClick={handleCanvasClick}
      >
        <div
          className="map-canvas-grid"
          style={{
            transform: `translate(${pan.x}px, ${pan.y}px) scale(${zoom})`,
            transformOrigin: '0 0',
          }}
        >
          {objects.map((obj) => (
            <MapObjectShape
              key={obj.id}
              object={obj}
              selected={selectedObjectId === obj.id}
              onSelect={setSelectedObject}
            />
          ))}
        </div>
      </div>
    </div>
  );
}
