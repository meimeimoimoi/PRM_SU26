// src/components/components_draw_map/Toolbar.tsx
import { useMemo } from 'react';
import { Alert, Button, Popconfirm, message } from 'antd';
import { useMapStore } from '../../store/mapStore';
import { exportWaypoints } from '../../utils/exportWaypoints';
import { exportGraph } from '../../utils/exportGraph';
import { validateGraph, getGraphValidationSummary } from '../../utils/validateGraph';
import { pixelToWorld } from '../../utils/coordinateUtils';

export const Toolbar = () => {
  const objects = useMapStore((state) => state.objects);
  const graphNodes = useMapStore((state) => state.graphNodes);
  const graphEdges = useMapStore((state) => state.graphEdges);
  const resetMap = useMapStore((state) => state.resetMap);
  const floorSize = useMapStore((s: any) => s.floorSize) || 20;
  const resolution = useMapStore((s: any) => s.resolution) || 0.05;

  const graphValidation = useMemo(() => validateGraph(objects, graphNodes, graphEdges, floorSize, resolution), [objects, graphNodes, graphEdges, floorSize, resolution]);
  const graphValidationSummary = useMemo(() => getGraphValidationSummary(graphValidation), [graphValidation]);

  const handleSendToRobot = async () => {
    const startNode = graphNodes.find((n) => n.type === 'robotStart');
    const startObj = objects.find((obj) => obj.type === 'robotStart');
    let robot_start_world_x = 0;
    let robot_start_world_y = 0;
    let robot_start_world_theta = 0;

    if (startNode) {
      robot_start_world_x = startNode.x;
      robot_start_world_y = startNode.y;
      robot_start_world_theta = startNode.theta ?? 0;
    } else if (startObj) {
      const startPx = startObj.x + startObj.width / 2;
      const startPy = startObj.y + startObj.height / 2;
      const worldPos = pixelToWorld(startPx, startPy, floorSize, resolution);
      robot_start_world_x = worldPos.x;
      robot_start_world_y = worldPos.y;
    }

    const waypointsText = exportWaypoints(graphNodes, floorSize, resolution);
    const graphText = exportGraph(graphNodes, graphEdges, floorSize, resolution);
    const payload = {
      floorSize,
      resolution,
      robot_start_world_x,
      robot_start_world_y,
      robot_start_world_theta,
      objects,
      graph: graphText,
      waypoints: waypointsText,
    };
    try {
      const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5000/api/v1';
      const response = await fetch(`${API_BASE}/maps`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });
      let result;
      if (!response.ok) {
        const text = await response.text();
        throw new Error(`Server responded ${response.status}: ${text}`);
      }
      result = await response.json();
      if (result.id) {
        message.success('Dữ liệu đã gửi đến robot thành công!');
      } else {
        message.error('Lỗi: ' + (result.error || 'Unknown error'));
      }
    } catch (error) {
      message.error('Không thể kết nối đến server');
    }
  };

  return (
    <div className="toolbar">
      <div style={{ display: 'flex', flexDirection: 'column', gap: 10, width: '100%' }}>
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, alignItems: 'center' }}>
          <Button
            type="primary"
            size="large"
            onClick={handleSendToRobot}
            style={{
              height: 44,
              fontSize: 15,
              fontWeight: 600,
              padding: '0 24px',
              boxShadow: '0 2px 8px rgba(24,144,255,0.4)',
            }}
          >
            Gửi dữ liệu đến robot
          </Button>
          <Popconfirm
            title="Reset map?"
            description="Xóa toàn bộ state hiện tại, bao gồm objects, graph nodes, graph edges và dữ liệu lưu local."
            okText="Reset"
            cancelText="Cancel"
            okButtonProps={{ danger: true }}
            onConfirm={resetMap}
          >
            <Button danger>Reset map</Button>
          </Popconfirm>
        </div>
        <Alert
          showIcon
          type={graphValidation.valid ? 'success' : 'error'}
          message={graphValidation.valid ? 'Graph hợp lệ' : `Graph có ${graphValidationSummary.totalIssues} lỗi`}
          description={
            graphValidation.valid
              ? 'Node, edge, robotStart và đường đi tới table đều hợp lệ.'
              : (
                <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                  {graphValidation.issues.slice(0, 5).map((issue, index) => (
                    <div key={`${issue.type}-${issue.nodeId || issue.edgeId || index}`}>
                      {issue.message}
                    </div>
                  ))}
                  {graphValidation.issues.length > 5 && (
                    <div>… và {graphValidation.issues.length - 5} lỗi nữa.</div>
                  )}
                </div>
              )
          }
        />
      </div>
    </div>
  );
};