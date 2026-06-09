import { Layout, message, Tabs } from 'antd';
import { MapCanvas } from '@/components/components_draw_map/MapCanvas';
import { PropertyPanel } from '@/components/components_draw_map/PropertyPanel';
import { Toolbar } from '@/components/components_draw_map/Toolbar';
import { Toolbox } from '@/components/components_draw_map/Toolbox';
import { useMapStore } from '@/store/mapStore';
import { astarService } from '@/utils/astar';
import { exportPGM } from '@/utils/exportPGM';
import { exportWaypoints } from '@/utils/exportWaypoints';

const { Header, Content, Sider } = Layout;

function App() {
  const objects = useMapStore((state) => state.objects);
  const selectedObject = useMapStore((state) => state.selectedObject);
  const [messageApi, contextHolder] = message.useMessage();

  const handleExportPGM = () => {
    const pgm = exportPGM(objects);
    messageApi.success(`PGM export generated (${pgm.length.toLocaleString()} characters)`);
  };

  const handleExportWaypoints = () => {
    const waypoints = exportWaypoints(objects);
    messageApi.success(`Waypoint export generated (${waypoints.split('\n').length} entries)`);
  };

  const handleValidateNavigation = () => {
    const result = astarService.validateRoute();
    messageApi.success(result.isValid ? 'Navigation route is valid' : 'Navigation warnings found');
  };

  const handleGeneratePath = () => {
    const path = astarService.findPath({ x: 347, y: 452 }, { x: 426, y: 166 });
    messageApi.success(`Generated mocked A* path with ${path.length} points`);
  };

  return (
    <Layout className="min-h-screen bg-[#f5f5f5]">
      {contextHolder}
      <Header className="!h-16 !bg-transparent !p-0">
        <Toolbar
          onExportPGM={handleExportPGM}
          onExportWaypoints={handleExportWaypoints}
          onValidateNavigation={handleValidateNavigation}
          onGeneratePath={handleGeneratePath}
        />
      </Header>

      <Layout className="bg-[#f5f5f5]">
        <Content className="p-4 lg:p-6">
          <div className="grid min-h-[calc(100vh-112px)] grid-cols-1 gap-4 xl:grid-cols-[minmax(0,1fr)_340px]">
            <main className="min-w-0">
              <MapCanvas />
            </main>

            <Sider
              width={340}
              breakpoint="xl"
              collapsedWidth={0}
              className="!max-w-none !bg-transparent"
              theme="light"
            >
              <Tabs
                defaultActiveKey="toolbox"
                items={[
                  {
                    key: 'toolbox',
                    label: 'Toolbox',
                    children: <Toolbox />,
                  },
                  {
                    key: 'properties',
                    label: 'Properties',
                    children: <PropertyPanel selectedObject={selectedObject} />,
                  },
                ]}
              />
            </Sider>
          </div>
        </Content>
      </Layout>
    </Layout>
  );
}

export default App;
