import { Card, Form, Input, InputNumber, Select } from 'antd';
import type { MapObject, ObjectType } from '@/types/map';

const objectTypeOptions: { value: ObjectType; label: string }[] = [
  { value: 'table', label: 'Table' },
  { value: 'chair', label: 'Chair' },
  { value: 'wall', label: 'Wall' },
  { value: 'kitchen', label: 'Kitchen' },
  { value: 'delivery', label: 'Delivery Point' },
  { value: 'charging', label: 'Charging Station' },
  { value: 'restricted', label: 'Restricted Area' },
  { value: 'door', label: 'Door' },
  { value: 'robot', label: 'Robot Start Position' },
];

const defaultObject: MapObject & { tableNumber: number } = {
  id: 'table-4',
  name: 'Table 04',
  type: 'table',
  x: 12.4,
  y: 8.2,
  rotation: 45,
  width: 1.2,
  height: 1.2,
  tableNumber: 4,
};

interface PropertyPanelProps {
  selectedObject?: MapObject | null;
}

export function PropertyPanel({ selectedObject }: PropertyPanelProps) {
  const object = selectedObject
    ? {
        ...selectedObject,
        tableNumber:
          selectedObject.type === 'table' ? Number(selectedObject.name.match(/\d+/)?.[0] ?? 4) : 4,
      }
    : defaultObject;

  return (
    <Card title="Property Panel" className="shadow-sm" styles={{ body: { paddingBottom: 8 } }}>
      <Form layout="vertical" initialValues={object} key={object.id} requiredMark={false}>
        <Form.Item label="Name" name="name">
          <Input />
        </Form.Item>
        <Form.Item label="Type" name="type">
          <Select options={objectTypeOptions} />
        </Form.Item>
        <div className="grid grid-cols-2 gap-3">
          <Form.Item label="X Position" name="x">
            <InputNumber className="w-full" step={0.1} />
          </Form.Item>
          <Form.Item label="Y Position" name="y">
            <InputNumber className="w-full" step={0.1} />
          </Form.Item>
        </div>
        <Form.Item label="Rotation" name="rotation">
          <InputNumber className="w-full" min={0} max={360} step={1} />
        </Form.Item>
        <div className="grid grid-cols-2 gap-3">
          <Form.Item label="Width" name="width">
            <InputNumber className="w-full" min={0.1} step={0.1} />
          </Form.Item>
          <Form.Item label="Height" name="height">
            <InputNumber className="w-full" min={0.1} step={0.1} />
          </Form.Item>
        </div>
        <Form.Item label="Table Number" name="tableNumber">
          <InputNumber className="w-full" min={1} step={1} />
        </Form.Item>
      </Form>
    </Card>
  );
}
