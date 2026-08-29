import { defineConfig, loadEnv, type ProxyOptions } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  const target = env.VITE_API_PROXY_TARGET || "http://localhost:5176";
  const preserveAuthorization: ProxyOptions = {
    target,
    changeOrigin: true,
    secure: false,
    configure(proxy) {
      proxy.on("proxyReq", (proxyRequest, request) => {
        if (request.headers.authorization) {
          proxyRequest.setHeader("authorization", request.headers.authorization);
        }
      });
    }
  };

  return {
    plugins: [react()],
    server: {
      port: 5173,
      proxy: {
        "/api": preserveAuthorization,
        "/hubs": { ...preserveAuthorization, ws: true },
        "/health": { target, changeOrigin: true, secure: false }
      }
    }
  };
});
