import { create } from 'zustand';
import type { MapObject } from '@/types/map';

interface MapState {
  objects: MapObject[];
  selectedObject: MapObject | null;
  addObject: (object: MapObject) => void;
  updateObject: (id: string, updates: Partial<MapObject>) => void;
  removeObject: (id: string) => void;
  selectObject: (id: string) => void;
  clearSelection: () => void;
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
  },
  {
    id: 'table-3',
    name: 'Table 03',
    type: 'table',
    x: 470,
    y: 300,
    width: 92,
    height: 92,
    rotation: 0,
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
    type: 'robot',
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

export const useMapStore = create<MapState>((set, get) => ({
  objects: initialObjects,
  selectedObject: initialObjects[1],
  addObject: (object) =>
    set((state) => ({
      objects: [...state.objects, object],
      selectedObject: object,
    })),
  updateObject: (id, updates) =>
    set((state) => {
      const objects = state.objects.map((object) =>
        object.id === id ? { ...object, ...updates } : object,
      );

      return {
        objects,
        selectedObject:
          state.selectedObject?.id === id
            ? objects.find((object) => object.id === id) ?? null
            : state.selectedObject,
      };
    }),
  removeObject: (id) =>
    set((state) => ({
      objects: state.objects.filter((object) => object.id !== id),
      selectedObject: state.selectedObject?.id === id ? null : state.selectedObject,
    })),
  selectObject: (id) =>
    set({
      selectedObject: get().objects.find((object) => object.id === id) ?? null,
    }),
  clearSelection: () => set({ selectedObject: null }),
}));
