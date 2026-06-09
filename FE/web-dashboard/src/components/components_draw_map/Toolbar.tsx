import { Avatar, Button, Space, Tooltip, Typography } from 'antd';
import {
  Download,
  FolderOpen,
  HelpCircle,
  Map,
  Navigation,
  Route,
  Save,
  Settings,
} from 'lucide-react';

export function Toolbar() {
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
