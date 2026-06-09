export interface GridPoint {
  x: number;
  y: number;
}

export interface RouteValidationResult {
  isValid: boolean;
  warnings: string[];
}

export class AStarService {
  findPath(start: GridPoint, end: GridPoint): GridPoint[] {
    return [
      start,
      { x: (start.x + end.x) / 2, y: start.y },
      { x: (start.x + end.x) / 2, y: end.y },
      end,
    ];
  }

  validateRoute(): RouteValidationResult {
    return {
      isValid: true,
      warnings: [],
    };
  }
}

export const astarService = new AStarService();
