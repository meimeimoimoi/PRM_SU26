import { Button, Space, Tooltip, Typography } from 'antd';
import {
  Download,
  FileDown,
  FolderOpen,
  Map,
  Navigation,
  Redo2,
  RotateCcw,
  Save,
  Settings,
} from 'lucide-react';

interface ToolbarProps {
  onExportPGM: () => void;
  onExportWaypoints: () => void;
  onValidateNavigation: () => void;
  onGeneratePath: () => void;
}

const iconSize = 17;

export function Toolbar({
  onExportPGM,
  onExportWaypoints,
  onValidateNavigation,
  onGeneratePath,
}: ToolbarProps) {
  return (
    <div className="flex h-16 items-center justify-between gap-4 border-b border-slate-200 bg-white px-5 shadow-sm">
      <div className="flex min-w-0 items-center gap-3">
        <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-[#1677ff] text-white">
          <Map size={20} />
        </div>
        <Typography.Title level={4} className="!mb-0 whitespace-nowrap !text-slate-900">
          Restaurant Map Designer
        </Typography.Title>
      </div>

      <Space size={8} wrap className="min-w-0 justify-center">
        <Button icon={<Save size={iconSize} />}>Save</Button>
        <Button icon={<FolderOpen size={iconSize} />}>Load</Button>
        <Button icon={<Download size={iconSize} />} onClick={onExportPGM}>
          Export PGM
        </Button>
        <Button icon={<FileDown size={iconSize} />} onClick={onExportWaypoints}>
          Export Waypoints
        </Button>
        <Button icon={<Navigation size={iconSize} />} onClick={onValidateNavigation}>
          Validate Navigation
        </Button>
        <Button type="primary" icon={<RotateCcw size={iconSize} />} onClick={onGeneratePath}>
          Generate Path
        </Button>
      </Space>

      <Space size={6}>
        <Tooltip title="Undo">
          <Button aria-label="Undo" icon={<RotateCcw size={iconSize} />} />
        </Tooltip>
        <Tooltip title="Redo">
          <Button aria-label="Redo" icon={<Redo2 size={iconSize} />} />
        </Tooltip>
        <Tooltip title="Settings">
          <Button aria-label="Settings" icon={<Settings size={iconSize} />} />
        </Tooltip>
      </Space>
    </div>
  );
}
