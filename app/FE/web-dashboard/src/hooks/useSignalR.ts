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
  if (!token) throw new Error('No access_token in localStorage');

  sharedConnection = new HubConnectionBuilder()
    .withUrl(SIGNALR_URL, {
      accessTokenFactory: () => token,
      transport: 0,
    })
    .withAutomaticReconnect([0, 2, 5, 10, 15, 30])
    .build();

  sharedConnection.onreconnecting(() => {
    sharedConnected = false;
    sharedListeners.forEach((l) => l());
  });
  sharedConnection.onreconnected(() => {
    sharedConnected = true;
    sharedConnection?.invoke('JoinRobotGroup').catch(() => {});
    sharedListeners.forEach((l) => l());
  });
  sharedConnection.onclose(() => {
    sharedConnected = false;
    sharedListeners.forEach((l) => l());
  });

  sharedConnection.start()
    .then(() => {
      sharedConnected = true;
      return sharedConnection!.invoke('JoinRobotGroup');
    })
    .then(() => sharedListeners.forEach((l) => l()))
    .catch((err: unknown) => console.error('[SignalR] Connection error:', err));

  return sharedConnection;
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
      connectionRef.current?.on(event, callback);
      return () => {
        connectionRef.current?.off(event, callback);
      };
    },
    [],
  );

  return { connection: connectionRef.current, connected, invoke, on };
};
