import React, { useEffect, useRef, useState } from 'react';
import { Alert, Button, Card, Col, Row, Select, Space, Statistic, Tag, Typography } from 'antd';
import {
  ArrowDown,
  ArrowLeft,
  ArrowRight,
  ArrowUp,
  Circle,
  Crosshair,
  Home,
  Navigation,
  Utensils,
} from 'lucide-react';
import { useMapStore } from '../../store/mapStore';
import { useSignalR } from '@/hooks/useSignalR';

interface Telemetry {
  x: number;
  y: number;
  theta: number;
  v: number;
  omega: number;
  status: string;
}

export const RobotConsole: React.FC = () => {
  const graphNodes = useMapStore((state) => state.graphNodes);
  // Lấy delivery nodes từ graph thay vì objects — để target gửi cho robot khớp với graph.json
  const deliveryNodes = graphNodes.filter((node) => node.type === 'delivery');
  const kitchenNodes = graphNodes.filter((node) => node.type === 'kitchen');
  const kitchenNode = kitchenNodes[0];

  const { invoke, on, connected } = useSignalR();
  const [selectedTable, setSelectedTable] = useState<string | undefined>(undefined);
  const [telemetry, setTelemetry] = useState<Telemetry>({
    x: 0,
    y: 0,
    theta: 0,
    v: 0,
    omega: 0,
    status: 'OFFLINE',
  });

  useEffect(() => {
    if (!connected) return;
    const cleanup = on('ReceiveRobotState', (...args: unknown[]) => {
      const data = args[0] as Telemetry;
      if (data && data.status !== 'OFFLINE') {
        setTelemetry(data);
      } else {
        setTelemetry((prev) => ({ ...prev, status: 'OFFLINE' }));
      }
    });
    return cleanup;
  }, [on, connected]);

  const sendControlCommand = async (command: string, target?: string, direction?: string) => {
    try {
      await invoke('SendRobotCommand', command, target || 'NONE', direction || 'NONE');
    } catch (_error) {
    }
  };

  const handleNavigate = () => {
    if (selectedTable) {
      sendControlCommand('NAV_TO_TABLE', selectedTable);
    }
  };

  const handleReturnHome = () => {
    sendControlCommand('NAV_TO_TABLE', 'robotStart');
  };

  const handleReturnToKitchen = () => {
    if (kitchenNode) {
      sendControlCommand('NAV_TO_TABLE', kitchenNode.id);
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'IDLE':
        return 'blue';
      case 'NAV_TO_TABLE':
        return 'green';
      case 'MANUAL_MOVE':
        return 'orange';
      case 'RETURN_TO_KITCHEN':
        return 'gold';
      case 'ARRIVED_TABLE':
        return 'cyan';
      case 'ARRIVED_KITCHEN':
        return 'purple';
      case 'OFFLINE':
      default:
        return 'red';
    }
  };

  const getStatusLabel = (status: string) => {
    switch (status) {
      case 'IDLE': return '⏸️ Chờ lệnh';
      case 'NAV_TO_TABLE': return '🚚 Đang giao hàng';
      case 'MANUAL_MOVE': return '🕹️ Thủ công';
      case 'RETURN_TO_KITCHEN': return '🔄 Đang về bếp';
      case 'ARRIVED_TABLE': return '✅ Đã đến bàn';
      case 'ARRIVED_KITCHEN': return '✅ Đã về bếp';
      case 'OFFLINE': return '🔴 Offline';
      default: return status;
    }
  };

  const isCalibrating = telemetry.status === 'RETURN_TO_KITCHEN';
  const isNavigating = telemetry.status === 'NAV_TO_TABLE';
  const isArrived = telemetry.status === 'ARRIVED_TABLE' || telemetry.status === 'ARRIVED_KITCHEN';
  const isOffline = telemetry.status === 'OFFLINE';

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>

      {/* Banner cảnh báo khi robot đang calibrating */}
      {isCalibrating && (
        <Alert
          type="warning"
          showIcon
          message="Đang xác định vị trí..."
          description="Robot đang chạy về điểm xuất phát để hiệu chỉnh bản đồ. Vị trí hiển thị trên UI là vị trí GPS thực tế (chấm vàng)."
          style={{ borderRadius: '8px' }}
        />
      )}

      {/* Màn hình trạng thái */}
      <Card title="Robot Telemetry" size="small" className="panel-card">
        <Row justify="space-between" align="middle" style={{ marginBottom: '12px' }}>
          <Typography.Text strong>Trạng thái:</Typography.Text>
          <Tag color={getStatusColor(telemetry.status)} style={{ fontWeight: 'bold' }}>
            {getStatusLabel(telemetry.status)}
          </Tag>
        </Row>
        
        <Row gutter={[16, 16]}>
          <Col span={12}>
            <Statistic title="Odom X (m)" value={telemetry.x} precision={2} />
          </Col>
          <Col span={12}>
            <Statistic title="Odom Y (m)" value={telemetry.y} precision={2} />
          </Col>
          <Col span={12}>
            <Statistic title="Vận tốc V (m/s)" value={telemetry.v} precision={2} />
          </Col>
          <Col span={12}>
            <Statistic title="Góc Omega (rad/s)" value={telemetry.omega} precision={2} />
          </Col>
        </Row>
      </Card>

      {/* Bảng chọn bàn giao hàng */}
      <Card title="Giao Hàng Tới Bàn" size="small" className="panel-card">
        <Space direction="vertical" style={{ width: '100%' }}>
          <Select
            placeholder="Chọn bàn ăn..."
            style={{ width: '100%' }}
            value={selectedTable}
            onChange={setSelectedTable}
            options={deliveryNodes.map((node) => ({
              value: node.id,   // ID graph node — robot dùng để lookup trong graph.json
              label: node.name, // Hiển thị tên thân thiện, vd "Table 101_Delivery"
            }))}
          />
          <Row gutter={8}>
            <Col span={8}>
              <Button
                type="primary"
                icon={<Navigation size={14} />}
                onClick={handleNavigate}
                disabled={!selectedTable || telemetry.status === 'OFFLINE'}
                block
              >
                Giao Hàng
              </Button>
            </Col>
            <Col span={8}>
              <Button
                icon={<Home size={14} />}
                onClick={handleReturnHome}
                disabled={telemetry.status === 'OFFLINE'}
                block
              >
                Về XP
              </Button>
            </Col>
            <Col span={8}>
              <Button
                icon={<Utensils size={14} />}
                onClick={handleReturnToKitchen}
                disabled={telemetry.status === 'OFFLINE' || !kitchenNode}
                block
              >
                Về Bếp
              </Button>
            </Col>
          </Row>
          <Row gutter={8} style={{ marginTop: '8px' }}>
            <Col span={24}>
              <Button
                icon={<Crosshair size={14} />}
                onClick={() => sendControlCommand('CALIBRATE')}
                disabled={telemetry.status === 'OFFLINE'}
                block
              >
                Hiệu Chuẩn (Calibrate)
              </Button>
            </Col>
          </Row>
        </Space>
      </Card>

      {/* D-Pad Lái Thủ Công */}
      <Card title="Lái Thủ Công (Manual)" size="small" className="panel-card">
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '8px' }}>
          {/* Hàng 1: Tiến */}
          <Button
            type="dashed"
            icon={<ArrowUp size={16} />}
            onMouseDown={() => sendControlCommand('MANUAL_MOVE', undefined, 'FORWARD')}
            onMouseUp={() => sendControlCommand('MANUAL_MOVE', undefined, 'STOP')}
            disabled={telemetry.status === 'OFFLINE'}
            style={{ width: '60px', height: '50px' }}
          />

          {/* Hàng 2: Trái - Dừng - Phải */}
          <Space>
            <Button
              type="dashed"
              icon={<ArrowLeft size={16} />}
              onMouseDown={() => sendControlCommand('MANUAL_MOVE', undefined, 'LEFT')}
              onMouseUp={() => sendControlCommand('MANUAL_MOVE', undefined, 'STOP')}
              disabled={telemetry.status === 'OFFLINE'}
              style={{ width: '60px', height: '50px' }}
            />
            <Button
              danger
              type="primary"
              icon={<Circle size={16} fill="white" />}
              onClick={() => sendControlCommand('STOP')}
              disabled={telemetry.status === 'OFFLINE'}
              style={{ width: '60px', height: '50px' }}
              title="Dừng Khẩn Cấp"
            />
            <Button
              type="dashed"
              icon={<ArrowRight size={16} />}
              onMouseDown={() => sendControlCommand('MANUAL_MOVE', undefined, 'RIGHT')}
              onMouseUp={() => sendControlCommand('MANUAL_MOVE', undefined, 'STOP')}
              disabled={telemetry.status === 'OFFLINE'}
              style={{ width: '60px', height: '50px' }}
            />
          </Space>

          {/* Hàng 3: Lùi */}
          <Button
            type="dashed"
            icon={<ArrowDown size={16} />}
            onMouseDown={() => sendControlCommand('MANUAL_MOVE', undefined, 'BACKWARD')}
            onMouseUp={() => sendControlCommand('MANUAL_MOVE', undefined, 'STOP')}
            disabled={telemetry.status === 'OFFLINE'}
            style={{ width: '60px', height: '50px' }}
          />
        </div>
      </Card>
    </div>
  );
};
