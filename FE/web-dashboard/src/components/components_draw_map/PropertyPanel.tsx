import { Card, Form, Input, InputNumber, Layout, Select, Tabs, Typography } from 'antd';
import type { TabsProps } from 'antd';
import type { MapObject, MapObjectType } from '@/types/map';
import { useMapStore } from '@/store/mapStore';

const { Sider } = Layout;

const objectTypeOptions: { value: MapObjectType; label: string }[] = [
  { value: 'table', label: 'Table' },
  { value: 'chair', label: 'Chair' },
  { value: 'wall', label: 'Wall' },
  { value: 'kitchen', label: 'Kitchen' },
  { value: 'delivery', label: 'Delivery Point' },
  { value: 'charging', label: 'Charging Station' },
  { value: 'restricted', label: 'Restricted Area' },
  { value: 'door', label: 'Door' },
  { value: 'robotStart', label: 'Robot Start Position' },
];

interface InspectorFormProps {
  selectedObject?: MapObject;
  onUpdate: (id: string, updates: Partial<MapObject>) => void;
}

function InspectorForm({ selectedObject, onUpdate }: InspectorFormProps) {
  if (!selectedObject) {
    return (
      <Card className="panel-card">
        <Typography.Text type="secondary">Select an object on the canvas to inspect it.</Typography.Text>
      </Card>
    );
  }

  return (
    <Card title="Inspector" className="panel-card">
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

export function PropertyPanel() {
  const objects = useMapStore((state) => state.objects);
  const selectedObjectId = useMapStore((state) => state.selectedObjectId);
  const updateObject = useMapStore((state) => state.updateObject);
  const selectedObject = objects.find((object) => object.id === selectedObjectId);

  const tabItems: TabsProps['items'] = [
    {
      key: 'toolbox',
      label: 'Object Toolbox',
      children: (
        <Card className="panel-card">
          <Typography.Paragraph>
            Use the floating toolbox on the canvas to select drawing tools.
          </Typography.Paragraph>
        </Card>
      ),
    },
    {
      key: 'layers',
      label: 'Layers',
      children: (
        <Card className="panel-card">
          <div className="layer-list">
            {objects.map((object) => (
              <div key={object.id} className={object.id === selectedObjectId ? 'layer-row active' : 'layer-row'}>
                <span>{object.name}</span>
                <Typography.Text type="secondary">{object.type}</Typography.Text>
              </div>
            ))}
          </div>
        </Card>
      ),
    },
    {
      key: 'inspector',
      label: 'Inspector',
      children: <InspectorForm selectedObject={selectedObject} onUpdate={updateObject} />,
    },
  ];

  return (
    <Sider width={320} className="property-panel" theme="light">
      <Card className="project-card">
        <Typography.Text type="secondary">Project Information</Typography.Text>
        <Typography.Title level={5}>Restaurant Navigation Map</Typography.Title>
        <Typography.Text type="secondary">{objects.length} objects in local state</Typography.Text>
      </Card>
      <Tabs defaultActiveKey="inspector" items={tabItems} />
    </Sider>
  );
}
