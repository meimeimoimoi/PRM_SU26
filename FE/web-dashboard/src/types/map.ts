export type MapObjectType =
  | 'table'
  | 'chair'
  | 'wall'
  | 'kitchen'
  | 'delivery'
  | 'charging'
  | 'restricted'
  | 'door'
  | 'robotStart';

export type MapTool =
  | 'select'
  | 'pan'
  | MapObjectType;

export interface MapObject {
  id: string;
  name: string;
  type: MapObjectType;
  x: number;
  y: number;
  width: number;
  height: number;
  rotation: number;
  tableNumber?: number;
}
