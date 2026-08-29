import { afterEach, describe, expect, it, vi } from "vitest";
import { api, setAccessToken } from "./api";

afterEach(() => { vi.unstubAllGlobals(); setAccessToken(null); });

describe("API client", () => {
  it("adds the bearer token and includes refresh cookies", async () => {
    setAccessToken("test-token");
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ ok: true }), {
      status: 200, headers: { "Content-Type": "application/json" }
    }));
    vi.stubGlobal("fetch", fetchMock);

    await api("/api/example");

    const [, request] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect((request.headers as Headers).get("Authorization")).toBe("Bearer test-token");
    expect(request.credentials).toBe("include");
  });

  it("surfaces problem-details messages", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(JSON.stringify({ detail: "Room is already reserved." }), {
      status: 409, headers: { "Content-Type": "application/problem+json" }
    })));
    await expect(api("/api/schedules")).rejects.toEqual(expect.objectContaining({ status: 409, message: "Room is already reserved." }));
  });

  it("surfaces plain-text errors without consuming the response twice", async () => {
    const response = new Response("Too Many Requests", { status: 429 });
    const textSpy = vi.spyOn(response, "text");
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(response));

    await expect(api("/api/auth/login")).rejects.toEqual(expect.objectContaining({
      status: 429,
      message: "Too Many Requests",
      details: "Too Many Requests"
    }));
    expect(textSpy).toHaveBeenCalledTimes(1);
  });
});
