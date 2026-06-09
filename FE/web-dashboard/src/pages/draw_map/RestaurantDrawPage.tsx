import React from 'react';
import { MapCanvas } from '@/components/components_draw_map/MapCanvas';
import { Toolbar } from '@/components/components_draw_map/Toolbar';
import { PropertyPanel } from '@/components/components_draw_map/PropertyPanel';
import { Toolbox } from '@/components/components_draw_map/Toolbox';

const RestaurantDrawPage: React.FC = () => {
  return (
    <div className="restaurant-map-designer" style={{ height: '100vh', display: 'flex', flexDirection: 'column' }}>
      <Toolbar />
      <div style={{ flex: 1, display: 'flex' }}>
        <MapCanvas />
        <PropertyPanel />
      </div>
    </div>
  );
};

export default RestaurantDrawPage;
