import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { useEffect, useRef, useState, useCallback } from 'react';

const SIGNALR_URL = import.meta.env.VITE_SIGNALR_URL || 'http://localhost:5000/hubs/robot';

let sharedConnection: HubConnection | null = null;
let sharedRefCount = 0;
let sharedConnected = false;
const sharedListeners: Set<() => void> = new Set();

function getOrCreateConnection(): HubConnection {
  if (sharedConnection && sharedConnection.state !== HubConnectionState.Disconnected) {
    return sharedConnection;
  }

  const token = localStorage.getItem('access_token');

  const conn = new HubConnectionBuilder()
    .withUrl(SIGNALR_URL, {
      ...(token ? { accessTokenFactory: () => token } : {}),
    })
    .withAutomaticReconnect([0, 2, 5, 10, 15, 30])
    .build();

  conn.onreconnecting(() => {
    sharedConnected = false;
    sharedListeners.forEach((l) => l());
  });
  conn.onreconnected(() => {
    sharedConnected = true;
    conn.invoke('JoinRobotGroup').catch(() => {});
    sharedListeners.forEach((l) => l());
  });
  conn.onclose(() => {
    sharedConnected = false;
    sharedListeners.forEach((l) => l());
  });

  conn.start()
    .then(() => {
      sharedConnected = true;
      return conn.invoke('JoinRobotGroup');
    })
    .then(() => sharedListeners.forEach((l) => l()))
    .catch((err: unknown) => console.error('[SignalR] Connection error:', err));

  sharedConnection = conn;
  return conn;
}

export const useSignalR = () => {
  const [connected, setConnected] = useState(sharedConnected);
  const connectionRef = useRef<HubConnection | null>(null);

  useEffect(() => {
    try {
      connectionRef.current = getOrCreateConnection();
    } catch {
      return;
    }
    sharedRefCount++;
    setConnected(sharedConnected);

    const listener = () => setConnected(sharedConnected);
    sharedListeners.add(listener);

    return () => {
      sharedListeners.delete(listener);
      sharedRefCount--;
      if (sharedRefCount === 0 && sharedConnection) {
        sharedConnection.stop();
        sharedConnection = null;
        sharedConnected = false;
      }
    };
  }, []);

  const invoke = useCallback(
    (method: string, ...args: unknown[]) => {
      return connectionRef.current?.invoke(method, ...args);
    },
    [],
  );

  const on = useCallback(
    (event: string, callback: (...args: unknown[]) => void) => {
      const conn = connectionRef.current;
      if (!conn) {
        console.warn(`[SignalR] on('${event}'): connection not ready yet`);
        return () => {};
      }
      conn.on(event, callback);
      return () => {
        connectionRef.current?.off(event, callback);
      };
    },
    [],
  );

  return { connection: connectionRef.current, connected, invoke, on };
};
