import type { MapObject } from '@/types/map';

export function exportWaypoints(objects: MapObject[]): string {
  return objects
    .filter((object) => object.type === 'table')
    .map((object, index) => `TABLE_${index + 1} ${object.x.toFixed(2)} ${object.y.toFixed(2)}`)
    .join('\n');
}
