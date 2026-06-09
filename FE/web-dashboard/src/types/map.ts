export type ObjectType =
  | 'table'
  | 'chair'
  | 'wall'
  | 'kitchen'
  | 'delivery'
  | 'charging'
  | 'restricted'
  | 'door'
  | 'robot';

export interface MapObject {
  id: string;
  name: string;
  type: ObjectType;
  x: number;
  y: number;
  width: number;
  height: number;
  rotation: number;
}
