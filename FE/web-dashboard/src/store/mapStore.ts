import { create } from 'zustand';
import type { MapObject, MapTool } from '@/types/map';

interface MapState {
  selectedTool: MapTool;
  selectedObjectId: string | null;
  objects: MapObject[];
  zoom: number;
  setSelectedTool: (tool: MapTool) => void;
  setSelectedObject: (id: string | null) => void;
  addObject: (object: MapObject) => void;
  updateObject: (id: string, updates: Partial<MapObject>) => void;
  removeObject: (id: string) => void;
  setZoom: (zoom: number) => void;
}

const initialObjects: MapObject[] = [
  {
    id: 'kitchen-1',
    name: 'Kitchen Area',
    type: 'kitchen',
    x: 60,
    y: 70,
    width: 220,
    height: 140,
    rotation: 0,
  },
  {
    id: 'table-1',
    name: 'Table 01',
    type: 'table',
    x: 380,
    y: 120,
    width: 92,
    height: 92,
    rotation: 0,
    tableNumber: 1,
  },
  {
    id: 'table-2',
    name: 'Table 02',
    type: 'table',
    x: 560,
    y: 120,
    width: 92,
    height: 92,
    rotation: 0,
    tableNumber: 2,
  },
  {
    id: 'table-4',
    name: 'Table 04',
    type: 'table',
    x: 470,
    y: 300,
    width: 92,
    height: 92,
    rotation: 45,
    tableNumber: 4,
  },
  {
    id: 'restricted-1',
    name: 'Restricted Area',
    type: 'restricted',
    x: 720,
    y: 240,
    width: 170,
    height: 130,
    rotation: 0,
  },
  {
    id: 'robot-1',
    name: 'Robot Start Position',
    type: 'robotStart',
    x: 315,
    y: 420,
    width: 64,
    height: 64,
    rotation: 0,
  },
  {
    id: 'charging-1',
    name: 'Charging Station',
    type: 'charging',
    x: 80,
    y: 420,
    width: 110,
    height: 76,
    rotation: 0,
  },
];

export const useMapStore = create<MapState>((set) => ({
  selectedTool: 'select',
  selectedObjectId: 'table-4',
  objects: initialObjects,
  zoom: 1,
  setSelectedTool: (tool) => set({ selectedTool: tool }),
  setSelectedObject: (id) => set({ selectedObjectId: id }),
  addObject: (object) =>
    set((state) => ({
      objects: [...state.objects, object],
      selectedObjectId: object.id,
    })),
  updateObject: (id, updates) =>
    set((state) => ({
      objects: state.objects.map((obj) =>
        obj.id === id ? { ...obj, ...updates } : obj,
      ),
    })),
  removeObject: (id) =>
    set((state) => ({
      objects: state.objects.filter((obj) => obj.id !== id),
      selectedObjectId: state.selectedObjectId === id ? null : state.selectedObjectId,
    })),
  setZoom: (zoom) => set({ zoom }),
}));
