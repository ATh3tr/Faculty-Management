const apiBase = (import.meta.env.VITE_API_URL || "").replace(/\/$/, "");
let accessToken: string | null = null;

export class ApiError extends Error {
  constructor(public status: number, message: string, public details?: unknown) { super(message); }
}

export function setAccessToken(token: string | null) { accessToken = token; }
export function getApiBase() { return apiBase; }

export async function api<T = void>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers);
  if (accessToken) headers.set("Authorization", `Bearer ${accessToken}`);
  if (init.body && !(init.body instanceof FormData) && !headers.has("Content-Type")) headers.set("Content-Type", "application/json");
  const response = await fetch(`${apiBase}${path}`, { ...init, headers, credentials: "include" });
  if (!response.ok) {
    const rawBody = await response.text();
let details: unknown = rawBody;

if (rawBody) {
  try {
    details = JSON.parse(rawBody);
  } catch {
    // Keep the original text when it is not JSON.
  }
}
    const problem = details as { detail?: string; title?: string; errors?: Record<string, string[]> } | undefined;
    const validation = problem?.errors ? Object.values(problem.errors).flat().join(" ") : "";
    throw new ApiError(response.status, validation || problem?.detail || problem?.title || `Request failed (${response.status})`, details);
  }
  if (response.status === 204 || response.headers.get("content-length") === "0") return undefined as T;
  return response.json() as Promise<T>;
}

export const json = (body: unknown): RequestInit => ({ body: JSON.stringify(body) });
