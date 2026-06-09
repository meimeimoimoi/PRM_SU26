export interface GridPoint {
  x: number;
  y: number;
}

export interface RouteValidationResult {
  isValid: boolean;
  warnings: string[];
}

export class AStarService {
  findPath(): GridPoint[] {
    console.log('TODO: calculate A* path for robot navigation.');
    return [];
  }

  validateRoute(): RouteValidationResult {
    console.log('TODO: validate robot route against walls and restricted zones.');
    return {
      isValid: true,
      warnings: ['TODO_ROUTE_VALIDATION'],
    };
  }
}

export const astarService = new AStarService();
