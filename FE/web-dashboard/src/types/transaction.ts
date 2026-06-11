export interface Transaction {
  id: string; // e.g. #ORD-8921
  dateTime: string;
  tableNo: string;
  totalAmount: number;
  paymentMethod: string;
  status: 'Completed' | 'Cancelled' | 'Refunded';
}
