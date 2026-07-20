import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';

// BE chỉ expose ASP.NET Core SignalR (OrderHub tại /hubs/orders), không có server
// Socket.IO nào — client phải nói cùng giao thức thì mới nhận được sự kiện thật.
const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api/v1';
const HUB_URL = import.meta.env.VITE_ORDER_HUB_URL
  || `${API_BASE_URL.replace(/\/api\/v1\/?$/, '')}/hubs/orders`;

interface UseOrderHubOptions {
  // Tham gia nhóm "KitchenGroup" để nhận ReceiveNewOrder — chỉ dành cho STAFF/CHEF/MANAGER
  // (OrderHub.JoinKitchenGroup yêu cầu Roles.KitchenStaff ở phía BE).
  joinKitchenGroup?: boolean;
}

/**
 * Kết nối tới OrderHub (SignalR) và lắng nghe các sự kiện realtime thực sự do BE
 * gửi: ReceiveNewOrder, ReceiveOrderStatusUpdate, ReceivePaymentSuccess,
 * ReceiveCashPaymentPending.
 */
export const useOrderHub = (
  eventMap: { [eventName: string]: (data: any) => void },
  options: UseOrderHubOptions = {}
) => {
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    const token = localStorage.getItem('access_token');
    if (!token) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .build();
    connectionRef.current = connection;

    Object.entries(eventMap).forEach(([event, handler]) => {
      connection.on(event, handler);
    });

    connection
      .start()
      .then(() => {
        if (options.joinKitchenGroup) {
          connection.invoke('JoinKitchenGroup').catch((err) => {
            console.warn('JoinKitchenGroup failed:', err);
          });
        }
      })
      .catch((err) => console.warn('SignalR connect error:', err));

    return () => {
      Object.keys(eventMap).forEach((event) => connection.off(event));
      if (options.joinKitchenGroup && connection.state === signalR.HubConnectionState.Connected) {
        connection.invoke('LeaveKitchenGroup').catch(() => {});
      }
      connection.stop();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [eventMap, options.joinKitchenGroup]);
};
