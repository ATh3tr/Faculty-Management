import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { AuthProvider } from "./auth/AuthContext";
import { BrandingProvider } from "./branding/BrandingContext";
import { LanguageProvider } from "./lib/i18n";
import App from "./App";
import "./styles.css";

createRoot(document.getElementById("root")!).render(
  <StrictMode><LanguageProvider><BrandingProvider><BrowserRouter><AuthProvider><App /></AuthProvider></BrowserRouter></BrandingProvider></LanguageProvider></StrictMode>
);
