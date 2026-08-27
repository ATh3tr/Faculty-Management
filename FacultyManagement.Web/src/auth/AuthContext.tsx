import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { HubConnectionBuilder, HttpTransportType, LogLevel, type HubConnection } from "@microsoft/signalr";
import { api, getApiBase, json, setAccessToken } from "../lib/api";
import type { Notification, Role, User } from "../types";

type AuthContextValue = {
  user: User | null; token: string | null; loading: boolean; liveNotification: Notification | null;
  login: (email: string, password: string) => Promise<void>; logout: () => Promise<void>; hasRole: (...roles: Role[]) => boolean;
};
const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [expiresAt, setExpiresAt] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [liveNotification, setLiveNotification] = useState<Notification | null>(null);

  const loadUser = useCallback(async (nextToken: string, nextExpiry?: string) => {
    setAccessToken(nextToken); setToken(nextToken); if (nextExpiry) setExpiresAt(nextExpiry);
    const me = await api<User>("/api/auth/me"); setUser(me);
  }, []);

  useEffect(() => { (async () => {
    try { const result = await api<{ accessToken: string; accessTokenExpiresAtUtc: string }>("/api/auth/refresh", { method: "POST" }); await loadUser(result.accessToken, result.accessTokenExpiresAtUtc); }
    catch { setAccessToken(null); setToken(null); setExpiresAt(null); setUser(null); }
    finally { setLoading(false); }
  })(); }, [loadUser]);

  useEffect(() => {
    if (!token || !expiresAt) return;
    const delay = Math.max(5_000, new Date(expiresAt).getTime() - Date.now() - 60_000);
    const timer = window.setTimeout(async () => {
      try {
        const result = await api<{ accessToken: string; accessTokenExpiresAtUtc: string }>("/api/auth/refresh", { method: "POST" });
        setAccessToken(result.accessToken); setToken(result.accessToken); setExpiresAt(result.accessTokenExpiresAtUtc);
      } catch { setAccessToken(null); setToken(null); setExpiresAt(null); setUser(null); }
    }, delay);
    return () => window.clearTimeout(timer);
  }, [token, expiresAt]);

  useEffect(() => {
    if (!token) return;
    let connection: HubConnection | undefined;
    connection = new HubConnectionBuilder().withUrl(`${getApiBase()}/hubs/faculty`, {
      accessTokenFactory: () => token, transport: HttpTransportType.WebSockets | HttpTransportType.LongPolling
    }).withAutomaticReconnect().configureLogging(LogLevel.Warning).build();
    connection.on("notificationReceived", (notification: Notification) => setLiveNotification(notification));
    connection.start().catch(() => undefined);
    return () => { connection?.stop().catch(() => undefined); };
  }, [token]);

  const login = async (email: string, password: string) => {
    const result = await api<{ accessToken: string; accessTokenExpiresAtUtc: string }>("/api/auth/login", { method: "POST", ...json({ email, password }) });
    await loadUser(result.accessToken, result.accessTokenExpiresAtUtc);
  };
  const logout = async () => { try { await api("/api/auth/logout", { method: "POST" }); } finally { setAccessToken(null); setToken(null); setExpiresAt(null); setUser(null); } };
  const value = useMemo(() => ({ user, token, loading, liveNotification, login, logout, hasRole: (...roles: Role[]) => !!user?.roles.some(r => roles.includes(r)) }), [user, token, loading, liveNotification]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() { const value = useContext(AuthContext); if (!value) throw new Error("AuthProvider is missing"); return value; }
