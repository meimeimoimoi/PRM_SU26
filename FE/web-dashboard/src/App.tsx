import { Layout } from 'antd';
import { MapCanvas } from '@/components/components_draw_map/MapCanvas';
import { PropertyPanel } from '@/components/components_draw_map/PropertyPanel';
import { Toolbar } from '@/components/components_draw_map/Toolbar';
import '@/styles/mapDesigner.css';

const { Header, Content } = Layout;

function App() {
  return (
    <Layout className="restaurant-map-designer">
      <Header className="app-header">
        <Toolbar />
      </Header>

      <Layout className="app-body">
        <Content className="app-content">
          <MapCanvas />
        </Content>
        <PropertyPanel />
      </Layout>
    </Layout>
  );
}

export default App;
