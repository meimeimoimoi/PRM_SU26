import { useCallback, useEffect, useState } from 'react';
import { Button, Card, Empty, Form, Input, InputNumber, Select, Tabs, Typography } from 'antd';
import type { TabsProps } from 'antd';
import { Trash2, RotateCw } from 'lucide-react';
import type { GraphNode } from '@/types/graph';
import type { MapObject, MapObjectType } from '@/types/map';
import { useMapStore } from '@/store/mapStore';
import { RobotConsole } from './RobotConsole';

const objectTypeOptions: { value: MapObjectType; label: string }[] = [
  { value: 'table', label: 'Table' },
  { value: 'chair', label: 'Chair' },
  { value: 'wall', label: 'Wall' },
  { value: 'kitchen', label: 'Kitchen' },
  { value: 'charging', label: 'Charging Station' },
  { value: 'restricted', label: 'Restricted Area' },
  { value: 'robotStart', label: 'Robot Start Position' },
];

interface InspectorFormProps {
  selectedObject?: MapObject;
  onUpdate: (id: string, updates: Partial<MapObject>) => void;
  onDelete: (id: string) => void;
}

function InspectorForm({ selectedObject, onUpdate, onDelete }: InspectorFormProps) {
  if (!selectedObject) {
    return (
      <Card className="panel-card">
        <Empty description="Select an object on the canvas to inspect it." />
      </Card>
    );
  }

  return (
    <Card title="Inspector" className="panel-card" extra={
      <Button
        danger
        size="small"
        icon={<Trash2 size={14} />}
        onClick={() => onDelete(selectedObject.id)}
      >
        Delete
      </Button>
    }>
      <Form
        key={selectedObject.id}
        layout="vertical"
        initialValues={selectedObject}
        onValuesChange={(changedValues: Partial<MapObject>) => onUpdate(selectedObject.id, changedValues)}
      >
        <Form.Item label="Name" name="name">
          <Input />
        </Form.Item>
        <Form.Item label="Type" name="type">
          <Select options={objectTypeOptions} />
        </Form.Item>
        <Form.Item label="Rotation" name="rotation">
          <InputNumber className="full-width-control" min={0} max={360} step={1} />
        </Form.Item>
        <div className="property-grid">
          <Form.Item label="X Position" name="x">
            <InputNumber className="full-width-control" step={1} />
          </Form.Item>
          <Form.Item label="Y Position" name="y">
            <InputNumber className="full-width-control" step={1} />
          </Form.Item>
        </div>
        <div className="property-grid">
          <Form.Item label="Width" name="width">
            <InputNumber className="full-width-control" min={1} step={1} />
          </Form.Item>
          <Form.Item label="Height" name="height">
            <InputNumber className="full-width-control" min={1} step={1} />
          </Form.Item>
        </div>
        <Form.Item label="Table Number" name="tableNumber">
          <InputNumber className="full-width-control" min={1} step={1} />
        </Form.Item>
      </Form>
    </Card>
  );
}

function StartNodeInspector({ node }: { node: GraphNode }) {
  const updateGraphNode = useMapStore((s) => s.updateGraphNode);
  const removeGraphNode = useMapStore((s) => s.removeGraphNode);
  const [thetaDeg, setThetaDeg] = useState(((node.theta ?? 0) * 180) / Math.PI);

  useEffect(() => {
    setThetaDeg(((node.theta ?? 0) * 180) / Math.PI);
  }, [node.theta, node.id]);

  const commitTheta = useCallback(
    (deg: number | null) => {
      if (deg === null) return;
      const clamped = ((deg % 360) + 360) % 360;
      setThetaDeg(clamped);
      updateGraphNode(node.id, { theta: (clamped * Math.PI) / 180 });
    },
    [node.id, updateGraphNode],
  );

  return (
    <Card title="Start Position" className="panel-card" extra={
      <Button danger size="small" icon={<Trash2 size={14} />} onClick={() => removeGraphNode(node.id)}>Delete</Button>
    }>
      <Form layout="vertical">
        <div className="property-grid">
          <Form.Item label="X (m)" style={{ marginBottom: 8 }}>
            <InputNumber className="full-width-control" value={node.x} step={0.1} disabled />
          </Form.Item>
          <Form.Item label="Y (m)" style={{ marginBottom: 8 }}>
            <InputNumber className="full-width-control" value={node.y} step={0.1} disabled />
          </Form.Item>
        </div>
        <Form.Item label="Heading θ (degrees)" style={{ marginBottom: 4 }}>
          <InputNumber
            className="full-width-control"
            value={thetaDeg}
            min={0}
            max={360}
            step={1}
            formatter={(val) => val !== undefined ? `${Number(val).toFixed(1)}°` : ''}
            parser={(val) => parseFloat(val?.replace('°', '') || '0')}
            addonAfter={<RotateCw size={14} />}
            onChange={commitTheta}
            onStep={(_val, info) => {
              if (info.type === 'up') commitTheta(((thetaDeg + 1) % 360 + 360) % 360);
              if (info.type === 'down') commitTheta(((thetaDeg - 1) % 360 + 360) % 360);
            }}
          />
        </Form.Item>
        <Typography.Text type="secondary" style={{ fontSize: 11 }}>
          0° = East, 90° = North, {thetaDeg.toFixed(1)}° currently
        </Typography.Text>
      </Form>
    </Card>
  );
}

function ChargingNodeInspector({ node }: { node: GraphNode }) {
  const removeGraphNode = useMapStore((s) => s.removeGraphNode);

  return (
    <Card title="Charging Station" className="panel-card" extra={
      <Button danger size="small" icon={<Trash2 size={14} />} onClick={() => removeGraphNode(node.id)}>Delete</Button>
    }>
      <Form layout="vertical">
        <div className="property-grid">
          <Form.Item label="X (m)" style={{ marginBottom: 8 }}>
            <InputNumber className="full-width-control" value={node.x} step={0.1} disabled />
          </Form.Item>
          <Form.Item label="Y (m)" style={{ marginBottom: 8 }}>
            <InputNumber className="full-width-control" value={node.y} step={0.1} disabled />
          </Form.Item>
        </div>
      </Form>
    </Card>
  );
}

export function PropertyPanel() {
  const objects = useMapStore((s) => s.objects);
  const selectedObjectId = useMapStore((s) => s.selectedObjectId);
  const updateObject = useMapStore((s) => s.updateObject);
  const removeObject = useMapStore((s) => s.removeObject);
  const setSelectedObject = useMapStore((s) => s.setSelectedObject);
  const graphNodes = useMapStore((s) => s.graphNodes);
  const selectedGraphNodeId = useMapStore((s) => s.selectedGraphNodeId);
  const selectedObject = objects.find((o) => o.id === selectedObjectId);
  const selectedGraphNode = graphNodes.find((n) => n.id === selectedGraphNodeId);

  const tabItems: TabsProps['items'] = [
    {
      key: 'inspector',
      label: 'Inspector',
      children: selectedGraphNode?.type === 'robotStart'
        ? <StartNodeInspector node={selectedGraphNode} />
        : selectedGraphNode?.type === 'charging'
          ? <ChargingNodeInspector node={selectedGraphNode} />
          : <InspectorForm selectedObject={selectedObject} onUpdate={updateObject} onDelete={removeObject} />,
    },
    {
      key: 'console',
      label: 'Robot Console',
      children: <RobotConsole />,
    },
    {
      key: 'layers',
      label: 'Layers',
      children: (
        <Card className="panel-card">
          <div className="layer-list">
            {objects.map((obj) => (
              <div
                key={obj.id}
                className={obj.id === selectedObjectId ? 'layer-row active' : 'layer-row'}
                onClick={() => setSelectedObject(obj.id)}
                style={{ cursor: 'pointer' }}
              >
                <span>{obj.name}</span>
                <Typography.Text type="secondary">{obj.type}</Typography.Text>
              </div>
            ))}
          </div>
        </Card>
      ),
    },
  ];

  return (
    <div className="property-panel-inner">
      <Card className="project-card">
        <Typography.Text type="secondary">Project Information</Typography.Text>
        <Typography.Title level={5}>Restaurant Navigation Map</Typography.Title>
        <Typography.Text type="secondary">{objects.length} objects in local state</Typography.Text>
      </Card>
      <Tabs defaultActiveKey="inspector" items={tabItems} />
    </div>
  );
}
