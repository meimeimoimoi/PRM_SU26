export type KitchenItemStatus = 'WAITING' | 'DOING' | 'DONE' | 'SERVED' | 'CANCELLED' | 'RETURNED';

export interface KitchenItem {
  id: number; // Matches int Id in BaseEntity
  orderId: number; // Matches int OrderId
  tableNumber: number; // From Table entity via DiningSession
  name: string; // From MenuItem Name
  quantity: number;
  notes?: string;
  status: KitchenItemStatus;
  orderedAt: string; // Matches CreatedAt timestamp
  elapsedSeconds: number; // for UI cooking timer
}
