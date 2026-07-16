export type TableStatus = 'AVAILABLE' | 'OCCUPIED';

export interface Table {
  id: number;
  tableNumber: number;
  capacity: number;
  status: TableStatus;
  qrCode?: string;
  locationId?: number;
  locationName?: string;
}
