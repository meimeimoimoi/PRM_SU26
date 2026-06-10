import { Avatar, Button, Space, Tooltip, Typography } from 'antd';
import {
  Download,
  FolderOpen,
  HelpCircle,
  Map,
  Minus,
  Navigation,
  Plus,
  Route,
  RotateCcw,
  Save,
  Settings,
} from 'lucide-react';
import { useMapStore } from '@/store/mapStore';

const MIN_ZOOM = 0.25;
const MAX_ZOOM = 3;
const ZOOM_STEP = 0.15;

export function Toolbar() {
  const zoom = useMapStore((s) => s.zoom);
  const setZoom = useMapStore((s) => s.setZoom);

  return (
    <div className="top-toolbar">
      <div className="toolbar-brand">
        <div className="toolbar-logo">
          <Map size={20} />
        </div>
        <Typography.Title level={4} className="toolbar-title">
          Restaurant Map Designer
        </Typography.Title>
      </div>

      <Space size={8} wrap className="toolbar-actions">
        <Button icon={<Save size={16} />}>Save</Button>
        <Button icon={<FolderOpen size={16} />}>Load</Button>
        <Button icon={<Download size={16} />}>Export</Button>
        <Button type="primary" icon={<Navigation size={16} />}>
          Validate Navigation
        </Button>
        <Button type="primary" icon={<Route size={16} />}>
          Generate Path
        </Button>
      </Space>

      <Space size={4} className="toolbar-zoom">
        <Tooltip title="Zoom Out (scroll down)">
          <Button
            icon={<Minus size={15} />}
            onClick={() => setZoom(Math.max(MIN_ZOOM, +(zoom - ZOOM_STEP).toFixed(2)))}
            disabled={zoom <= MIN_ZOOM}
          />
        </Tooltip>
        <Button
          onClick={() => setZoom(1)}
          style={{ minWidth: 62, fontVariantNumeric: 'tabular-nums' }}
          title="Click to reset zoom"
        >
          {Math.round(zoom * 100)}%
        </Button>
        <Tooltip title="Zoom In (scroll up)">
          <Button
            icon={<Plus size={15} />}
            onClick={() => setZoom(Math.min(MAX_ZOOM, +(zoom + ZOOM_STEP).toFixed(2)))}
            disabled={zoom >= MAX_ZOOM}
          />
        </Tooltip>
        <Tooltip title="Reset Zoom">
          <Button icon={<RotateCcw size={15} />} onClick={() => setZoom(1)} />
        </Tooltip>
      </Space>

      <Space size={8} className="toolbar-meta">
        <Tooltip title="Settings">
          <Button aria-label="Settings" icon={<Settings size={17} />} />
        </Tooltip>
        <Tooltip title="Help">
          <Button aria-label="Help" icon={<HelpCircle size={17} />} />
        </Tooltip>
        <Avatar style={{ backgroundColor: '#1677ff' }}>RM</Avatar>
      </Space>
    </div>
  );
}
