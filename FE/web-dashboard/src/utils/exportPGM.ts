import type { MapObject } from '@/types/map';

export function exportPGM(objects: MapObject[]): string {
  const width = 120;
  const height = 80;
  const pixels = Array.from({ length: height }, () => Array<string>(width).fill('255'));

  objects.forEach((object) => {
    if (object.type === 'wall' || object.type === 'restricted') {
      const gridX = Math.max(0, Math.min(width - 1, Math.round(object.x / 20)));
      const gridY = Math.max(0, Math.min(height - 1, Math.round(object.y / 20)));
      pixels[gridY][gridX] = '0';
    }
  });

  const body = pixels.map((row) => row.join(' ')).join('\n');

  return [
    'P2',
    '# Future: convert map objects to occupancy grid for Webots navigation',
    `${width} ${height}`,
    '255',
    body,
  ].join('\n');
}
