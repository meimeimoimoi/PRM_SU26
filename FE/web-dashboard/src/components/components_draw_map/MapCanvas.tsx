import { Card, Tag, Typography } from 'antd';
import { BatteryCharging, Bot, ChefHat, ShieldAlert, Square } from 'lucide-react';
import type { MapObject, MapObjectType } from '@/types/map';
import { useMapStore } from '@/store/mapStore';
import { Toolbox } from './Toolbox';

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
      onClick={(event) => {
        event.stopPropagation();
        onSelect(object.id);
      }}
    >
      {getObjectIcon(object.type)}
      <span>{object.name}</span>
    </button>
  );
}

export function MapCanvas() {
  const objects = useMapStore((state) => state.objects);
  const selectedObjectId = useMapStore((state) => state.selectedObjectId);
  const selectedTool = useMapStore((state) => state.selectedTool);
  const setSelectedObject = useMapStore((state) => state.setSelectedObject);

  return (
    <Card className="map-canvas-card" styles={{ body: { padding: 0, height: '100%' } }}>
      <Toolbox />

      <div className="canvas-status">
        <Typography.Text strong>Canvas</Typography.Text>
        <Tag color="blue">{toolLabels[selectedTool]}</Tag>
      </div>

      <div className="map-canvas-grid" onClick={() => setSelectedObject(null)}>
        <div className="canvas-path-placeholder" />
        <div className="canvas-zoom-placeholder">Zoom and path visualization placeholders</div>

        {objects.map((object) => (
          <MapObjectShape
            key={object.id}
            object={object}
            selected={selectedObjectId === object.id}
            onSelect={setSelectedObject}
          />
        ))}
      </div>
    </Card>
  );
}
